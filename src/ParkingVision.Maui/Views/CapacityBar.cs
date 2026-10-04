using Microsoft.Maui.Controls.Shapes;

namespace ParkingVision.Maui.Views;

/// <summary>
/// The signature element of the app: a strip of cells, ONE CELL PER SPOT (green free, red taken, grey unknown),
/// like a street seen from above. Zones with more than MaxCells spots are scaled down proportionally.
/// </summary>
public sealed class CapacityBar : ContentView
{
    private const int MaxCells = 40;

    public static readonly BindableProperty FreeProperty = Make(nameof(Free));
    public static readonly BindableProperty TakenProperty = Make(nameof(Taken));
    public static readonly BindableProperty UnknownProperty = Make(nameof(Unknown));

    private static BindableProperty Make(string name) =>
        BindableProperty.Create(name, typeof(int), typeof(CapacityBar), 0, propertyChanged: (b, _, _) => ((CapacityBar)b).Rebuild());

    public int Free { get => (int)GetValue(FreeProperty); set => SetValue(FreeProperty, value); }
    public int Taken { get => (int)GetValue(TakenProperty); set => SetValue(TakenProperty, value); }
    public int Unknown { get => (int)GetValue(UnknownProperty); set => SetValue(UnknownProperty, value); }

    private readonly Grid _grid = new() { ColumnSpacing = 2 };

    public CapacityBar()
    {
        HeightRequest = 12;
        Content = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 4 },
            BackgroundColor = Palette.Track,
            Content = _grid
        };
    }

    private void Rebuild()
    {
        _grid.ColumnDefinitions.Clear();
        _grid.Children.Clear();

        int total = Free + Taken + Unknown;
        if (total <= 0) return;

        double k = total > MaxCells ? MaxCells / (double)total : 1.0;
        int f = (int)Math.Round(Free * k), t = (int)Math.Round(Taken * k), u = (int)Math.Round(Unknown * k);

        int col = 0;
        void Add(int n, Color c)
        {
            for (int i = 0; i < n; i++)
            {
                _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                _grid.Add(new BoxView { Color = c, CornerRadius = 2 }, col++, 0);
            }
        }
        Add(f, Palette.Free);
        Add(t, Palette.Taken);
        Add(u, Palette.Unknown);
    }
}
