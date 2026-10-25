using System.Net.Http.Headers;
using SmartDocs.Models;

namespace SmartDocs.Services;

/// <summary>
/// Accès aux documents via l'API. La liste est chargée dans un cache local (RefreshAsync) ;
/// la recherche, le filtrage et le tri de l'écran s'appliquent sur ce cache.
/// </summary>
public class DocumentService(ApiClient api)
{
    public static readonly string[] Categories = { "RH", "Finance", "Direction", "Juridique" };
    public static readonly string[] ConfidentialityLevels = { "Public", "Interne", "Confidentiel" };

    private List<DocumentItem> _docs = new();

    // ------------------------------------------------------------------ Chargement
    public async Task RefreshAsync()
    {
        var active = await api.GetAsync<List<DocumentDto>>("/api/documents?archived=false");
        var archived = await api.GetAsync<List<DocumentDto>>("/api/documents?archived=true");
        _docs = active.Concat(archived).Select(FromDto).ToList();
    }

    private static DocumentItem FromDto(DocumentDto d) => new()
    {
        Id = d.Id, Name = d.Name, Category = d.Category, CategoryPath = d.CategoryPath, Extension = d.Extension,
        Subtitle = d.Subtitle, Description = d.Description, Owner = d.Owner, OwnerRole = d.OwnerRole,
        Confidentiality = d.Confidentiality, Tags = d.Tags ?? new List<string>(),
        SizeKb = d.SizeBytes / 1024.0, ModifiedAt = d.ModifiedAt.ToLocalTime(), ModifiedBy = d.ModifiedBy,
        IsArchived = d.IsArchived, ArchivedAt = d.ArchivedAt?.ToLocalTime(), IsFavorite = d.IsFavorite,
        CurrentVersion = d.CurrentVersion, VersionsCount = d.VersionsCount,
        CanArchive = d.CanArchive, CanAddVersion = d.CanAddVersion,
        CanDelete = d.CanDelete, CanPurge = d.CanPurge, DeletedAt = d.DeletedAt?.ToLocalTime(), DeletedBy = d.DeletedBy ?? ""
    };

    private static DocumentVersion FromDto(VersionDto v) => new()
    {
        Id = v.Id, DocumentId = v.DocumentId, Number = v.Number, SizeKb = v.SizeBytes / 1024.0,
        Date = v.CreatedAt.ToLocalTime(), Author = v.Author, Comment = v.Comment,
        IsCurrent = v.IsCurrent, IsInitial = v.IsInitial
    };

    /// <summary>Vide le cache local (déconnexion).</summary>
    public void Clear() => _docs = new();

    // ------------------------------------------------------------------ Lecture (cache)
    public DocumentItem? GetById(string? id) => _docs.FirstOrDefault(d => d.Id == id);

    public int CountActive => _docs.Count(d => !d.IsArchived);
    public int CountArchived => _docs.Count(d => d.IsArchived);
    public double ArchivedSizeMb => _docs.Where(d => d.IsArchived).Sum(d => d.SizeKb) / 1024.0;
    public int VersionUpdates => _docs.Sum(d => Math.Max(0, d.VersionsCount - 1));

    public List<DocumentItem> GetRecents(int take = 3)
        => _docs.Where(d => !d.IsArchived).OrderByDescending(d => d.ModifiedAt).Take(take).ToList();

    public List<DocumentItem> GetDocumentsWithHistory()
        => _docs.Where(d => !d.IsArchived && d.VersionsCount > 1).OrderBy(d => d.Name).ToList();

    /// <param name="sort">"recent" | "name" | "size"</param>
    public List<DocumentItem> Query(bool archived, string? text = null, string? category = null,
                                    string? extension = null, int? archiveYear = null, string sort = "recent",
                                    bool ascendingDate = false)
    {
        IEnumerable<DocumentItem> q = _docs.Where(d => d.IsArchived == archived);

        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(d => d.Category == category);
        if (!string.IsNullOrWhiteSpace(extension)) q = q.Where(d => d.Extension == extension);
        if (archiveYear.HasValue) q = q.Where(d => d.ArchivedAt?.Year == archiveYear);
        if (!string.IsNullOrWhiteSpace(text))
        {
            var t = text.Trim();
            q = q.Where(d =>
                d.Name.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                d.Category.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                d.Subtitle.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                d.Owner.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                d.Tags.Any(tag => tag.Contains(t, StringComparison.OrdinalIgnoreCase)));
        }

        if (archived)
        {
            q = ascendingDate ? q.OrderBy(d => d.ArchivedAt) : q.OrderByDescending(d => d.ArchivedAt);
        }
        else
        {
            q = sort switch
            {
                "name" => q.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase),
                "size" => q.OrderByDescending(d => d.SizeKb),
                _ => q.OrderByDescending(d => d.ModifiedAt)
            };
        }
        return q.ToList();
    }

    public List<ChipItem> BuildCategoryChips(string? selected)
    {
        var active = _docs.Where(d => !d.IsArchived).ToList();
        var chips = new List<ChipItem> { new(null, $"Tous ({active.Count})", selected == null) };
        foreach (var c in Categories.Where(c => active.Any(d => d.Category == c)))
            chips.Add(new ChipItem(c, c, selected == c));
        return chips;
    }

    public List<ChipItem> BuildYearChips(int? selected)
    {
        var archived = _docs.Where(d => d.IsArchived).ToList();
        var chips = new List<ChipItem> { new(null, $"Toutes ({archived.Count})", selected == null) };
        foreach (var g in archived.Where(d => d.ArchivedAt.HasValue)
                                  .GroupBy(d => d.ArchivedAt!.Value.Year).OrderByDescending(g => g.Key))
            chips.Add(new ChipItem(g.Key.ToString(), $"{g.Key} ({g.Count()})", selected == g.Key));
        return chips;
    }

    // ------------------------------------------------------------------ Appels API
    public async Task<List<DocumentVersion>> GetVersionsAsync(string documentId)
    {
        var versions = await api.GetAsync<List<VersionDto>>($"/api/documents/{documentId}/versions");
        return versions.Select(FromDto).ToList();
    }

    public async Task ArchiveAsync(DocumentItem doc)
    {
        await api.PostAsync<DocumentDto>($"/api/documents/{doc.Id}/archive");
        await RefreshAsync();
    }

    public async Task UnarchiveAsync(DocumentItem doc)
    {
        await api.PostAsync<DocumentDto>($"/api/documents/{doc.Id}/unarchive");
        await RefreshAsync();
    }

    // ----- Corbeille (suppression logique) -----
    /// <summary>Documents dans la corbeille sur lesquels l'utilisateur peut agir (chargés à la demande, hors cache principal).</summary>
    public async Task<List<DocumentItem>> GetTrashAsync()
    {
        var list = await api.GetAsync<List<DocumentDto>>("/api/documents?trash=true");
        return list.Select(FromDto).ToList();
    }

    public async Task MoveToTrashAsync(DocumentItem doc)
    {
        await api.DeleteAsync<DocumentDto>($"/api/documents/{doc.Id}");
        await RefreshAsync();
    }

    public async Task UndeleteAsync(DocumentItem doc)
    {
        await api.PostAsync<DocumentDto>($"/api/documents/{doc.Id}/undelete");
        await RefreshAsync();
    }

    /// <summary>Suppression définitive (irréversible) d'un document déjà dans la corbeille.</summary>
    public async Task PurgeAsync(DocumentItem doc)
        => await api.DeleteAsync($"/api/documents/{doc.Id}/purge");

    public async Task ToggleFavoriteAsync(DocumentItem doc)
    {
        var result = await api.PostAsync<FavoriteResponse>($"/api/documents/{doc.Id}/favorite");
        doc.IsFavorite = result.IsFavorite;
    }

    /// <summary>Envoie un nouveau fichier comme version active du document (même format que le document).</summary>
    public async Task<DocumentVersion> AddVersionAsync(string documentId, Stream file, string fileName, string comment)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(MimeOf(fileName));
        content.Add(fileContent, "file", fileName);
        content.Add(new StringContent(comment ?? ""), "comment");

        var created = await api.PostMultipartAsync<VersionDto>($"/api/documents/{documentId}/versions", content);
        await RefreshAsync();
        return FromDto(created);
    }

    /// <summary>Restaure une ancienne version : le serveur crée une NOUVELLE version active.</summary>
    public async Task<DocumentVersion> RestoreVersionAsync(string documentId, string versionId)
    {
        var created = await api.PostAsync<VersionDto>($"/api/documents/{documentId}/versions/{versionId}/restore");
        await RefreshAsync();
        return FromDto(created);
    }

    public async Task<DocumentItem> AddDocumentAsync(Stream file, string fileName, string name, string category,
                                                     string confidentiality, IEnumerable<string> tags)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(MimeOf(fileName));
        content.Add(fileContent, "file", fileName);
        content.Add(new StringContent(name), "name");
        content.Add(new StringContent(category), "category");
        content.Add(new StringContent(confidentiality), "confidentiality");
        content.Add(new StringContent(string.Join(",", tags)), "tags");

        var dto = await api.PostMultipartAsync<DocumentDto>("/api/documents", content);
        await RefreshAsync();
        return GetById(dto.Id) ?? FromDto(dto);
    }

    /// <summary>Télécharge le fichier (version courante ou version donnée) dans le cache de l'application.</summary>
    public async Task<string> DownloadToCacheAsync(DocumentItem doc, DocumentVersion? version = null)
    {
        var fileName = version == null
            ? doc.Name
            : $"{Path.GetFileNameWithoutExtension(doc.Name)}_v{version.Number}{Path.GetExtension(doc.Name)}";
        var destination = Path.Combine(FileSystem.CacheDirectory, fileName);
        var url = version == null
            ? $"/api/documents/{doc.Id}/file"
            : $"/api/documents/{doc.Id}/versions/{version.Id}/file";
        await api.DownloadAsync(url, destination);
        return destination;
    }

    private static string MimeOf(string fileName) => Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant() switch
    {
        "pdf" => "application/pdf",
        "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _ => "application/octet-stream"
    };
}
