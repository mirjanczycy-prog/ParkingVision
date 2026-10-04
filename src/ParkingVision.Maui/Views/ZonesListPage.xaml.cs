using ParkingVision.Maui.ViewModels;

namespace ParkingVision.Maui.Views;

public partial class ZonesListPage : ContentPage
{
    private readonly MapViewModel _vm;
    private IDispatcherTimer? _timer;

    public ZonesListPage(MapViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.RefreshAsync();
        _timer ??= Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(_vm.RefreshSeconds);
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
        _timer.Start();
    }

    protected override void OnDisappearing() { base.OnDisappearing(); _timer?.Stop(); }

    private async void OnTick(object? s, EventArgs e) => await _vm.RefreshAsync();
}
