using Microsoft.EntityFrameworkCore;
using ParkingVision.Core;

namespace ParkingVision.Data;

/// <summary>Answers "which camera covers which parkomat" - derived through spots (N-M on both sides).</summary>
public sealed class TopologyService(IDbContextFactory<ParkingDbContext> factory)
{
    public async Task<List<CameraInfoDto>> GetCamerasAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var cams = await db.Cameras.AsNoTracking()
            .Include(c => c.Spots).ThenInclude(cs => cs.Spot).ThenInclude(s => s.Parkomats).ThenInclude(ps => ps.Parkomat)
            .OrderBy(c => c.Code).ToListAsync(ct);

        return cams.Select(c => new CameraInfoDto(c.Code, c.Name, c.Status, c.LastHeartbeatUtc, c.IsSimulated,
            c.Spots.Select(cs => cs.Spot.Code).OrderBy(x => x).ToList(),
            c.Spots.SelectMany(cs => cs.Spot.Parkomats).Select(ps => ps.Parkomat.Code).Distinct().OrderBy(x => x).ToList())).ToList();
    }

    public async Task<List<ParkomatCoverageDto>> GetCoverageAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var parkomats = await db.Parkomats.AsNoTracking()
            .Include(p => p.Spots).ThenInclude(ps => ps.Spot).ThenInclude(s => s.Cameras).ThenInclude(cs => cs.Camera)
            .OrderBy(p => p.Code).ToListAsync(ct);

        return parkomats.Select(p =>
        {
            var spots = p.Spots.Select(ps => ps.Spot).ToList();
            var perCamera = spots.SelectMany(s => s.Cameras.Where(cs => cs.Enabled).Select(cs => cs.Camera.Code))
                .GroupBy(x => x).Select(g => new CameraShareDto(g.Key, g.Count())).OrderBy(x => x.CameraCode).ToList();
            int uncovered = spots.Count(s => !s.Cameras.Any(cs => cs.Enabled));
            return new ParkomatCoverageDto(p.Code, p.Name, spots.Count, uncovered, perCamera);
        }).ToList();
    }
}
