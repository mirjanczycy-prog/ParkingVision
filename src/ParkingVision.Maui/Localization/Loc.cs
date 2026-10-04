using System.ComponentModel;
using System.Globalization;
using ParkingVision.Core.Localization;

namespace ParkingVision.Maui.Localization;

/// <summary>
/// Runtime language switch. XAML binds to the indexer (see TExtension) so every label updates when the language changes;
/// code-behind / view models call Get/Format/Plural. Strings live in ParkingVision.Core.Localization.Strings (unit-tested).
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static Loc Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Language { get; private set; } = "pl";

    public string this[string key] => Get(key);

    public void Initialize(string? language) => Language = Normalize(language);

    public void SetLanguage(string? language)
    {
        var n = Normalize(language);
        if (n == Language) return;
        Language = n;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
        // An empty property name means "everything changed". It is what reliably refreshes the indexer bindings behind
        // every {loc:T ...}; raising only "Item[]" left the XAML labels in the old language.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    private static string Normalize(string? l) => string.Equals(l, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "pl";

    private CultureInfo Culture => CultureInfo.GetCultureInfo(Language == "en" ? "en-US" : "pl-PL");

    public string Get(string key) =>
        Strings.Table.TryGetValue(key, out var v) ? (Language == "en" ? v.En : v.Pl) : key;

    public string Format(string key, params object[] args) => string.Format(Culture, Get(key), args);

    /// <summary>Picks key|one / |few / |many for <paramref name="n"/> (missing |few falls back to |many), then formats with <paramref name="args"/>.</summary>
    public string Plural(string key, long n, params object[] args)
    {
        var k = $"{key}|{PluralRules.Category(Language, n)}";
        if (!Strings.Table.ContainsKey(k)) k = $"{key}|many";
        return string.Format(Culture, Get(k), args);
    }
}
