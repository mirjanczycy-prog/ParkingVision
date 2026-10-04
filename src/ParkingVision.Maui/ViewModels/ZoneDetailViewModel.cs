using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ParkingVision.Core;
using ParkingVision.Maui.Localization;
using ParkingVision.Maui.Services;

namespace ParkingVision.Maui.ViewModels;

public sealed record SpotTileVm(string ShortCode, string StatusGlyph, string KindGlyph, Color Background);
public sealed record ParkomatRowVm(string Name, string Info);

[QueryProperty(nameof(ZoneId), "id")]
public partial class ZoneDetailViewModel(ParkingApiClient api, SearchContext ctx, AppSettings settings) : ObservableObject
{
    private ZoneDetailDto? _dto;

    [ObservableProperty] private string? zoneId;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? error;

    [ObservableProperty] private string title = "";
    [ObservableProperty] private string tariff = "";
    [ObservableProperty] private string freeText = "–";
    [ObservableProperty] private string totalText = "";
    [ObservableProperty] private string badgeText = "";
    [ObservableProperty] private Color badgeColor = Palette.Unknown;
    [ObservableProperty] private Color accentColor = Palette.Unknown;
    [ObservableProperty] private string updatedText = "";
    [ObservableProperty] private string explainText = "";
    [ObservableProperty] private int barFree;
    [ObservableProperty] private int barTaken;
    [ObservableProperty] private int barUnknown;

    public ObservableCollection<SpotTileVm> Spots { get; } = new();
    public ObservableCollection<ParkomatRowVm> Parkomats { get; } = new();
    public int RefreshSeconds => settings.RefreshSeconds;

    partial void OnZoneIdChanged(string? value) => _ = RefreshAsync();

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (!int.TryParse(ZoneId, out var id)) return;
        try
        {
            IsBusy = true; Error = null;
            var dto = await api.GetZoneAsync(id, ctx.Lat, ctx.Lon, ctx.Type);
            if (dto is null) { Error = Loc.Instance.Get("detail_not_found"); return; }
            Apply(dto);
        }
        catch (Exception)
        {
            Error = Loc.Instance.Get("detail_error");
        }
        finally { IsBusy = false; }
    }

    private void Apply(ZoneDetailDto dto)
    {
        _dto = dto;
        var a = dto.Availability;
        Title = a.Name; Tariff = dto.TariffInfo ?? "";
        FreeText = Presentation.FreeText(a);
        TotalText = Loc.Instance.Plural("detail_free_of", a.Total, a.Total);
        BadgeText = Presentation.Badge(a.Confidence); BadgeColor = Presentation.BadgeColor(a.Confidence);
        AccentColor = Presentation.Accent(a);
        UpdatedText = Loc.Instance.Format("detail_updated", Presentation.Ago(a.UpdatedUtc));
        ExplainText = Presentation.Explain(a);
        BarFree = a.FreeLive; BarTaken = a.OccupiedLive; BarUnknown = a.WithoutLiveData;

        Spots.Clear();
        foreach (var s in dto.Spots)
        {
            var (glyph, color) = s.Status switch
            {
                OccupancyStatus.Free => ("✓", Palette.Free),
                OccupancyStatus.Occupied => ("✕", Palette.Taken),
                _ => ("?", Palette.Unknown)
            };
            var kind = s.Type switch { SpotType.Motorcycle => "M", SpotType.Disabled => "♿", SpotType.Electric => "⚡", _ => "" };
            var shortCode = s.Code.Contains('-') ? s.Code[(s.Code.IndexOf('-') + 1)..] : s.Code;
            Spots.Add(new SpotTileVm(shortCode, glyph, kind, color));
        }

        Parkomats.Clear();
        foreach (var p in dto.Parkomats)
            Parkomats.Add(new ParkomatRowVm(p.Name, Loc.Instance.Plural("parkomat_info", p.SpotCount, Presentation.Distance(p.DistanceM), p.SpotCount).TrimStart(',', ' ')));
    }

    [RelayCommand]
    private async Task NavigateAsync()
    {
        if (_dto is null) return;
        var a = _dto.Availability;
        await Microsoft.Maui.ApplicationModel.Map.Default.OpenAsync(a.Lat, a.Lon,
            new MapLaunchOptions { Name = a.Name, NavigationMode = NavigationMode.Driving });
    }
}
