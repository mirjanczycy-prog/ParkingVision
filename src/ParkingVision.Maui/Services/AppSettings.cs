namespace ParkingVision.Maui.Services;

/// <summary>User-editable settings persisted with MAUI Preferences.</summary>
public sealed class AppSettings
{
    // Android emulator reaches the host machine at 10.0.2.2. iOS simulator uses localhost. A physical phone needs the PC's LAN IP.
    private static string DefaultUrl => DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5080" : "http://localhost:5080";

    /// <summary>Middle of the three demo zones (fictional data around central Krakow). Used when "demo location" is on.</summary>
    public static (double Lat, double Lon) DemoLocation => (50.0600, 19.9410);

    /// <summary>Base URL given to the map WebView so tile requests carry a Referer (tile servers require an identifying Referer/User-Agent).</summary>
    public const string MapBaseUrl = "https://parkingvision.example/";

    public string ApiBaseUrl
    {
        get => Preferences.Get(nameof(ApiBaseUrl), DefaultUrl);
        set => Preferences.Set(nameof(ApiBaseUrl), value.Trim().TrimEnd('/'));
    }
    public int RadiusM
    {
        get => Preferences.Get(nameof(RadiusM), 2000);
        set => Preferences.Set(nameof(RadiusM), value);
    }
    public int RefreshSeconds
    {
        get => Preferences.Get(nameof(RefreshSeconds), 10);
        set => Preferences.Set(nameof(RefreshSeconds), Math.Max(3, value));
    }
    public bool UseDemoLocation
    {
        get => Preferences.Get(nameof(UseDemoLocation), true);
        set => Preferences.Set(nameof(UseDemoLocation), value);
    }

    /// <summary>UI language: "pl" (default) or "en". Not taken from the device on purpose - see AGENTS.md A-017.</summary>
    public string Language
    {
        get => Preferences.Get(nameof(Language), "pl");
        set => Preferences.Set(nameof(Language), value);
    }

    /// <summary>Raster tile URL template. Default: public OpenStreetMap tiles (fair-use policy applies; use another provider for real traffic).</summary>
    public string MapTileUrl
    {
        get => Preferences.Get(nameof(MapTileUrl), "https://tile.openstreetmap.org/{z}/{x}/{y}.png");
        set => Preferences.Set(nameof(MapTileUrl), value);
    }
}
