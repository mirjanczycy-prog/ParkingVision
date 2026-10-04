using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ParkingVision.Core;
using ParkingVision.Maui.Localization;
using ParkingVision.Maui.Services;

namespace ParkingVision.Maui.ViewModels;

/// <summary>Drives both the Map tab and the List tab (same data, two presentations).</summary>
public partial class MapViewModel : ObservableObject
{
    private readonly ParkingApiClient _api;
    private readonly LocationService _location;
    private readonly AppSettings _settings;
    private readonly SearchContext _ctx;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public MapViewModel(ParkingApiClient api, LocationService location, AppSettings settings, SearchContext ctx)
    {
        _api = api; _location = location; _settings = settings; _ctx = ctx;
        Filters = new[]
        {
            new FilterChipVm("filter_all", null, Select) { IsSelected = true },
            new FilterChipVm("filter_car", SpotType.Car, Select),
            new FilterChipVm("filter_moto", SpotType.Motorcycle, Select),
            new FilterChipVm("filter_disabled", SpotType.Disabled, Select),
            new FilterChipVm("filter_ev", SpotType.Electric, Select),
        };
    }

    public ObservableCollection<ZoneItemVm> Zones { get; } = new();
    public IReadOnlyList<FilterChipVm> Filters { get; }
    public (double Lat, double Lon) Center => (_ctx.Lat, _ctx.Lon);
    public int RadiusM => _settings.RadiusM;
    public int RefreshSeconds => _settings.RefreshSeconds;

    /// <summary>Raised after Zones were replaced - the map page redraws circles and pins.</summary>
    public event EventHandler? ZonesUpdated;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string statusText = Loc.Instance.Get("status_searching");
    [ObservableProperty] private string searchText = "";

    private void Select(FilterChipVm chip)
    {
        foreach (var f in Filters) f.IsSelected = ReferenceEquals(f, chip);
        _ctx.Type = chip.Type;
        _ = RefreshAsync();
    }

    private static Task OpenZoneAsync(int id) => Shell.Current.GoToAsync($"zone?id={id}");

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (!await _gate.WaitAsync(0)) return;          // ignore overlapping refreshes (timer + pull-to-refresh)
        try
        {
            IsBusy = true;
            if (!_ctx.HasOverride)
            {
                var loc = await _location.GetCurrentAsync();
                if (loc is null)
                {
                    StatusText = Loc.Instance.Get("status_no_location");
                    return;
                }
                (_ctx.Lat, _ctx.Lon) = loc.Value;
            }

            var zones = await _api.GetZonesAsync(_ctx.Lat, _ctx.Lon, _settings.RadiusM, _ctx.Type);
            Zones.Clear();
            foreach (var z in zones) Zones.Add(new ZoneItemVm(z, OpenZoneAsync));

            StatusText = zones.Count == 0
                ? Loc.Instance.Format("status_none_in_radius", _settings.RadiusM)
                : Loc.Instance.Plural("status_zones_found", zones.Count, zones.Count, DateTime.Now.ToString("HH:mm:ss"));
            ZonesUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            StatusText = Loc.Instance.Format("status_server_error", _settings.ApiBaseUrl);
        }
        finally
        {
            IsBusy = false;
            _gate.Release();
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;
        try
        {
            var found = (await Geocoding.Default.GetLocationsAsync(SearchText))?.FirstOrDefault();
            if (found is null) { StatusText = Loc.Instance.Get("status_address_not_found"); return; }
            _ctx.SetOverride(found.Latitude, found.Longitude);
            await RefreshAsync();
        }
        catch (Exception)
        {
            StatusText = Loc.Instance.Get("status_search_unavailable");
        }
    }

    [RelayCommand]
    private async Task UseMyLocationAsync()
    {
        _ctx.ClearOverride();
        SearchText = "";
        await RefreshAsync();
    }
}
