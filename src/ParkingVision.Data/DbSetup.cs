using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkingVision.Core;

namespace ParkingVision.Data;

public static class DbSetup
{
    /// <summary>Registers DbContext factory + all data services as singletons (they open short-lived contexts).</summary>
    public static IServiceCollection AddParkingData(this IServiceCollection s, string? configuredDbPath = null, ParkingSettings? settings = null)
    {
        var path = DbPaths.Resolve(configuredDbPath);
        s.AddDbContextFactory<ParkingDbContext>(o => o.UseSqlite($"Data Source={path};Default Timeout=30"));
        s.AddSingleton(settings ?? new ParkingSettings());
        s.AddSingleton(TimeProvider.System);
        s.AddSingleton<IObservationIngest, ObservationIngest>();
        s.AddSingleton<IParkomatIngest, ParkomatIngest>();
        s.AddSingleton<AvailabilityService>();
        s.AddSingleton<TopologyService>();
        s.AddSingleton<MaintenanceService>();
        s.AddSingleton<DemoSeeder>();
        return s;
    }

    /// <summary>Creates schema if missing (prototype: no migrations yet) and enables WAL so API + simulator + worker can share the file.</summary>
    public static async Task EnsureDatabaseAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        var f = sp.GetRequiredService<IDbContextFactory<ParkingDbContext>>();
        await using var db = await f.CreateDbContextAsync(ct);
        await db.Database.EnsureCreatedAsync(ct);
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", ct);
    }
}
