using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ParkingVision.Maui.Localization;
using ParkingVision.Maui.Services;

namespace ParkingVision.Maui.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly ParkingApiClient _api;
    private readonly MapViewModel _map;

    public SettingsViewModel(AppSettings settings, ParkingApiClient api, MapViewModel map)
    {
        _settings = settings; _api = api; _map = map;
        apiBaseUrl = settings.ApiBaseUrl;
        radiusM = settings.RadiusM;
        refreshSeconds = settings.RefreshSeconds;
        useDemoLocation = settings.UseDemoLocation;
        languageIndex = settings.Language == "en" ? 1 : 0;
        Loc.Instance.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(RadiusLabel));
            OnPropertyChanged(nameof(RefreshLabel));
            OnPropertyChanged(nameof(LanguageLabel));
        };
    }

    /// <summary>Names are shown in their own language on purpose.</summary>
    public IReadOnlyList<string> Languages { get; } = new[] { "Polski", "English" };

    [ObservableProperty] private string apiBaseUrl = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(RadiusLabel))] private double radiusM;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(RefreshLabel))] private double refreshSeconds;
    [ObservableProperty] private bool useDemoLocation;
    [ObservableProperty] private int languageIndex;
    [ObservableProperty] private string connectionStatus = "";

    public string RadiusLabel => Loc.Instance.Format("set_radius", (int)Math.Round(RadiusM));
    public string LanguageLabel => Loc.Instance.Get("set_language");
    public string RefreshLabel => Loc.Instance.Format("set_refresh", (int)Math.Round(RefreshSeconds));

    /// <summary>Applied immediately (no Save needed): labels switch, lists are rebuilt in the new language.</summary>
    partial void OnLanguageIndexChanged(int value)
    {
        if (value < 0) return;                       // Picker may briefly report -1 while loading
        var code = value == 1 ? "en" : "pl";
        _settings.Language = code;
        Loc.Instance.SetLanguage(code);
        _ = _map.RefreshAsync();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        _settings.ApiBaseUrl = ApiBaseUrl;
        _settings.RadiusM = (int)Math.Round(RadiusM);
        _settings.RefreshSeconds = (int)Math.Round(RefreshSeconds);
        _settings.UseDemoLocation = UseDemoLocation;
        ConnectionStatus = Loc.Instance.Get("set_saved");
        await _map.RefreshAsync();
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        _settings.ApiBaseUrl = ApiBaseUrl;           // test what is typed, not what was saved before
        ConnectionStatus = Loc.Instance.Get("set_checking");
        ConnectionStatus = await _api.PingAsync() ? Loc.Instance.Get("set_connected") : Loc.Instance.Get("set_not_connected");
    }
}
