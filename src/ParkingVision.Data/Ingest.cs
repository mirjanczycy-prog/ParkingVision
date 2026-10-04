using Microsoft.EntityFrameworkCore;
using ParkingVision.Core;

namespace ParkingVision.Data;

public sealed class ObservationIngest(IDbContextFactory<ParkingDbContext> factory, ParkingSettings settings) : IObservationIngest
{
    public async Task<int> IngestAsync(ObservationBatchDto batch, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var camera = await db.Cameras.SingleOrDefaultAsync(c => c.Code == batch.CameraCode, ct)
                     ?? throw new KeyNotFoundException($"Unknown camera '{batch.CameraCode}'");

        camera.LastHeartbeatUtc = batch.TimestampUtc;
        camera.Status = CameraStatus.Online;

        // Only accept spots that this camera is actually linked to (ignores misconfigured edge devices).
        var links = await db.CameraSpots.Where(cs => cs.CameraId == camera.Id && cs.Enabled)
            .Select(cs => new { cs.SpotId, cs.Spot.Code }).ToListAsync(ct);
        var spotIdByCode = links.ToDictionary(l => l.Code, l => l.SpotId);

        var accepted = new List<(int SpotId, ObservationItemDto Item)>();
        foreach (var item in batch.Items)
            if (spotIdByCode.TryGetValue(item.SpotCode, out var sid)) accepted.Add((sid, item));

        foreach (var (spotId, item) in accepted)
            db.SpotObservations.Add(new SpotObservation
            {
                CameraId = camera.Id, SpotId = spotId, TimestampUtc = batch.TimestampUtc,
                Occupied = item.Occupied, Confidence = item.Confidence, VehicleClass = item.VehicleClass
            });
        await db.SaveChangesAsync(ct);

        await RecomputeStatesAsync(db, accepted.Select(a => a.SpotId).Distinct().ToList(), batch.TimestampUtc, ct);
        return accepted.Count;
    }

    private async Task RecomputeStatesAsync(ParkingDbContext db, List<int> spotIds, DateTime nowUtc, CancellationToken ct)
    {
        if (spotIds.Count == 0) return;
        var ttl = TimeSpan.FromSeconds(settings.ObservationTtlSeconds);
        var cutoff = nowUtc - ttl;

        var recent = await db.SpotObservations
            .Where(o => spotIds.Contains(o.SpotId) && o.TimestampUtc >= cutoff)
            .ToListAsync(ct);
        var weights = await db.CameraSpots.Where(cs => spotIds.Contains(cs.SpotId))
            .ToDictionaryAsync(cs => (cs.SpotId, cs.CameraId), cs => cs.Weight, ct);
        var states = await db.SpotStates.Where(s => spotIds.Contains(s.SpotId)).ToDictionaryAsync(s => s.SpotId, ct);

        foreach (var spotId in spotIds)
        {
            var latestPerCamera = recent.Where(o => o.SpotId == spotId)
                .GroupBy(o => o.CameraId)
                .Select(g => g.OrderByDescending(o => o.TimestampUtc).First())
                .Select(o => new SpotFusion.Reading(o.TimestampUtc, o.Occupied, o.Confidence,
                    weights.GetValueOrDefault((spotId, o.CameraId), 1.0)))
                .ToList();

            var (status, conf) = SpotFusion.Fuse(latestPerCamera, nowUtc, ttl);

            if (!states.TryGetValue(spotId, out var st))
            {
                st = new SpotState { SpotId = spotId, SinceUtc = nowUtc };
                db.SpotStates.Add(st);
            }
            if (st.Status != status) st.SinceUtc = nowUtc;
            st.Status = status;
            st.Confidence = conf;
            st.Source = status == OccupancyStatus.Unknown ? DataSource.None : DataSource.Camera;
            st.UpdatedUtc = nowUtc;
        }
        await db.SaveChangesAsync(ct);
    }
}

public sealed class ParkomatIngest(IDbContextFactory<ParkingDbContext> factory) : IParkomatIngest
{
    public async Task<int> IngestAsync(IEnumerable<ParkomatTicketDto> tickets, CancellationToken ct = default)
    {
        var list = tickets.ToList();
        if (list.Count == 0) return 0;
        await using var db = await factory.CreateDbContextAsync(ct);

        var codes = list.Select(t => t.ParkomatCode).Distinct().ToList();
        var parkomats = await db.Parkomats.Where(p => codes.Contains(p.Code)).ToDictionaryAsync(p => p.Code, ct);

        var ids = list.Select(t => t.ExternalTicketId).Distinct().ToList();
        var existing = (await db.ParkomatTickets.Where(t => ids.Contains(t.ExternalTicketId))
            .Select(t => new { t.ParkomatId, t.ExternalTicketId }).ToListAsync(ct))
            .Select(t => (t.ParkomatId, t.ExternalTicketId)).ToHashSet();

        int added = 0;
        foreach (var t in list)
        {
            if (!parkomats.TryGetValue(t.ParkomatCode, out var p)) continue;
            if (!existing.Add((p.Id, t.ExternalTicketId))) continue;
            db.ParkomatTickets.Add(new ParkomatTicket
            {
                ParkomatId = p.Id, ZoneId = p.ZoneId, ExternalTicketId = t.ExternalTicketId,
                IssuedUtc = t.IssuedUtc, ValidToUtc = t.ValidToUtc, Amount = t.Amount, PlateHash = t.PlateHash
            });
            added++;
        }
        await db.SaveChangesAsync(ct);
        return added;
    }
}
