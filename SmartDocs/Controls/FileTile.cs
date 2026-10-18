using Microsoft.Maui.Controls.Shapes;
using SmartDocs.Helpers;

namespace SmartDocs.Controls;

/// <summary>Tuile carrée arrondie avec l'icône du type de fichier (PDF / DOCX / XLSX) et, en option, son extension.</summary>
public class FileTile : Border
{
    public static readonly BindableProperty GlyphProperty =
        BindableProperty.Create(nameof(Glyph), typeof(string), typeof(FileTile), string.Empty,
            propertyChanged: (b, _, n) => ((FileTile)b)._icon.Text = (string)n);

    public static readonly BindableProperty FgProperty =
        BindableProperty.Create(nameof(Fg), typeof(Color), typeof(FileTile), Palette.Primary,
            propertyChanged: (b, _, n) =>
            {
                var t = (FileTile)b;
                t._icon.TextColor = (Color)n;
                t._ext.TextColor = (Color)n;
            });

    public static readonly BindableProperty BgProperty =
        BindableProperty.Create(nameof(Bg), typeof(Color), typeof(FileTile), Palette.AccentLight,
            propertyChanged: (b, _, n) => ((FileTile)b).BackgroundColor = (Color)n);

    public static readonly BindableProperty ExtProperty =
        BindableProperty.Create(nameof(Ext), typeof(string), typeof(FileTile), string.Empty,
            propertyChanged: (b, _, n) => ((FileTile)b)._ext.Text = (string)n);

    public static readonly BindableProperty ShowExtProperty =
        BindableProperty.Create(nameof(ShowExt), typeof(bool), typeof(FileTile), false,
            propertyChanged: (b, _, n) => ((FileTile)b)._ext.IsVisible = (bool)n);

    private readonly Label _icon = new()
    {
        FontFamily = "MaterialIcons",
        FontSize = 24,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center
    };

    private readonly Label _ext = new()
    {
        FontFamily = "InterBold",
        FontSize = 9,
        IsVisible = false,
        HorizontalTextAlignment = TextAlignment.Center
    };

    public FileTile()
    {
        WidthRequest = 44;
        HeightRequest = 44;
        StrokeThickness = 0;
        Stroke = Colors.Transparent;
        StrokeShape = new RoundRectangle { CornerRadius = 10 };
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Start;
        BackgroundColor = Palette.AccentLight;
        Content = new VerticalStackLayout
        {
            Spacing = 0,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Children = { _icon, _ext }
        };
    }

    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public Color Fg { get => (Color)GetValue(FgProperty); set => SetValue(FgProperty, value); }
    public Color Bg { get => (Color)GetValue(BgProperty); set => SetValue(BgProperty, value); }
    public string Ext { get => (string)GetValue(ExtProperty); set => SetValue(ExtProperty, value); }
    public bool ShowExt { get => (bool)GetValue(ShowExtProperty); set => SetValue(ShowExtProperty, value); }
}
