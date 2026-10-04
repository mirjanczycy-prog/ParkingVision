namespace ParkingVision.Maui.Localization;

/// <summary>XAML: Text="{loc:T Key=tab_map}" - a binding to Loc.Instance["tab_map"], so it follows language changes.</summary>
[ContentProperty(nameof(Key))]
public sealed class TExtension : IMarkupExtension<BindingBase>
{
    public string Key { get; set; } = "";

    public BindingBase ProvideValue(IServiceProvider serviceProvider) =>
        new Binding($"[{Key}]", BindingMode.OneWay, source: Loc.Instance);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
