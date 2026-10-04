using ParkingVision.Maui.ViewModels;

namespace ParkingVision.Maui.Views;

public partial class ZoneDetailPage : ContentPage
{
    private readonly ZoneDetailViewModel _vm;
    private IDispatcherTimer? _timer;

    public ZoneDetailPage(ZoneDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _timer ??= Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(_vm.RefreshSeconds);
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
        _timer.Start();
    }

    protected override void OnDisappearing() { base.OnDisappearing(); _timer?.Stop(); }

    private async void OnTick(object? s, EventArgs e) => await _vm.RefreshAsync();
}
