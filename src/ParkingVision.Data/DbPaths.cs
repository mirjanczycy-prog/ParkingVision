namespace ParkingVision.Data;

public static class DbPaths
{
    /// <summary>Priority: env PV_DB_PATH > explicit config path > &lt;repo root&gt;/data/parkingvision.db (found by walking up to ParkingVision.sln).</summary>
    public static string Resolve(string? configured = null)
    {
        var env = Environment.GetEnvironmentVariable("PV_DB_PATH");
        if (!string.IsNullOrWhiteSpace(env)) return env;
        if (!string.IsNullOrWhiteSpace(configured)) return configured;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ParkingVision.sln")))
            dir = dir.Parent;
        var root = dir?.FullName ?? Directory.GetCurrentDirectory();
        var data = Path.Combine(root, "data");
        Directory.CreateDirectory(data);
        return Path.Combine(data, "parkingvision.db");
    }
}
