using System.Text.Json;
using NetTopologySuite.Geometries;

namespace ParkingVision.Vision;

/// <summary>
/// Maps detections to spot ROIs. Key ideas (documented in docs/03-camera-software.md):
///  * Only the BOTTOM HALF of a vehicle box (its "footprint") is compared with the ROI - the roof overlaps neighbours in oblique views.
///  * Each detection is assigned to exactly ONE spot: the ROI it covers the most. Prevents one car marking two spots.
/// </summary>
public sealed class SpotEvaluator(VisionOptions options)
{
    private static readonly GeometryFactory Gf = new();

    /// <summary>Normalized JSON [[x,y],...] -> pixel polygon for a given frame size. Returns null when invalid.</summary>
    public static Polygon? ParseRoi(string json, int width, int height)
    {
        try
        {
            var pts = JsonSerializer.Deserialize<double[][]>(json);
            if (pts is null || pts.Length < 3) return null;
            var coords = pts.Select(p => new Coordinate(p[0] * width, p[1] * height)).ToList();
            coords.Add(coords[0]);
            var poly = Gf.CreatePolygon(coords.ToArray());
            return poly.IsValid && poly.Area > 1 ? poly : null;
        }
        catch { return null; }
    }

    public Dictionary<string, SpotReading> Evaluate(IReadOnlyList<Detection> detections, IReadOnlyDictionary<string, Polygon> rois)
    {
        var result = rois.Keys.ToDictionary(k => k, _ => new SpotReading(false, options.FreeConfidence, null));

        foreach (var d in detections)
        {
            double h = d.Y2 - d.Y1;
            var footprint = Gf.ToGeometry(new Envelope(d.X1, d.X2, d.Y2 - h * 0.5, d.Y2));

            string? bestKey = null;
            double bestCoverage = 0;
            foreach (var (key, poly) in rois)
            {
                if (!poly.Intersects(footprint)) continue;
                double coverage = poly.Intersection(footprint).Area / poly.Area;
                if (coverage > bestCoverage) { bestCoverage = coverage; bestKey = key; }
            }

            if (bestKey is not null && bestCoverage >= options.CoverageThreshold)
            {
                var cur = result[bestKey];
                if (!cur.Occupied || d.Score > cur.Confidence)
                    result[bestKey] = new SpotReading(true, d.Score, d.ClassId);
            }
        }
        return result;
    }
}
