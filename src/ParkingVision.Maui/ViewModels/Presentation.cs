using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ParkingVision.Core;
using ParkingVision.Maui.Localization;

namespace ParkingVision.Maui.ViewModels;

/// <summary>Shared wording + colour rules. Texts come from the localized string table (Core/Localization/Strings.cs).</summary>
public static class Presentation
{
    private static Loc L => Loc.Instance;

    public static string Badge(AvailabilityConfidence c) => L.Get(c switch
    {
        AvailabilityConfidence.Live => "badge_live",
        AvailabilityConfidence.Mixed => "badge_mixed",
        AvailabilityConfidence.Estimated => "badge_estimated",
        _ => "badge_nodata"
    });

    public static Color BadgeColor(AvailabilityConfidence c) => c switch
    {
        AvailabilityConfidence.Live => Palette.Free,
        AvailabilityConfidence.Mixed => Palette.Warn,
        _ => Palette.Unknown
    };

    /// <summary>green >= 30% free, amber 10-30%, red < 10%, grey when we know nothing.</summary>
    public static Color Accent(ZoneAvailabilityDto z)
    {
        if (z.Confidence == AvailabilityConfidence.NoData || z.Total == 0) return Palette.Unknown;
        double ratio = z.FreeTotalEstimate / (double)z.Total;
        return ratio >= 0.30 ? Palette.Free : ratio >= 0.10 ? Palette.Warn : Palette.Taken;
    }

    public static string FreeText(ZoneAvailabilityDto z) => z.Confidence switch
    {
        AvailabilityConfidence.NoData => "–",
        AvailabilityConfidence.Live => z.FreeTotalEstimate.ToString(),
        _ => "~" + z.FreeTotalEstimate
    };

    public static string Distance(double? m) => m switch
    {
        null => "",
        < 1000 => $"{m:0} m",
        _ => $"{m / 1000:0.0} km"
    };

    public static string Ago(DateTime? utc)
    {
        if (utc is null) return L.Get("ago_none");
        var s = (DateTime.UtcNow - utc.Value).TotalSeconds;
        return s < 5 ? L.Get("ago_now")
             : s < 90 ? L.Format("ago_sec", (int)s)
             : s < 3600 ? L.Format("ago_min", (int)(s / 60))
             : L.Get("ago_hour");
    }

    public static string Explain(ZoneAvailabilityDto z) => z.Confidence switch
    {
        AvailabilityConfidence.Live => L.Get("explain_live"),
        AvailabilityConfidence.Mixed => L.Format("explain_mixed", z.FreeLive + z.OccupiedLive, z.Total, z.WithoutLiveData),
        AvailabilityConfidence.Estimated => L.Get("explain_estimated"),
        _ => L.Get("explain_nodata")
    };

    /// <summary>"#RRGGBB" for the map page (JavaScript).</summary>
    public static string Hex(Color c) =>
        $"#{(int)Math.Round(c.Red * 255):X2}{(int)Math.Round(c.Green * 255):X2}{(int)Math.Round(c.Blue * 255):X2}";
}

/// <summary>One card in the list / map sheet.</summary>
public sealed class ZoneItemVm
{
    public ZoneAvailabilityDto Dto { get; }
    public ZoneItemVm(ZoneAvailabilityDto dto, Func<int, Task> open)
    {
        Dto = dto;
        OpenCommand = new AsyncRelayCommand(() => open(dto.ZoneId));
    }

    public IAsyncRelayCommand OpenCommand { get; }
    public string Name => Dto.Name;
    public string DistanceText => Presentation.Distance(Dto.DistanceM);
    public string BadgeText => Presentation.Badge(Dto.Confidence);
    public Color BadgeColor => Presentation.BadgeColor(Dto.Confidence);
    public Color AccentColor => Presentation.Accent(Dto);
    public string FreeText => Presentation.FreeText(Dto);
    public string TotalText => Loc.Instance.Plural("total_of", Dto.Total, Dto.Total);
    public string UpdatedText => Presentation.Ago(Dto.UpdatedUtc);
    public int FreeLive => Dto.FreeLive;
    public int OccupiedLive => Dto.OccupiedLive;
    public int WithoutLive => Dto.WithoutLiveData;
}

/// <summary>Vehicle filter chip. The label follows the UI language.</summary>
public partial class FilterChipVm : ObservableObject
{
    private readonly string _labelKey;

    public FilterChipVm(string labelKey, SpotType? type, Action<FilterChipVm> onSelect)
    {
        _labelKey = labelKey; Type = type;
        SelectCommand = new RelayCommand(() => onSelect(this));
        Loc.Instance.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Label));
    }

    public string Label => Loc.Instance.Get(_labelKey);
    public SpotType? Type { get; }
    public IRelayCommand SelectCommand { get; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(Background), nameof(Foreground))]
    private bool isSelected;

    public Color Background => IsSelected ? Palette.Asphalt : Palette.ChipOff;
    public Color Foreground => IsSelected ? Palette.Paint : Palette.Asphalt;
}
