namespace ParkingVision.Maui.Services;

public sealed class LocationService(AppSettings settings)
{
    public async Task<(double Lat, double Lon)?> GetCurrentAsync()
    {
        if (settings.UseDemoLocation) return AppSettings.DemoLocation;
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted) return null;
            var loc = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8)));
            return loc is null ? null : (loc.Latitude, loc.Longitude);
        }
        catch { return null; }
    }
}
