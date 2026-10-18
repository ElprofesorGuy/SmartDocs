using Microsoft.Maui.Controls.Shapes;
using SmartDocs.Helpers;

namespace SmartDocs.Controls;

/// <summary>Pastille / badge de statut (forme pilule), avec icône optionnelle.</summary>
public class Badge : Border
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(Badge), string.Empty,
            propertyChanged: (b, _, n) => ((Badge)b)._label.Text = (string)n);

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(Badge), Palette.TextSecondary,
            propertyChanged: (b, _, n) => ((Badge)b).ApplyColor((Color)n));

    public static readonly BindableProperty GlyphProperty =
        BindableProperty.Create(nameof(Glyph), typeof(string), typeof(Badge), string.Empty,
            propertyChanged: (b, _, n) =>
            {
                var badge = (Badge)b;
                badge._icon.Text = (string)n;
                badge._icon.IsVisible = !string.IsNullOrEmpty((string)n);
            });

    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(nameof(FontSize), typeof(double), typeof(Badge), 11d,
            propertyChanged: (b, _, n) =>
            {
                var badge = (Badge)b;
                badge._label.FontSize = (double)n;
                badge._icon.FontSize = (double)n + 2;
            });

    private readonly Label _label = new()
    {
        FontFamily = "InterSemiBold",
        FontSize = 11,
        VerticalTextAlignment = TextAlignment.Center,
        VerticalOptions = LayoutOptions.Center
    };

    private readonly Label _icon = new()
    {
        FontFamily = "MaterialIcons",
        FontSize = 13,
        IsVisible = false,
        VerticalTextAlignment = TextAlignment.Center,
        VerticalOptions = LayoutOptions.Center
    };

    public Badge()
    {
        StrokeThickness = 0;
        Stroke = Colors.Transparent;
        StrokeShape = new RoundRectangle { CornerRadius = 999 };
        Padding = new Thickness(8, 3);
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        Content = new HorizontalStackLayout { Spacing = 3, Children = { _icon, _label } };
        ApplyColor(Palette.TextSecondary);
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public Color TextColor { get => (Color)GetValue(TextColorProperty); set => SetValue(TextColorProperty, value); }
    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }

    private void ApplyColor(Color c)
    {
        _label.TextColor = c;
        _icon.TextColor = c;
    }
}
