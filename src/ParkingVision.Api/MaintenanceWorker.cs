using ParkingVision.Data;

namespace ParkingVision.Api;

public sealed class MaintenanceWorker(MaintenanceService maintenance, ILogger<MaintenanceWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync(ct))
        {
            try { await maintenance.SweepAsync(ct); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { log.LogError(ex, "Maintenance sweep failed"); }
        }
    }
}
