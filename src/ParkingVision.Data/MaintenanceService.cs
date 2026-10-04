using Microsoft.EntityFrameworkCore;
using ParkingVision.Core;

namespace ParkingVision.Data;

/// <summary>Housekeeping: expire stale states, flag offline cameras, purge old rows. Called periodically by the API host.</summary>
public sealed class MaintenanceService(IDbContextFactory<ParkingDbContext> factory, ParkingSettings settings, TimeProvider clock)
{
    public async Task SweepAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var now = clock.GetUtcNow().UtcDateTime;
        var ttl = TimeSpan.FromSeconds(settings.ObservationTtlSeconds);

        var stale = await db.SpotStates
            .Where(s => s.Status != OccupancyStatus.Unknown && s.Source == DataSource.Camera && s.UpdatedUtc < now - ttl)
            .ToListAsync(ct);
        foreach (var s in stale)
        {
            s.Status = OccupancyStatus.Unknown; s.Confidence = 0; s.Source = DataSource.None;
            s.SinceUtc = now; s.UpdatedUtc = now;
        }

        var offline = await db.Cameras
            .Where(c => c.Status == CameraStatus.Online && (c.LastHeartbeatUtc == null || c.LastHeartbeatUtc < now - ttl * 2))
            .ToListAsync(ct);
        foreach (var c in offline) c.Status = CameraStatus.Offline;

        await db.SaveChangesAsync(ct);

        var obsCutoff = now - TimeSpan.FromHours(settings.ObservationRetentionHours);
        await db.SpotObservations.Where(o => o.TimestampUtc < obsCutoff).ExecuteDeleteAsync(ct);
        await db.ParkomatTickets.Where(t => t.ValidToUtc < now.AddDays(-7)).ExecuteDeleteAsync(ct);
    }
}
