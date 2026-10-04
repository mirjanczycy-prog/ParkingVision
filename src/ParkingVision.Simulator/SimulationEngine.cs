using Microsoft.EntityFrameworkCore;
using ParkingVision.Core;
using ParkingVision.Data;

namespace ParkingVision.Simulator;

/// <summary>
/// Drives the SAME ingest services the real camera worker / parkomat integration use, so the platform and apps
/// cannot tell simulated data from real data. One "agent" per spot (truth) + one per camera (noisy observer).
/// </summary>
public sealed class SimulationEngine(IDbContextFactory<ParkingDbContext> factory, IObservationIngest obsIngest,
                                     IParkomatIngest ticketIngest, SimOptions opt, ILogger<SimulationEngine> log)
{
    private sealed class SpotAgent
    {
        public required string Code; public required SpotType Type; public required string ZoneCode;
        public required List<string> ParkomatCodes;
        public bool Occupied; public DateTime DepartsAt;
    }

    private sealed class CamAgent
    {
        public required string Code; public required List<string> SpotCodes;
        public DateTime LastKeepAlive = DateTime.MinValue;
        public Dictionary<string, (bool Reported, bool? Pending, DateTime Since)> Spots = new();
    }

    private readonly Random _rng = new();
    private readonly Dictionary<string, SpotAgent> _spots = new();
    private readonly List<CamAgent> _cams = new();
    private DateTime _realStart;
    private int _ticketSeq;

    public double SimHour(DateTime nowUtc) => (opt.StartHour + (nowUtc - _realStart).TotalHours * opt.Speed) % 24.0;

    public async Task RunAsync(CancellationToken ct)
    {
        await LoadTopologyAsync(ct);
        _realStart = DateTime.UtcNow;
        await WarmStartAsync(ct);

        log.LogInformation("Simulation running: {Spots} spots, {Cams} cameras, speed x{Speed}, scenario {Sc}",
            _spots.Count, _cams.Count, opt.Speed, opt.Scenario);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(ct))
        {
            try { await TickAsync(DateTime.UtcNow, ct); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { log.LogError(ex, "Tick failed"); }
        }
    }

    public string Status()
    {
        var now = DateTime.UtcNow;
        int occ = _spots.Values.Count(s => s.Occupied);
        return $"sim time {SimHour(now):00.0}h | occupied {occ}/{_spots.Count} | speed x{opt.Speed} | scenario {opt.Scenario} | offline: [{string.Join(",", opt.OfflineCameras)}]";
    }

    // ------------------------------------------------------------------ setup
    private async Task LoadTopologyAsync(CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var spots = await db.Spots.AsNoTracking().Include(s => s.Zone).Include(s => s.Parkomats).ThenInclude(p => p.Parkomat).ToListAsync(ct);
        foreach (var s in spots)
            _spots[s.Code] = new SpotAgent { Code = s.Code, Type = s.Type, ZoneCode = s.Zone.Code,
                ParkomatCodes = s.Parkomats.Select(p => p.Parkomat.Code).ToList() };

        var cams = await db.Cameras.AsNoTracking().Where(c => c.Enabled && c.IsSimulated)
            .Include(c => c.Spots).ThenInclude(cs => cs.Spot).ToListAsync(ct);
        foreach (var c in cams)
            _cams.Add(new CamAgent { Code = c.Code, SpotCodes = c.Spots.Where(cs => cs.Enabled).Select(cs => cs.Spot.Code).ToList() });
    }

    private async Task WarmStartAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var tickets = new List<ParkomatTicketDto>();
        foreach (var s in _spots.Values)
        {
            if (_rng.NextDouble() >= Target(s, now)) continue;
            s.Occupied = true;
            double remainingSimMin = Exp(opt.MeanStayMinutes / 2) + 5;
            s.DepartsAt = now.AddMinutes(remainingSimMin / opt.Speed);
            var t = MaybeTicket(s, now.AddMinutes(-_rng.NextDouble() * 20 / opt.Speed), s.DepartsAt);
            if (t != null) tickets.Add(t);
        }
        await ticketIngest.IngestAsync(tickets, ct);

        // cameras start knowing the truth -> app has data immediately
        foreach (var c in _cams)
        {
            if (opt.OfflineCameras.Contains(c.Code)) continue;
            foreach (var code in c.SpotCodes) c.Spots[code] = (_spots[code].Occupied, null, now);
            await SendCameraAsync(c, c.SpotCodes, now, ct);
            c.LastKeepAlive = now;
        }
    }

    // ------------------------------------------------------------------ tick
    private async Task TickAsync(DateTime now, CancellationToken ct)
    {
        var newTickets = new List<ParkomatTicketDto>();
        double simMinPerTick = opt.Speed / 60.0;

        foreach (var s in _spots.Values)
        {
            if (s.Occupied)
            {
                if (now >= s.DepartsAt) s.Occupied = false;
            }
            else
            {
                double target = Math.Clamp(Target(s, now), 0.01, 0.97);
                double lambda = target / ((1 - target) * opt.MeanStayMinutes);   // arrivals per sim minute (steady-state occupancy = target)
                double p = 1 - Math.Exp(-lambda * simMinPerTick);
                if (_rng.NextDouble() < p)
                {
                    s.Occupied = true;
                    double stay = Math.Clamp(Exp(opt.MeanStayMinutes), 10, 360);   // sim minutes
                    s.DepartsAt = now.AddMinutes(stay / opt.Speed);
                    var t = MaybeTicket(s, now, s.DepartsAt);
                    if (t != null) newTickets.Add(t);
                }
            }
        }
        if (newTickets.Count > 0) await ticketIngest.IngestAsync(newTickets, ct);

        foreach (var c in _cams)
        {
            if (opt.OfflineCameras.Contains(c.Code)) continue;
            var due = new List<string>();
            bool keepAlive = (now - c.LastKeepAlive).TotalSeconds >= opt.CameraKeepAliveSeconds;

            foreach (var code in c.SpotCodes)
            {
                bool truth = _spots[code].Occupied;
                var st = c.Spots.GetValueOrDefault(code, (!truth, null, now));   // first time: force a report
                if (!c.Spots.ContainsKey(code)) { c.Spots[code] = (truth, null, now); due.Add(code); continue; }

                if (truth == st.Reported) { c.Spots[code] = (st.Reported, null, now); }
                else if (st.Pending != truth) { c.Spots[code] = (st.Reported, truth, now); }
                else if ((now - st.Since).TotalSeconds >= opt.CameraDelaySeconds) { c.Spots[code] = (truth, null, now); due.Add(code); }
            }

            if (keepAlive) { due = c.SpotCodes.ToList(); c.LastKeepAlive = now; }
            if (due.Count > 0) await SendCameraAsync(c, due.Distinct().ToList(), now, ct);
        }
    }

    private Task SendCameraAsync(CamAgent c, List<string> spotCodes, DateTime now, CancellationToken ct)
    {
        var items = new List<ObservationItemDto>();
        foreach (var code in spotCodes)
        {
            bool reported = c.Spots.TryGetValue(code, out var st) ? st.Reported : _spots[code].Occupied;
            if (opt.CameraNoise > 0 && _rng.NextDouble() < opt.CameraNoise) reported = !reported;
            double conf = reported ? 0.70 + _rng.NextDouble() * 0.25 : 0.80 + _rng.NextDouble() * 0.15;
            items.Add(new ObservationItemDto(code, reported, Math.Round(conf, 3), reported ? PickVehicle(_spots[code].Type) : null));
        }
        return obsIngest.IngestAsync(new ObservationBatchDto(c.Code, now, items), ct);
    }

    // ------------------------------------------------------------------ model
    /// <summary>Target occupancy (0..1) by simulated hour of day, spot type and scenario.</summary>
    private double Target(SpotAgent s, DateTime nowUtc)
    {
        double[] curve = { .15,.12,.10,.10,.12,.18, .30,.50,.75,.85,.88,.90, .90,.88,.85,.85,.88,.90, .85,.70,.55,.40,.30,.20 };
        double h = SimHour(nowUtc);
        int h0 = (int)h % 24, h1 = (h0 + 1) % 24;
        double v = curve[h0] + (curve[h1] - curve[h0]) * (h - Math.Floor(h));
        v *= s.Type switch { SpotType.Motorcycle => 0.6, SpotType.Disabled => 0.5, SpotType.Electric => 0.7, _ => 1.0 };
        v = opt.Scenario switch { Scenario.Busy => Math.Max(v, 0.95), Scenario.Quiet => v * 0.25, _ => v };
        return Math.Clamp(v, 0, 0.97);
    }

    private ParkomatTicketDto? MaybeTicket(SpotAgent s, DateTime issuedUtc, DateTime departsAt)
    {
        if (s.ParkomatCodes.Count == 0 || _rng.NextDouble() > opt.PayRate) return null;
        var real = departsAt - issuedUtc;
        double f = 1 + (_rng.NextDouble() * 2 - 1) * opt.TicketOverUnder;
        var validTo = issuedUtc + real * f;
        decimal simHours = (decimal)(real.TotalMinutes * opt.Speed / 60.0);
        decimal amount = Math.Max(1m, Math.Ceiling(simHours * 2) / 2 * opt.PricePerHour);
        var pm = s.ParkomatCodes[_rng.Next(s.ParkomatCodes.Count)];
        return new ParkomatTicketDto(pm, $"SIM-{DateTime.UtcNow:yyMMddHHmmss}-{Interlocked.Increment(ref _ticketSeq):0000}",
                                     issuedUtc, validTo, amount);
    }

    private string PickVehicle(SpotType t) => t == SpotType.Motorcycle ? "motorcycle" : _rng.NextDouble() < 0.9 ? "car" : "truck";

    private double Exp(double mean) => -mean * Math.Log(1 - _rng.NextDouble());
}
