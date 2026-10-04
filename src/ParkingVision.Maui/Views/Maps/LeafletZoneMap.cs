using System.Text.Json;
using ParkingVision.Maui.Localization;
using ParkingVision.Maui.Services;
using ParkingVision.Maui.ViewModels;

namespace ParkingVision.Maui.Views.Maps;

/// <summary>OpenStreetMap through Leaflet in a WebView (used on Android). Page: Services/MapHtml.cs.</summary>
public sealed class LeafletZoneMap : IZoneMap
{
    private sealed record MapState(IReadOnlyList<ZoneItemVm> Zones, double Lat, double Lon, int RadiusM);

    private readonly AppSettings _settings;
    private readonly WebView _web = new();
    private bool _ready;
    private string? _language;
    private MapState? _last;

    public LeafletZoneMap(AppSettings settings)
    {
        _settings = settings;
        _web.Navigating += OnNavigating;
        _web.Navigated += (_, _) => { _ready = true; if (_last is not null) _ = PushAsync(_last); };
    }

    public View Control => _web;
    public event Action<int>? ZoneOpened;

    public void Prepare()
    {
        if (_language == Loc.Instance.Language) return;   // the offline message in the page is localized
        _language = Loc.Instance.Language;
        _ready = false;
        _web.Source = new HtmlWebViewSource
        {
            Html = MapHtml.Build(_settings.MapTileUrl, Loc.Instance.Get("map_offline")),
            BaseUrl = AppSettings.MapBaseUrl
        };
    }

    public void Show(IReadOnlyList<ZoneItemVm> zones, double lat, double lon, int radiusM)
    {
        _last = new MapState(zones, lat, lon, radiusM);
        if (_ready) _ = PushAsync(_last);
    }

    /// <summary>A tap on "Details" in a popup is a link to pvapp://zone?id=N - intercept it and cancel the navigation.</summary>
    private void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!e.Url.StartsWith("pvapp://zone", StringComparison.OrdinalIgnoreCase)) return;
        e.Cancel = true;
        var i = e.Url.IndexOf("id=", StringComparison.OrdinalIgnoreCase);
        if (i >= 0 && int.TryParse(e.Url[(i + 3)..].TrimEnd('/'), out var id)) ZoneOpened?.Invoke(id);
    }

    private async Task PushAsync(MapState s)
    {
        var zones = s.Zones.Select(z => new
        {
            id = z.Dto.ZoneId, name = z.Name, lat = z.Dto.Lat, lon = z.Dto.Lon,
            color = Presentation.Hex(z.AccentColor), free = z.FreeText, total = z.Dto.Total, badge = z.BadgeText
        });
        var labels = new { details = Loc.Instance.Get("map_details") };
        var js = FormattableString.Invariant(
            $"pvSetZones({JsonSerializer.Serialize(zones)},{s.Lat},{s.Lon},{s.RadiusM},{JsonSerializer.Serialize(labels)});");
        try { await _web.EvaluateJavaScriptAsync(js); }
        catch { /* map not available (offline) - the list below keeps working */ }
    }
}
