using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParkingVision.Core;
using ParkingVision.Data;
using ParkingVision.Simulator;

// Usage:
//   dotnet run --project src/ParkingVision.Simulator -- [run|seed|reset] [--speed 30] [--scenario normal|busy|quiet]
//                                                        [--start-hour 8] [--offline CAM-B2] [--noise 0.01] [--db path]
var cmd = args.Length > 0 && !args[0].StartsWith("--") ? args[0] : "run";
string? Arg(string name) { int i = Array.IndexOf(args, "--" + name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }

var opt = new SimOptions();
if (Arg("speed") is { } sp) opt.Speed = double.Parse(sp, System.Globalization.CultureInfo.InvariantCulture);
if (Arg("start-hour") is { } sh) opt.StartHour = double.Parse(sh, System.Globalization.CultureInfo.InvariantCulture);
if (Arg("noise") is { } nz) opt.CameraNoise = double.Parse(nz, System.Globalization.CultureInfo.InvariantCulture);
if (Arg("scenario") is { } sc) opt.Scenario = Enum.Parse<Scenario>(sc, ignoreCase: true);
if (Arg("offline") is { } off) foreach (var c in off.Split(',')) opt.OfflineCameras.Add(c.Trim());

var services = new ServiceCollection();
services.AddLogging(b => b.AddSimpleConsole(o => { o.TimestampFormat = "HH:mm:ss "; o.SingleLine = true; }).SetMinimumLevel(LogLevel.Information).AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning));
services.AddParkingData(Arg("db"));
services.AddSingleton(opt);
services.AddSingleton<SimulationEngine>();
await using var sp2 = services.BuildServiceProvider();

var seeder = sp2.GetRequiredService<DemoSeeder>();
if (cmd == "reset") { await seeder.ResetAsync(); Console.WriteLine("Database reset."); }
await DbSetup.EnsureDatabaseAsync(sp2);
var seeded = await seeder.SeedIfEmptyAsync();
if (seeded) Console.WriteLine("Demo topology seeded (3 zones, 56 spots, 4 cameras, 6 parkomats).");
if (cmd is "seed" or "reset") return;

var engine = sp2.GetRequiredService<SimulationEngine>();
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

Console.WriteLine("Commands while running: offline <CAM> | online <CAM> | speed <n> | busy | quiet | normal | status | q");
_ = Task.Run(() =>
{
    if (Console.IsInputRedirected) return;
    while (!cts.IsCancellationRequested)
    {
        var line = Console.ReadLine()?.Trim();
        if (line is null) return;
        var p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (p.Length == 0) continue;
        switch (p[0].ToLowerInvariant())
        {
            case "offline" when p.Length > 1: opt.OfflineCameras.Add(p[1]); break;
            case "online" when p.Length > 1: opt.OfflineCameras.Remove(p[1]); break;
            case "speed" when p.Length > 1: opt.Speed = double.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture); break;
            case "busy": opt.Scenario = Scenario.Busy; break;
            case "quiet": opt.Scenario = Scenario.Quiet; break;
            case "normal": opt.Scenario = Scenario.Normal; break;
            case "status": Console.WriteLine(engine.Status()); break;
            case "q": cts.Cancel(); return;
        }
    }
});

await engine.RunAsync(cts.Token);
