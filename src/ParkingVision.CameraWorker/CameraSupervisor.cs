using Microsoft.EntityFrameworkCore;
using ParkingVision.Core;
using ParkingVision.Data;
using ParkingVision.Vision;

namespace ParkingVision.CameraWorker;

public sealed record SpotRoi(string SpotCode, string RoiJson);
public sealed record CameraConfig(string Code, string StreamUrl, List<SpotRoi> Spots);

/// <summary>Loads real (non-simulated) cameras from the DB and runs one pipeline per camera. Restart the worker to pick up config changes.</summary>
public sealed class CameraSupervisor(
    IDbContextFactory<ParkingDbContext> factory, IObservationIngest ingest, YoloDetector detector,
    SpotEvaluator evaluator, VisionOptions options, ILoggerFactory loggers, ILogger<CameraSupervisor> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        List<CameraConfig> configs;
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            var cams = await db.Cameras.AsNoTracking()
                .Where(c => c.Enabled && !c.IsSimulated && c.StreamUrl != null)
                .Include(c => c.Spots.Where(s => s.Enabled)).ThenInclude(s => s.Spot)
                .ToListAsync(ct);
            configs = cams.Select(c => new CameraConfig(c.Code, ResolveUrl(c.StreamUrl!),
                c.Spots.Select(s => new SpotRoi(s.Spot.Code, s.RoiJson)).ToList())).ToList();
        }

        if (configs.Count == 0)
        {
            log.LogWarning("No real cameras in DB (Enabled, IsSimulated=false, StreamUrl set). Use the Simulator for demo data. Idling.");
            await Task.Delay(Timeout.Infinite, ct);
            return;
        }

        log.LogInformation("Starting {Count} camera pipeline(s)", configs.Count);
        var tasks = configs.Select(c =>
            new CameraPipeline(c, detector, evaluator, ingest, options, loggers.CreateLogger($"cam.{c.Code}")).RunAsync(ct));
        await Task.WhenAll(tasks);
    }

    /// <summary>"env:CAM_A1_URL" -> value of that environment variable (keeps RTSP credentials out of the DB).</summary>
    private static string ResolveUrl(string url) =>
        url.StartsWith("env:", StringComparison.OrdinalIgnoreCase)
            ? Environment.GetEnvironmentVariable(url[4..]) ?? throw new InvalidOperationException($"Environment variable '{url[4..]}' is not set")
            : url;
}
