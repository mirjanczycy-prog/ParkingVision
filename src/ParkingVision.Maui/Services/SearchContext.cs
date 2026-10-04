using ParkingVision.Core;

namespace ParkingVision.Maui.Services;

/// <summary>Where the user is looking + vehicle filter. Shared by map, list and detail screens.</summary>
public sealed class SearchContext
{
    public double Lat { get; set; } = AppSettings.DemoLocation.Lat;
    public double Lon { get; set; } = AppSettings.DemoLocation.Lon;
    public SpotType? Type { get; set; }
    public bool HasOverride { get; private set; }

    public void SetOverride(double lat, double lon) { Lat = lat; Lon = lon; HasOverride = true; }
    public void ClearOverride() => HasOverride = false;
}
