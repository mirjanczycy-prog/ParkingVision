using ParkingVision.Maui.Services;
using ParkingVision.Maui.ViewModels;

namespace ParkingVision.Maui.Views.Maps;

/// <summary>The map shown on the Map tab. One implementation per platform family (see ZoneMapFactory).</summary>
public interface IZoneMap
{
    View Control { get; }

    /// <summary>User tapped "Details" on a zone.</summary>
    event Action<int>? ZoneOpened;

    /// <summary>Called when the page appears (the Leaflet map (re)loads its page here, e.g. after a language change).</summary>
    void Prepare();

    /// <summary>Draws the zones and, when the search centre or the zone count changed, re-frames the view.</summary>
    void Show(IReadOnlyList<ZoneItemVm> zones, double lat, double lon, int radiusM);
}

public static class ZoneMapFactory
{
    public static IZoneMap Create(AppSettings settings)
    {
#if PV_APPLE_MAPS
        return new AppleZoneMap();
#else
        return new LeafletZoneMap(settings);
#endif
    }
}
