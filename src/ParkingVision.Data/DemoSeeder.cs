using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ParkingVision.Core;

namespace ParkingVision.Data;

/// <summary>
/// Deterministic DEMO topology (fictional streets near Krakow centre). Shows every relation shape:
///  Zone A: 1 camera -> 2 parkomats (overlapping spots)       Zone B: 2 cameras -> 1 parkomat (overlapping spots, motorcycles)
///  Zone C: 1 camera -> 3 parkomats, 4 spots without any camera (ticket-based estimate only)
/// ROI polygons are PLACEHOLDERS (evenly spaced strip). Real deployments need per-camera calibration.
/// </summary>
public sealed class DemoSeeder(IDbContextFactory<ParkingDbContext> factory)
{
    public async Task ResetAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.Database.EnsureDeletedAsync(ct);
        await db.Database.EnsureCreatedAsync(ct);
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", ct);
    }

    public async Task<bool> SeedIfEmptyAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (await db.Zones.AnyAsync(ct)) return false;

        // ---------------- Zone A: street, 12 car spots ----------------
        var a = new Zone { Code = "KRK-A", Name = "Demo: ulica A (Stare Miasto)", City = "Kraków (dane demo)",
                           CenterLat = 50.0650, CenterLon = 19.9390, TariffInfo = "Strefa płatna, 6 zł/h, pn-pt 8-20" };
        var aSpots = Line(a, "A", 12, SpotType.Car, 50.0645, 19.9378, 50.0655, 19.9402, "ul. Demo A");
        var pmA1 = Pm(a, "PM-A1", "Parkomat A1 (róg)", 50.0646, 19.9380);
        var pmA2 = Pm(a, "PM-A2", "Parkomat A2 (środek)", 50.0652, 19.9395);

        // ---------------- Zone B: square, 16 car + 4 motorcycle ----------------
        var b = new Zone { Code = "KRK-B", Name = "Demo: plac B (Kazimierz)", City = "Kraków (dane demo)",
                           CenterLat = 50.0515, CenterLon = 19.9445, TariffInfo = "Strefa płatna, 5 zł/h, codziennie 8-22" };
        var bCars = Grid(b, "B", 16, SpotType.Car, 50.0512, 19.9438, 8, "plac Demo B");
        var bMoto = Grid(b, "B-M", 4, SpotType.Motorcycle, 50.0518, 19.9438, 4, "plac Demo B");
        var pmB1 = Pm(b, "PM-B1", "Parkomat B1", 50.0515, 19.9443);

        // ---------------- Zone C: car park, 20 car + 2 disabled + 2 EV ----------------
        var c = new Zone { Code = "KRK-C", Name = "Demo: parking C (dworzec)", City = "Kraków (dane demo)",
                           CenterLat = 50.0680, CenterLon = 19.9450, TariffInfo = "Parking niestrzeżony, 4 zł/h, całą dobę" };
        var cCars = Grid(c, "C", 20, SpotType.Car, 50.0676, 19.9445, 10, "parking Demo C");
        var cDis = Grid(c, "C-D", 2, SpotType.Disabled, 50.0683, 19.9445, 2, "parking Demo C");
        var cEv = Grid(c, "C-E", 2, SpotType.Electric, 50.0683, 19.9452, 2, "parking Demo C");
        var pmC1 = Pm(c, "PM-C1", "Parkomat C1", 50.0677, 19.9444);
        var pmC2 = Pm(c, "PM-C2", "Parkomat C2", 50.0678, 19.9455);
        var pmC3 = Pm(c, "PM-C3", "Parkomat C3 (wejście)", 50.0684, 19.9449);

        db.Zones.AddRange(a, b, c);
        await db.SaveChangesAsync(ct); // get ids

        // ---------------- Parkomat <-> Spot (N-M) ----------------
        Link(db, pmA1, aSpots.Take(7));                 // A-01..A-07
        Link(db, pmA2, aSpots.Skip(5));                 // A-06..A-12  (A-06, A-07 served by both)
        Link(db, pmB1, bCars.Concat(bMoto));
        Link(db, pmC1, cCars.Take(8));
        Link(db, pmC2, cCars.Skip(8).Take(8));
        Link(db, pmC3, cCars.Skip(16).Concat(cDis).Concat(cEv));

        // ---------------- Camera <-> Spot (N-M, with ROI) ----------------
        var camA1 = Cam("CAM-A1", "Kamera A1 (latarnia)", 50.0649, 19.9388);
        var camB1 = Cam("CAM-B1", "Kamera B1 (północ)", 50.0517, 19.9436);
        var camB2 = Cam("CAM-B2", "Kamera B2 (południe)", 50.0511, 19.9452);
        var camC1 = Cam("CAM-C1", "Kamera C1 (maszt)", 50.0680, 19.9448);
        db.Cameras.AddRange(camA1, camB1, camC1, camB2);
        await db.SaveChangesAsync(ct);

        LinkCam(db, camA1, aSpots);
        LinkCam(db, camB1, bCars.Take(10).Concat(bMoto.Take(2)));        // B-01..B-10, B-M1, B-M2
        LinkCam(db, camB2, bCars.Skip(8).Concat(bMoto.Skip(2)));         // B-09..B-16, B-M3, B-M4 (B-09, B-10 overlap)
        LinkCam(db, camC1, cCars.Take(16).Concat(cDis).Concat(cEv));     // C-17..C-20 deliberately uncovered

        await db.SaveChangesAsync(ct);
        return true;
    }

    // ---------------- helpers ----------------
    private static List<Spot> Line(Zone z, string prefix, int n, SpotType type, double lat1, double lon1, double lat2, double lon2, string street)
    {
        var list = new List<Spot>();
        for (int i = 0; i < n; i++)
        {
            double t = n == 1 ? 0 : i / (double)(n - 1);
            var s = new Spot { Code = $"{prefix}-{i + 1:00}", Type = type, StreetName = street,
                               Lat = lat1 + (lat2 - lat1) * t, Lon = lon1 + (lon2 - lon1) * t };
            z.Spots.Add(s); list.Add(s);
        }
        return list;
    }

    private static List<Spot> Grid(Zone z, string prefix, int n, SpotType type, double lat0, double lon0, int perRow, string street)
    {
        var list = new List<Spot>();
        for (int i = 0; i < n; i++)
        {
            var s = new Spot { Code = $"{prefix}-{i + 1:00}", Type = type, StreetName = street,
                               Lat = lat0 + (i / perRow) * 0.00008, Lon = lon0 + (i % perRow) * 0.00006 };
            z.Spots.Add(s); list.Add(s);
        }
        return list;
    }

    private static Parkomat Pm(Zone z, string code, string name, double lat, double lon)
    {
        var p = new Parkomat { Code = code, Name = name, Lat = lat, Lon = lon };
        z.Parkomats.Add(p);
        return p;
    }

    private static Camera Cam(string code, string name, double lat, double lon) =>
        new() { Code = code, Name = name, Lat = lat, Lon = lon, IsSimulated = true, Enabled = true, StreamUrl = null };

    private static void Link(ParkingDbContext db, Parkomat p, IEnumerable<Spot> spots)
    {
        foreach (var s in spots) db.ParkomatSpots.Add(new ParkomatSpot { ParkomatId = p.Id, SpotId = s.Id });
    }

    private static void LinkCam(ParkingDbContext db, Camera cam, IEnumerable<Spot> spots)
    {
        var list = spots.ToList();
        for (int i = 0; i < list.Count; i++)
            db.CameraSpots.Add(new CameraSpot { CameraId = cam.Id, SpotId = list[i].Id, RoiJson = PlaceholderRoi(i, list.Count), Weight = 1.0 });
    }

    /// <summary>Evenly spaced trapezoids along the lower part of the frame (normalized coords).</summary>
    public static string PlaceholderRoi(int index, int count)
    {
        double margin = 0.03, w = (1 - 2 * margin) / count, x0 = margin + index * w, x1 = x0 + w * 0.92;
        double inset = w * 0.12, yTop = 0.55, yBot = 0.90;
        double[][] poly = { new[] { x0 + inset, yTop }, new[] { x1 - inset, yTop }, new[] { x1, yBot }, new[] { x0, yBot } };
        return JsonSerializer.Serialize(poly);
    }
}
