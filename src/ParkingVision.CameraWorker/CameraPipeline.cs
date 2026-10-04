using NetTopologySuite.Geometries;
using ParkingVision.Core;
using ParkingVision.Vision;

namespace ParkingVision.CameraWorker;

/// <summary>frame -> YOLO -> per-spot raw reading -> debounce -> (on change / keep-alive) ingest.</summary>
public sealed class CameraPipeline(
    CameraConfig config, YoloDetector detector, SpotEvaluator evaluator,
    IObservationIngest ingest, VisionOptions o, ILogger log)
{
    public async Task RunAsync(CancellationToken ct)
    {
        using var source = new CaptureFrameSource(config.StreamUrl);
        var debouncers = config.Spots.ToDictionary(s => s.SpotCode, _ => new SpotDebouncer());
        var lastSent = new Dictionary<string, (bool Occupied, DateTime At)>();
        Dictionary<string, Polygon>? polygons = null;
        (int W, int H) polygonsFor = (0, 0);

        var stableFor = TimeSpan.FromSeconds(o.StableSeconds);
        var keepAlive = TimeSpan.FromSeconds(o.KeepAliveSeconds);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(o.IntervalSeconds));

        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                var now = DateTime.UtcNow;
                if (source.LastFrameUtc is null || now - source.LastFrameUtc > TimeSpan.FromSeconds(o.FrameStaleSeconds))
                {
                    log.LogWarning("No fresh frame - camera treated as offline (nothing reported)");
                    continue;
                }

                using var frame = source.GetLatest();
                if (frame is null) continue;

                if (polygons is null || polygonsFor != (frame.Width, frame.Height))
                {
                    polygons = new Dictionary<string, Polygon>();
                    foreach (var s in config.Spots)
                    {
                        var p = SpotEvaluator.ParseRoi(s.RoiJson, frame.Width, frame.Height);
                        if (p is null) log.LogError("Invalid ROI for spot {Spot} - spot skipped", s.SpotCode);
                        else polygons[s.SpotCode] = p;
                    }
                    polygonsFor = (frame.Width, frame.Height);
                }

                var detections = await Task.Run(() => detector.Detect(frame), ct);
                var readings = evaluator.Evaluate(detections, polygons);

                var items = new List<ObservationItemDto>();
                foreach (var (spot, reading) in readings)
                {
                    var (stable, _) = debouncers[spot].Update(reading.Occupied, now, stableFor);
                    bool changed = !lastSent.TryGetValue(spot, out var last) || last.Occupied != stable;
                    bool due = !changed && now - last.At >= keepAlive;
                    if (!changed && !due) continue;

                    items.Add(new ObservationItemDto(spot, stable, Math.Round(reading.Confidence, 3), CocoVehicle.Name(reading.ClassId)));
                    lastSent[spot] = (stable, now);
                }

                if (items.Count > 0)
                {
                    await ingest.IngestAsync(new ObservationBatchDto(config.Code, now, items), ct);
                    log.LogInformation("Sent {Count} spot update(s); detections in frame: {Det}", items.Count, detections.Count);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                log.LogError(ex, "Pipeline iteration failed - continuing");
            }
        }
    }
}
