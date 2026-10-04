using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using ParkingVision.Maui.Localization;
using ParkingVision.Maui.Services;
using ParkingVision.Maui.ViewModels;
using ParkingVision.Maui.Views;

namespace ParkingVision.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
#if PV_APPLE_MAPS
        builder.UseMauiMaps();      // Apple Maps; Android uses a Leaflet/OSM WebView instead
#endif

        // services (singletons: one settings object, one search context, shared by all screens)
        builder.Services.AddSingleton<AppSettings>();
        builder.Services.AddSingleton<SearchContext>();
        builder.Services.AddSingleton<ParkingApiClient>();
        builder.Services.AddSingleton<LocationService>();

        // view models
        builder.Services.AddSingleton<MapViewModel>();        // shared by Map + List tabs
        builder.Services.AddTransient<ZoneDetailViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();

        // pages
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<ZonesListPage>();
        builder.Services.AddTransient<ZoneDetailPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        var app = builder.Build();
        Loc.Instance.Initialize(app.Services.GetRequiredService<AppSettings>().Language);
        return app;
    }
}
