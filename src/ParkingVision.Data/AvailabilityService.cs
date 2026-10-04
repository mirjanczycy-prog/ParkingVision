using Microsoft.EntityFrameworkCore;
using ParkingVision.Core;

namespace ParkingVision.Data;

/// <summary>
/// Read model for user apps. Combines live camera state (SpotState) with a ticket-based estimate for spots
/// that have no live data. Heuristic v0 is documented in docs/04-processing.md - change it only together with that doc.
/// </summary>
public sealed class AvailabilityService(IDbContextFactory<ParkingDbContext> factory, TimeProvider clock)
{
    private sealed record SpotRow(int Id, int ZoneId, SpotType Type, double Lat, double Lon, OccupancyStatus Status,
                                  DataSource Source, DateTime? UpdatedUtc);

    public async Task<List<ZoneAvailabilityDto>> GetZonesAsync(double? lat, double? lon, double? radiusM, SpotType? type, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var now = clock.GetUtcNow().UtcDateTime;

        var zones = await db.Zones.AsNoTracking().ToListAsync(ct);
        var spots = await LoadSpotsAsync(db, ct);
        var tickets = await db.ParkomatTickets.AsNoTracking()
            .Where(t => t.ValidToUtc > now && t.IssuedUtc <= now)
            .GroupBy(t => t.ZoneId).Select(g => new { ZoneId = g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.ZoneId, x => x.N, ct);

        var result = new List<ZoneAvailabilityDto>();
        foreach (var z in zones)
        {
            var dto = Build(z, spots.Where(s => s.ZoneId == z.Id).ToList(), tickets.GetValueOrDefault(z.Id), lat, lon, type);
            if (dto.Total == 0) continue;
            if (radiusM.HasValue && dto.DistanceM.HasValue && dto.DistanceM.Value > radiusM.Value) continue;
            result.Add(dto);
        }
        return lat.HasValue && lon.HasValue
            ? result.OrderBy(r => r.DistanceM).ToList()
            : result.OrderBy(r => r.Name).ToList();
    }

    public async Task<ZoneDetailDto?> GetZoneAsync(int zoneId, double? lat, double? lon, SpotType? type, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var now = clock.GetUtcNow().UtcDateTime;

        var z = await db.Zones.AsNoTracking().SingleOrDefaultAsync(x => x.Id == zoneId, ct);
        if (z is null) return null;

        var spotRows = (await LoadSpotsAsync(db, ct)).Where(s => s.ZoneId == zoneId).ToList();
        var activeTickets = await db.ParkomatTickets.CountAsync(t => t.ZoneId == zoneId && t.ValidToUtc > now && t.IssuedUtc <= now, ct);
        var availability = Build(z, spotRows, activeTickets, lat, lon, type);

        var spotDtos = await db.Spots.AsNoTracking().Where(s => s.ZoneId == zoneId)
            .Include(s => s.State).OrderBy(s => s.Code).ToListAsync(ct);
        var spotList = spotDtos
            .Where(s => type == null || s.Type == type)
            .Select(s => new SpotDto(s.Id, s.Code, s.StreetName, s.Type, s.Lat, s.Lon,
                s.State?.Status ?? OccupancyStatus.Unknown, s.State?.Confidence ?? 0, s.State?.Source ?? DataSource.None,
                s.State?.SinceUtc, s.State?.UpdatedUtc)).ToList();

        var parkomats = await db.Parkomats.AsNoTracking().Where(p => p.ZoneId == zoneId)
            .Select(p => new { p.Id, p.Code, p.Name, p.Lat, p.Lon, Count = p.Spots.Count }).ToListAsync(ct);
        var parkomatDtos = parkomats.Select(p => new ParkomatDto(p.Id, p.Code, p.Name, p.Lat, p.Lon, p.Count,
            lat.HasValue && lon.HasValue ? Geo.DistanceM(lat.Value, lon.Value, p.Lat, p.Lon) : null))
            .OrderBy(p => p.DistanceM ?? 0).ToList();

        return new ZoneDetailDto(availability, z.TariffInfo, spotList, parkomatDtos);
    }

    private static Task<List<SpotRow>> LoadSpotsAsync(ParkingDbContext db, CancellationToken ct) =>
        db.Spots.AsNoTracking().Select(s => new SpotRow(
            s.Id, s.ZoneId, s.Type, s.Lat, s.Lon,
            s.State == null ? OccupancyStatus.Unknown : s.State.Status,
            s.State == null ? DataSource.None : s.State.Source,
            s.State == null ? null : s.State.UpdatedUtc)).ToListAsync(ct);

    private static ZoneAvailabilityDto Build(Zone z, List<SpotRow> zoneSpots, int activeTickets, double? lat, double? lon, SpotType? type)
    {
        var scoped = type == null ? zoneSpots : zoneSpots.Where(s => s.Type == type).ToList();
        int total = scoped.Count;
        int freeLive = scoped.Count(s => s.Source == DataSource.Camera && s.Status == OccupancyStatus.Free);
        int occLive = scoped.Count(s => s.Source == DataSource.Camera && s.Status == OccupancyStatus.Occupied);
        int uncovered = total - freeLive - occLive;

        // Heuristic v0: tickets are spread proportionally over all zone spots; applied to spots with no live data.
        int estOccUncovered = zoneSpots.Count == 0 ? 0
            : Math.Min(uncovered, (int)Math.Round(activeTickets * (double)uncovered / zoneSpots.Count));
        int estFreeUncovered = uncovered - estOccUncovered;

        AvailabilityConfidence conf =
            total == 0 ? AvailabilityConfidence.NoData :
            uncovered == 0 ? AvailabilityConfidence.Live :
            (freeLive + occLive) > 0 ? AvailabilityConfidence.Mixed :
            activeTickets > 0 ? AvailabilityConfidence.Estimated : AvailabilityConfidence.NoData;

        // No live data and no tickets at all -> we know nothing, do not pretend everything is free.
        if (conf == AvailabilityConfidence.NoData && total > 0) estFreeUncovered = 0;

        double? dist = null;
        if (lat.HasValue && lon.HasValue)
            dist = scoped.Count > 0
                ? scoped.Min(s => Geo.DistanceM(lat.Value, lon.Value, s.Lat, s.Lon))
                : Geo.DistanceM(lat.Value, lon.Value, z.CenterLat, z.CenterLon);

        var updated = scoped.Where(s => s.UpdatedUtc.HasValue).Select(s => s.UpdatedUtc).DefaultIfEmpty().Max();

        return new ZoneAvailabilityDto(z.Id, z.Code, z.Name, z.City, z.CenterLat, z.CenterLon,
            dist is null ? null : Math.Round(dist.Value), total, freeLive, occLive, uncovered, estFreeUncovered,
            freeLive + estFreeUncovered, activeTickets, conf, updated);
    }
}
