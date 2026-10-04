namespace ParkingVision.Core;

/// <summary>Runtime knobs shared by API / worker / simulator. Bound from the "Parking" config section.</summary>
public class ParkingSettings
{
    /// <summary>Observation older than this is ignored in fusion; spot state older than this becomes Unknown.</summary>
    public int ObservationTtlSeconds { get; set; } = 180;
    public int ObservationRetentionHours { get; set; } = 24;
}

public interface IObservationIngest
{
    /// <summary>Stores observations, updates camera heartbeat and recomputes fused SpotState. Returns number of accepted items.</summary>
    Task<int> IngestAsync(ObservationBatchDto batch, CancellationToken ct = default);
}

public interface IParkomatIngest
{
    /// <summary>Idempotent on (ParkomatCode, ExternalTicketId). Returns number of NEW tickets.</summary>
    Task<int> IngestAsync(IEnumerable<ParkomatTicketDto> tickets, CancellationToken ct = default);
}

public static class Geo
{
    public static double DistanceM(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        double dLat = ToRad(lat2 - lat1), dLon = ToRad(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * R * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }
    static double ToRad(double d) => d * Math.PI / 180;
}

/// <summary>Pure fusion logic: several camera readings about ONE spot -> one status. Unit-tested.</summary>
public static class SpotFusion
{
    public record Reading(DateTime TimestampUtc, bool Occupied, double Confidence, double Weight);

    /// <param name="latestPerCamera">Latest reading from each camera that covers the spot.</param>
    public static (OccupancyStatus Status, double Confidence) Fuse(IEnumerable<Reading> latestPerCamera, DateTime nowUtc, TimeSpan maxAge)
    {
        var fresh = latestPerCamera.Where(r => nowUtc - r.TimestampUtc <= maxAge).ToList();
        if (fresh.Count == 0) return (OccupancyStatus.Unknown, 0);

        double occ = fresh.Where(r => r.Occupied).Sum(r => r.Confidence * r.Weight);
        double free = fresh.Where(r => !r.Occupied).Sum(r => r.Confidence * r.Weight);
        double total = occ + free;
        if (total <= 0) return (OccupancyStatus.Unknown, 0);

        // Tie -> Occupied: wrongly showing a free spot costs the driver more than the opposite.
        bool isOccupied = occ >= free;
        double agreement = Math.Max(occ, free) / total;
        double meanConf = fresh.Average(r => r.Confidence);
        return (isOccupied ? OccupancyStatus.Occupied : OccupancyStatus.Free, Math.Round(agreement * meanConf, 3));
    }
}
