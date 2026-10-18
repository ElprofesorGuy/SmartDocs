using System.Globalization;
using SmartDocs.Helpers;

namespace SmartDocs.Models;

public class DocumentItem : ObservableObject
{
    private static readonly CultureInfo Fr = new("fr-FR");
    private bool _isArchived;
    private bool _isFavorite;
    private string _currentVersion = "1.0";
    private int _versionsCount = 1;
    private double _sizeKb;
    private DateTime _modifiedAt;

    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string CategoryPath { get; set; } = "";
    public string Extension { get; set; } = "pdf";            // pdf | docx | xlsx
    public string Description { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Owner { get; set; } = "";
    public string OwnerRole { get; set; } = "";
    public string ModifiedInfo { get; set; } = "";             // « il y a 2h par Claire D. »
    public string Confidentiality { get; set; } = "Interne";
    public List<string> Tags { get; set; } = new();
    public int RecentRank { get; set; }                        // >0 : apparaît dans « Documents récents »
    public DateTime? ArchivedAt { get; set; }

    public double SizeKb { get => _sizeKb; set => _sizeKb = value; }
    public DateTime ModifiedAt { get => _modifiedAt; set => _modifiedAt = value; }

    public string CurrentVersion
    {
        get => _currentVersion;
        set { if (SetProperty(ref _currentVersion, value)) Refresh(); }
    }

    public int VersionsCount
    {
        get => _versionsCount;
        set { if (SetProperty(ref _versionsCount, value)) Refresh(); }
    }

    public bool IsArchived
    {
        get => _isArchived;
        set => SetProperty(ref _isArchived, value);
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set { if (SetProperty(ref _isFavorite, value)) OnPropertyChanged(nameof(FavoriteGlyph)); }
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(SizeText));
        OnPropertyChanged(nameof(ListMeta));
        OnPropertyChanged(nameof(RecentMeta));
        OnPropertyChanged(nameof(DateText));
        OnPropertyChanged(nameof(PublicationText));
        OnPropertyChanged(nameof(VersionBadgeText));
        OnPropertyChanged(nameof(ArchivedText));
    }

    // ----- Affichage -----
    public string ExtensionUpper => Extension.ToUpperInvariant();

    public string TypeLabel => Extension switch
    {
        "pdf" => "Document PDF",
        "docx" => "Document Word (DOCX)",
        "xlsx" => "Feuille de calcul (XLSX)",
        _ => "Document"
    };

    public string SizeText => SizeKb < 1024
        ? $"{SizeKb.ToString("0", CultureInfo.InvariantCulture)} Ko"
        : $"{(SizeKb / 1024).ToString("0.0", CultureInfo.InvariantCulture)} Mo";

    public string FileGlyph => Extension switch
    {
        "pdf" => Icons.Pdf,
        "xlsx" => Icons.TableChart,
        _ => Icons.Description
    };

    public Color FileColor => Extension switch
    {
        "pdf" => Palette.Danger,
        "xlsx" => Palette.Success,
        _ => Palette.Primary
    };

    public Color FileBg => Extension switch
    {
        "pdf" => Color.FromArgb("#FEE2E2"),
        "xlsx" => Color.FromArgb("#DCFCE7"),
        _ => Palette.AccentLight
    };

    public Color CategoryFg => Category switch
    {
        "Finance" => Color.FromArgb("#15803D"),
        "RH" => Color.FromArgb("#1D4ED8"),
        "Juridique" => Color.FromArgb("#B45309"),
        "Direction" => Palette.PrimaryDark,
        _ => Palette.TextSecondary
    };

    public Color CategoryBg => Category switch
    {
        "Finance" => Color.FromArgb("#DCFCE7"),
        "RH" => Color.FromArgb("#DBEAFE"),
        "Juridique" => Color.FromArgb("#FEF3C7"),
        "Direction" => Color.FromArgb("#E0E7FF"),
        _ => Palette.NeutralBg
    };

    public string DateText => ModifiedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    public string PublicationText => ModifiedAt.ToString("d MMM yyyy 'à' HH:mm", Fr);

    public string ListMeta => VersionsCount > 1
        ? $"{SizeText} • {Subtitle} • v{CurrentVersion}"
        : $"{SizeText} • {Subtitle}";

    public string RecentMeta => $"{SizeText} • Modifié {ModifiedInfo}";
    public string ArchivedText => $"Archivé le {(ArchivedAt ?? DateTime.Now).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}";
    public string VersionBadgeText => $"v{CurrentVersion} (Active)";
    public string OwnerLabel => string.IsNullOrWhiteSpace(OwnerRole) ? Owner : Owner;
    public string OwnerRoleText => string.IsNullOrWhiteSpace(OwnerRole) ? "" : $"({OwnerRole})";

    public string OwnerInitials
    {
        get
        {
            var parts = Owner.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            return parts.Length == 1
                ? parts[0][..1].ToUpperInvariant()
                : $"{parts[0][..1]}{parts[^1][..1]}".ToUpperInvariant();
        }
    }

    public string FavoriteGlyph => IsFavorite ? Icons.Star : Icons.StarBorder;
}
