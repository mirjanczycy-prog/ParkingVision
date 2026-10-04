#if PV_APPLE_MAPS
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using ParkingVision.Core;
using ParkingVision.Maui.ViewModels;

namespace ParkingVision.Maui.Views.Maps;

/// <summary>Apple Maps (iOS / Mac Catalyst): no account or key needed.</summary>
public sealed class AppleZoneMap : IZoneMap
{
    private readonly Microsoft.Maui.Controls.Maps.Map _map = new() { MapType = MapType.Street };
    private string? _lastKey;

    public View Control => _map;
    public event Action<int>? ZoneOpened;

    public void Prepare() { }

    public void Show(IReadOnlyList<ZoneItemVm> zones, double lat, double lon, int radiusM)
    {
        _map.MapElements.Clear();
        _map.Pins.Clear();

        foreach (var item in zones)
        {
            var z = item.Dto;
            var loc = new Location(z.Lat, z.Lon);
            var color = item.AccentColor;

            _map.MapElements.Add(new Circle
            {
                Center = loc, Radius = Distance.FromMeters(70),
                StrokeColor = color, StrokeWidth = 4, FillColor = color.WithAlpha(0.40f)
            });

            var pin = new Pin { Label = z.Name, Address = $"{item.FreeText} / {z.Total} · {item.BadgeText}", Location = loc, Type = PinType.Place };
            var id = z.ZoneId;
            pin.InfoWindowClicked += (_, _) => ZoneOpened?.Invoke(id);
            _map.Pins.Add(pin);
        }

        // Re-frame only when the search centre or the number of zones changed - not on every refresh.
        var key = FormattableString.Invariant($"{lat:F5},{lon:F5},{zones.Count}");
        if (key == _lastKey) return;
        _lastKey = key;
        double farthest = zones.Count == 0 ? radiusM : zones.Max(zn => Geo.DistanceM(lat, lon, zn.Dto.Lat, zn.Dto.Lon));
        _map.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(lat, lon), Distance.FromMeters(Math.Max(300, farthest * 1.3))));
    }
}
#endif
