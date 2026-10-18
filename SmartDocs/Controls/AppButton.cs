using Microsoft.Maui.Controls.Shapes;
using SmartDocs.Helpers;

namespace SmartDocs.Controls;

public enum ButtonVariant { Primary, Outline, OutlinePrimary, Soft, SoftOutline, Danger, DangerOutline }

/// <summary>Bouton des maquettes : icône Material + texte, plusieurs variantes de style.</summary>
public class AppButton : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(AppButton), string.Empty,
            propertyChanged: (b, _, _) => ((AppButton)b).Apply());

    public static readonly BindableProperty GlyphProperty =
        BindableProperty.Create(nameof(Glyph), typeof(string), typeof(AppButton), string.Empty,
            propertyChanged: (b, _, _) => ((AppButton)b).Apply());

    public static readonly BindableProperty VariantProperty =
        BindableProperty.Create(nameof(Variant), typeof(ButtonVariant), typeof(AppButton), ButtonVariant.Primary,
            propertyChanged: (b, _, _) => ((AppButton)b).Apply());

    public static readonly BindableProperty IconAtEndProperty =
        BindableProperty.Create(nameof(IconAtEnd), typeof(bool), typeof(AppButton), false,
            propertyChanged: (b, _, _) => ((AppButton)b).Apply());

    public static readonly BindableProperty CompactProperty =
        BindableProperty.Create(nameof(Compact), typeof(bool), typeof(AppButton), false,
            propertyChanged: (b, _, _) => ((AppButton)b).Apply());

    private readonly Border _border = new();
    private readonly HorizontalStackLayout _row = new()
    {
        Spacing = 8,
        HorizontalOptions = LayoutOptions.Center,
        VerticalOptions = LayoutOptions.Center
    };
    private readonly Label _icon = new()
    {
        FontFamily = "MaterialIcons",
        VerticalOptions = LayoutOptions.Center,
        VerticalTextAlignment = TextAlignment.Center
    };
    private readonly Label _label = new()
    {
        FontFamily = "InterSemiBold",
        VerticalOptions = LayoutOptions.Center,
        VerticalTextAlignment = TextAlignment.Center,
        LineBreakMode = LineBreakMode.TailTruncation,
        MaxLines = 1
    };

    public event EventHandler? Clicked;

    public AppButton()
    {
        _border.StrokeThickness = 1;
        _border.Padding = new Thickness(12, 0);
        _border.Content = _row;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => { if (IsEnabled) Clicked?.Invoke(this, EventArgs.Empty); };
        _border.GestureRecognizers.Add(tap);
        Content = _border;
        Apply();
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public ButtonVariant Variant { get => (ButtonVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public bool IconAtEnd { get => (bool)GetValue(IconAtEndProperty); set => SetValue(IconAtEndProperty, value); }
    public bool Compact { get => (bool)GetValue(CompactProperty); set => SetValue(CompactProperty, value); }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == IsEnabledProperty.PropertyName) Apply();
    }

    private void Apply()
    {
        var (bg, fg, stroke) = Variant switch
        {
            ButtonVariant.Primary => (Palette.Primary, Colors.White, Palette.Primary),
            ButtonVariant.Outline => (Palette.Surface, Palette.TextPrimary, Palette.Border),
            ButtonVariant.OutlinePrimary => (Palette.Surface, Palette.Primary, Palette.Primary),
            ButtonVariant.Soft => (Palette.AccentLight, Palette.Primary, Palette.AccentLight),
            ButtonVariant.SoftOutline => (Palette.AccentLight, Palette.Primary, Palette.AccentBorder),
            ButtonVariant.Danger => (Palette.Danger, Colors.White, Palette.Danger),
            _ => (Palette.Surface, Palette.Danger, Palette.Danger)
        };

        if (!IsEnabled) { bg = Palette.Border; fg = Colors.White; stroke = Palette.Border; }

        var height = Compact ? 36 : 48;
        _border.HeightRequest = height;
        _border.BackgroundColor = bg;
        _border.Stroke = stroke;
        _border.StrokeShape = new RoundRectangle { CornerRadius = Compact ? 8 : 10 };

        _label.Text = Text;
        _label.TextColor = fg;
        _label.FontSize = Compact ? 13 : 15;

        _icon.Text = Glyph;
        _icon.TextColor = fg;
        _icon.FontSize = Compact ? 18 : 20;
        _icon.IsVisible = !string.IsNullOrEmpty(Glyph);

        _row.Children.Clear();
        if (IconAtEnd) { _row.Children.Add(_label); _row.Children.Add(_icon); }
        else { _row.Children.Add(_icon); _row.Children.Add(_label); }
    }
}
