using SmartDocs.Helpers;

namespace SmartDocs.Models;

/// <summary>Pastille de filtre (catégorie ou année).</summary>
public class ChipItem : ObservableObject
{
    private bool _isSelected;

    public ChipItem(string? key, string display, bool isSelected = false)
    {
        Key = key;
        Display = display;
        _isSelected = isSelected;
    }

    public string? Key { get; }
    public string Display { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(Fill));
                OnPropertyChanged(nameof(Foreground));
                OnPropertyChanged(nameof(Outline));
            }
        }
    }

    public Color Fill => IsSelected ? Palette.Primary : Palette.Surface;
    public Color Foreground => IsSelected ? Colors.White : Palette.TextPrimary;
    public Color Outline => IsSelected ? Palette.Primary : Palette.Border;
}
