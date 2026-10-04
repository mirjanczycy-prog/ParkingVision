using ParkingVision.Maui.Services;
using ParkingVision.Maui.ViewModels;
using ParkingVision.Maui.Views.Maps;

namespace ParkingVision.Maui.Views;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _vm;
    private readonly IZoneMap _map;
    private IDispatcherTimer? _timer;

    public MapPage(MapViewModel vm, AppSettings settings)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        _map = ZoneMapFactory.Create(settings);          // Apple Maps on iOS, Leaflet/OSM on Android
        MapHost.Content = _map.Control;
        _map.ZoneOpened += id => MainThread.BeginInvokeOnMainThread(() => _ = Shell.Current.GoToAsync($"zone?id={id}"));
        _vm.ZonesUpdated += (_, _) => MainThread.BeginInvokeOnMainThread(PushZones);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _map.Prepare();
        await _vm.RefreshAsync();

        _timer ??= Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(_vm.RefreshSeconds);
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
    }

    private async void OnTick(object? sender, EventArgs e) => await _vm.RefreshAsync();

    private void PushZones()
    {
        var c = _vm.Center;
        _map.Show(_vm.Zones.ToList(), c.Lat, c.Lon, _vm.RadiusM);
    }
}
