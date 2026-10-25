using System.Globalization;

namespace SmartDocs.Models;

public class DocumentVersion
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string DocumentId { get; init; } = "";
    public string Number { get; set; } = "1.0";
    public double SizeKb { get; set; }
    public DateTime Date { get; set; }
    public string Author { get; set; } = "";
    public string Comment { get; set; } = "";
    public bool IsCurrent { get; set; }
    public bool IsInitial { get; set; }
    public bool CanRestore { get; set; }

    public string Title => $"Version {Number}";
    public string SizeText => SizeKb < 1024
        ? $"{SizeKb.ToString("0", CultureInfo.InvariantCulture)} Ko"
        : $"{(SizeKb / 1024).ToString("0.0", CultureInfo.InvariantCulture)} Mo";
    public string HeaderText => $"{Title} • {SizeText}";
    public string DateText => Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    public string PublishedText => $"Publié le {DateText} à {Date.ToString("HH:mm", CultureInfo.InvariantCulture)}";
    public string ByText => $"Par {Author}";
    public string QuoteText => $"“{Comment}”";
    public string ShortLine => $"v{Number} • {DateText} • {Author}";
    public string TrailingGlyph => IsInitial ? Helpers.Icons.LockOutline : Helpers.Icons.History;
}
