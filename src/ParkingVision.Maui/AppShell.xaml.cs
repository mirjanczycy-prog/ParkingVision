using ParkingVision.Maui.Views;

namespace ParkingVision.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("zone", typeof(ZoneDetailPage));   // GoToAsync("zone?id=3")
    }
}
