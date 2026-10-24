using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SmartDocs.Api.Data;
using SmartDocs.Api.Dtos;
using SmartDocs.Api.Models;
using SmartDocs.Api.Services;

namespace SmartDocs.Api.Endpoints;

public static class DocumentEndpoints
{
    private const long MaxBytes = 10L * 1024 * 1024;
    private static readonly string[] Categories = { "RH", "Finance", "Direction", "Juridique" };
    private static readonly string[] Levels = { "Public", "Interne", "Confidentiel" };
    private static readonly Dictionary<string, string> Mime = new()
    {
        ["pdf"] = "application/pdf",
        ["docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ["xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };

    public static void MapDocuments(this WebApplication app)
    {
        var g = app.MapGroup("/api/documents").RequireAuthorization();

        g.MapGet("/", List);
        g.MapPost("/", Create).DisableAntiforgery();
        g.MapGet("/{id}", GetOne);
        g.MapPost("/{id}/archive", (string id, AppDbContext db, ClaimsPrincipal u) => SetArchived(id, true, db, u));
        g.MapPost("/{id}/unarchive", (string id, AppDbContext db, ClaimsPrincipal u) => SetArchived(id, false, db, u));
        g.MapDelete("/{id}", MoveToTrash);
        g.MapPost("/{id}/undelete", Undelete);
        g.MapDelete("/{id}/purge", Purge);
        g.MapPost("/{id}/favorite", ToggleFavorite);
        g.MapGet("/{id}/file", DownloadCurrent);
        g.MapGet("/{id}/versions", ListVersions);
        g.MapPost("/{id}/versions", AddVersion).DisableAntiforgery();
        g.MapGet("/{id}/versions/{versionId}/file", DownloadVersion);
        g.MapPost("/{id}/versions/{versionId}/restore", RestoreVersion);
    }

    private static IResult Bad(string message) => Results.BadRequest(new { error = message });
    private static IResult NotFound(string message = "Ressource introuvable.") => Results.NotFound(new { error = message });
    private static IResult Forbidden(string message) => Results.Json(new { error = message }, statusCode: StatusCodes.Status403Forbidden);

    /// <summary>Utilisateur courant relu en base : son rôle est toujours à jour.</summary>
    private static Task<User?> CurrentUser(ClaimsPrincipal principal, AppDbContext db)
        => db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == principal.UserId());

    /// <summary>Charge un document avec ses versions. Un document dans la corbeille est introuvable, sauf si includeDeleted.</summary>
    private static Task<Document?> LoadDoc(AppDbContext db, string id, bool track = true, bool includeDeleted = false)
        => (track ? db.Documents : db.Documents.AsNoTracking()).Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.Id == id && (includeDeleted || !d.IsDeleted));

    private static async Task<HashSet<string>> FavoritesOf(AppDbContext db, int userId)
        => (await db.Favorites.Where(f => f.UserId == userId).Select(f => f.DocumentId).ToListAsync()).ToHashSet();

    // ------------------------------------------------------------------ Lecture
    private static async Task<IResult> List(ClaimsPrincipal u, AppDbContext db, string? search, string? category,
        string? extension, bool? archived, bool? trash, string? sort, int? year)
    {
        var me = await CurrentUser(u, db);
        if (me == null) return Results.Unauthorized();

        var inTrash = trash == true;
        var q = db.Documents.Include(d => d.Versions).AsNoTracking().Where(Access.Visible(me));
        q = inTrash
            ? q.Where(d => d.IsDeleted)
            : q.Where(d => !d.IsDeleted && d.IsArchived == (archived ?? false));

        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(d => d.Category == category);
        if (!string.IsNullOrWhiteSpace(extension)) q = q.Where(d => d.Extension == extension.ToLower());
        if (year.HasValue) q = q.Where(d => d.ArchivedAt != null && d.ArchivedAt.Value.Year == year.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var like = $"%{search.Trim().Replace("%", "").Replace("_", "")}%";
            q = q.Where(d => EF.Functions.Like(d.Name, like) || EF.Functions.Like(d.Category, like) ||
                             EF.Functions.Like(d.Subtitle, like) || EF.Functions.Like(d.OwnerName, like) ||
                             EF.Functions.Like(d.Tags, like));
        }

        var favs = await FavoritesOf(db, u.UserId());
        var list = await q.ToListAsync();
        if (inTrash) list = list.Where(d => Access.CanSeeInTrash(me, d)).ToList();
        IEnumerable<DocumentDto> dtos = list.Select(d => d.ToDto(favs, me));
        dtos = (sort ?? (inTrash ? "deleted_desc" : "recent")) switch
        {
            "deleted_desc" => dtos.OrderByDescending(d => d.DeletedAt),
            "name" => dtos.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase),
            "size" => dtos.OrderByDescending(d => d.SizeBytes),
            "archived_asc" => dtos.OrderBy(d => d.ArchivedAt),
            "archived_desc" => dtos.OrderByDescending(d => d.ArchivedAt),
            _ => dtos.OrderByDescending(d => d.ModifiedAt)
        };
        return Results.Ok(dtos.ToList());
    }

    private static async Task<IResult> GetOne(string id, ClaimsPrincipal u, AppDbContext db)
    {
        var me = await CurrentUser(u, db);
        var doc = await LoadDoc(db, id, track: false);
        if (me == null || doc == null || !Access.CanSee(me, doc)) return NotFound("Document introuvable.");
        return Results.Ok(doc.ToDto(await FavoritesOf(db, me.Id), me));
    }

    private static async Task<IResult> ListVersions(string id, ClaimsPrincipal u, AppDbContext db)
    {
        var me = await CurrentUser(u, db);
        var doc = await LoadDoc(db, id, track: false);
        if (me == null || doc == null || !Access.CanSee(me, doc)) return NotFound("Document introuvable.");
        var versions = await db.Versions.AsNoTracking().Where(v => v.DocumentId == id)
            .OrderByDescending(v => v.CreatedAt).ToListAsync();
        return Results.Ok(versions.Select(v => v.ToDto()).ToList());
    }

    // ------------------------------------------------------------------ Dépôt / versions
    private static async Task<(string? Error, string? Ext)> ValidateFile(IFormFile? file)
    {
        if (file == null || file.Length == 0) return ("Aucun fichier reçu.", null);
        if (file.Length > MaxBytes) return ("Le fichier dépasse la taille maximale de 10 Mo.", null);

        var ext = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
        if (!Mime.ContainsKey(ext)) return ("Format non accepté (PDF, DOCX ou XLSX uniquement).", null);

        // Contrôle de la signature : %PDF pour les PDF, PK.. (zip) pour DOCX / XLSX
        var head = new byte[4];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(head, 4, throwOnEndOfStream: false);
        var ok = ext == "pdf"
            ? head[0] == 0x25 && head[1] == 0x50 && head[2] == 0x44 && head[3] == 0x46
            : head[0] == 0x50 && head[1] == 0x4B && head[2] == 0x03 && head[3] == 0x04;
        if (read < 4 || !ok) return ("Le contenu du fichier ne correspond pas à son extension.", null);
        return (null, ext);
    }

    private static string CleanName(string raw, string ext)
    {
        var name = raw.Trim();
        if (name.EndsWith("." + ext, StringComparison.OrdinalIgnoreCase)) name = name[..^(ext.Length + 1)];
        name = new string(name.Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' or '.').ToArray())
            .Trim().Replace(' ', '_');
        if (name.Length > 100) name = name[..100];
        return name.Length == 0 ? "" : $"{name}.{ext}";
    }

    private static List<string> ParseTags(string raw)
        => raw.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
              .Select(t => "#" + new string(t.TrimStart('#').Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray()))
              .Where(t => t.Length > 1 && t.Length <= 31)
              .Distinct(StringComparer.OrdinalIgnoreCase)
              .Take(10).ToList();

    private static string NextNumber(string current, IEnumerable<string> existing)
    {
        var used = existing.ToHashSet();
        var parts = current.Split('.');
        var major = parts.Length == 2 && int.TryParse(parts[0], out var maj) ? maj : 1;
        var minor = parts.Length == 2 && int.TryParse(parts[1], out var min) ? min : 0;
        string candidate;
        do { minor++; candidate = $"{major}.{minor}"; } while (used.Contains(candidate));
        return candidate;
    }

    private static DocumentVersion NewVersion(AppDbContext db, Document doc, string stored, long size, string author, string comment, string? number = null)
    {
        foreach (var old in doc.Versions) old.IsCurrent = false;
        var latest = doc.Versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
        var version = new DocumentVersion
        {
            DocumentId = doc.Id,
            Number = number ?? NextNumber(latest?.Number ?? "1.0", doc.Versions.Select(v => v.Number)),
            SizeBytes = size, StoredFileName = stored, CreatedAt = DateTime.UtcNow,
            Author = author, Comment = comment, IsCurrent = true
        };
        db.Versions.Add(version);
        doc.ModifiedAt = version.CreatedAt;
        doc.ModifiedBy = author;
        return version;
    }

    private static async Task<IResult> Create(HttpRequest req, ClaimsPrincipal u, AppDbContext db, FileStorage files)
    {
        if (!req.HasFormContentType) return Bad("Requête multipart/form-data attendue.");
        var form = await req.ReadFormAsync();
        var file = form.Files.GetFile("file");

        var (error, ext) = await ValidateFile(file);
        if (error != null) return Bad(error);

        var name = CleanName(form["name"].ToString(), ext!);
        if (name.Length == 0) return Bad("Le nom du document est obligatoire.");
        var category = form["category"].ToString();
        if (!Categories.Contains(category)) return Bad("Catégorie invalide.");
        var level = form["confidentiality"].ToString();
        if (!Levels.Contains(level)) level = "Interne";

        var user = await CurrentUser(u, db);
        if (user == null) return Results.Unauthorized();
        if (!Access.CanDeposit(user, category))
            return Forbidden("Votre rôle ne vous permet pas de déposer un document dans ce service.");

        await using var stream = file!.OpenReadStream();
        var stored = await files.SaveAsync(stream, ext!);

        var doc = new Document
        {
            Name = name, Category = category, CategoryPath = category, Extension = ext!,
            Subtitle = "Ajouté depuis l'application", Description = category,
            OwnerId = user.Id, OwnerName = user.FullName, OwnerRole = user.JobTitle, Confidentiality = level,
            Tags = string.Join('|', ParseTags(form["tags"].ToString())),
            ModifiedAt = DateTime.UtcNow, ModifiedBy = user.FullName
        };
        db.Documents.Add(doc);
        NewVersion(db, doc, stored, file.Length, user.FullName, "Version initiale", number: "1.0");
        await db.SaveChangesAsync();

        var created = await db.Documents.Include(d => d.Versions).AsNoTracking().FirstAsync(d => d.Id == doc.Id);
        return Results.Created($"/api/documents/{doc.Id}", created.ToDto(await FavoritesOf(db, user.Id), user));
    }

    private static async Task<IResult> AddVersion(string id, HttpRequest req, ClaimsPrincipal u, AppDbContext db, FileStorage files)
    {
        var user = await CurrentUser(u, db);
        var doc = await LoadDoc(db, id);
        if (user == null || doc == null || !Access.CanSee(user, doc)) return NotFound("Document introuvable.");
        if (!Access.CanAddVersion(user, doc)) return Forbidden("Votre rôle ne vous permet pas d'ajouter une version à ce document.");
        if (!req.HasFormContentType) return Bad("Requête multipart/form-data attendue.");

        var form = await req.ReadFormAsync();
        var file = form.Files.GetFile("file");
        var (error, ext) = await ValidateFile(file);
        if (error != null) return Bad(error);
        if (ext != doc.Extension) return Bad($"La nouvelle version doit être un fichier {doc.Extension.ToUpperInvariant()}.");

        var comment = form["comment"].ToString().Trim();
        if (comment.Length == 0) comment = "Nouvelle version";
        if (comment.Length > 300) comment = comment[..300];

        await using var stream = file!.OpenReadStream();
        var stored = await files.SaveAsync(stream, ext!);
        var version = NewVersion(db, doc, stored, file.Length, user.FullName, comment);
        await db.SaveChangesAsync();
        return Results.Created($"/api/documents/{id}/versions", version.ToDto());
    }

    private static async Task<IResult> RestoreVersion(string id, string versionId, ClaimsPrincipal u, AppDbContext db, FileStorage files)
    {
        var user = await CurrentUser(u, db);
        var doc = await LoadDoc(db, id);
        var source = doc?.Versions.FirstOrDefault(v => v.Id == versionId);
        if (user == null || doc == null || source == null || !Access.CanSee(user, doc)) return NotFound("Version introuvable.");
        if (!Access.CanAddVersion(user, doc)) return Forbidden("Votre rôle ne vous permet pas de restaurer une version de ce document.");

        var stored = files.Copy(source.StoredFileName, doc.Extension);
        if (stored == null) return NotFound("Fichier source introuvable sur le serveur.");

        // Crée une NOUVELLE version active : l'historique est conservé
        var version = NewVersion(db, doc, stored, source.SizeBytes, user.FullName, $"Restauration de la version {source.Number}");
        await db.SaveChangesAsync();
        return Results.Ok(version.ToDto());
    }

    // ------------------------------------------------------------------ Archivage / favoris
    private static async Task<IResult> SetArchived(string id, bool archived, AppDbContext db, ClaimsPrincipal u)
    {
        var me = await CurrentUser(u, db);
        var doc = await LoadDoc(db, id);
        if (me == null || doc == null || !Access.CanSee(me, doc)) return NotFound("Document introuvable.");
        if (!Access.CanArchive(me, doc)) return Forbidden("Votre rôle ne vous permet pas d'archiver ou de restaurer ce document.");
        doc.IsArchived = archived;
        doc.ArchivedAt = archived ? DateTime.UtcNow : null;
        await db.SaveChangesAsync();
        return Results.Ok(doc.ToDto(await FavoritesOf(db, me.Id), me));
    }

    // ------------------------------------------------------------------ Corbeille
    private static async Task<IResult> MoveToTrash(string id, ClaimsPrincipal u, AppDbContext db)
    {
        var me = await CurrentUser(u, db);
        var doc = await LoadDoc(db, id);
        if (me == null || doc == null || !Access.CanSee(me, doc)) return NotFound("Document introuvable.");
        if (!Access.CanDelete(me, doc)) return Forbidden("Votre rôle ne vous permet pas de supprimer ce document.");

        doc.IsDeleted = true;
        doc.DeletedAt = DateTime.UtcNow;
        doc.DeletedBy = me.FullName;
        await db.SaveChangesAsync();
        return Results.Ok(doc.ToDto(await FavoritesOf(db, me.Id), me));
    }

    private static async Task<IResult> Undelete(string id, ClaimsPrincipal u, AppDbContext db)
    {
        var me = await CurrentUser(u, db);
        var doc = await LoadDoc(db, id, includeDeleted: true);
        if (me == null || doc == null || !Access.CanSee(me, doc)) return NotFound("Document introuvable.");
        if (!doc.IsDeleted) return Bad("Ce document n'est pas dans la corbeille.");
        if (!Access.CanDelete(me, doc)) return Forbidden("Votre rôle ne vous permet pas de restaurer ce document.");

        // Le document retrouve son état d'avant (actif ou archivé)
        doc.IsDeleted = false;
        doc.DeletedAt = null;
        doc.DeletedBy = "";
        await db.SaveChangesAsync();
        return Results.Ok(doc.ToDto(await FavoritesOf(db, me.Id), me));
    }

    /// <summary>Suppression définitive : uniquement depuis la corbeille. Supprime la base ET les fichiers de toutes les versions.</summary>
    private static async Task<IResult> Purge(string id, ClaimsPrincipal u, AppDbContext db, FileStorage files)
    {
        var me = await CurrentUser(u, db);
        var doc = await LoadDoc(db, id, includeDeleted: true);
        if (me == null || doc == null || !Access.CanSee(me, doc)) return NotFound("Document introuvable.");
        if (!doc.IsDeleted) return Bad("Placez d'abord le document dans la corbeille.");
        if (!Access.CanPurge(me, doc)) return Forbidden("Seuls la Direction et l'administrateur peuvent supprimer définitivement un document.");

        var storedNames = doc.Versions.Select(v => v.StoredFileName).Distinct().ToList();
        db.Favorites.RemoveRange(db.Favorites.Where(f => f.DocumentId == id));
        db.Documents.Remove(doc); // les versions sont supprimées en cascade
        await db.SaveChangesAsync();

        foreach (var name in storedNames)
            if (!await db.Versions.AnyAsync(v => v.StoredFileName == name)) files.Delete(name);
        return Results.NoContent();
    }

    private static async Task<IResult> ToggleFavorite(string id, ClaimsPrincipal u, AppDbContext db)
    {
        var me = await CurrentUser(u, db);
        var target = await LoadDoc(db, id, track: false);
        if (me == null || target == null || !Access.CanSee(me, target)) return NotFound("Document introuvable.");
        var uid = me.Id;
        var fav = await db.Favorites.FindAsync(uid, id);
        bool on;
        if (fav == null) { db.Favorites.Add(new Favorite { UserId = uid, DocumentId = id }); on = true; }
        else { db.Favorites.Remove(fav); on = false; }
        await db.SaveChangesAsync();
        return Results.Ok(new { isFavorite = on });
    }

    // ------------------------------------------------------------------ Téléchargement
    private static IResult Send(Document doc, DocumentVersion version, FileStorage files, bool versionSuffix)
    {
        var path = files.PathOf(version.StoredFileName);
        if (path == null) return NotFound("Fichier introuvable sur le serveur.");
        var name = versionSuffix
            ? $"{Path.GetFileNameWithoutExtension(doc.Name)}_v{version.Number}.{doc.Extension}"
            : doc.Name;
        return Results.File(path, Mime[doc.Extension], name);
    }

    private static async Task<IResult> DownloadCurrent(string id, ClaimsPrincipal u, AppDbContext db, FileStorage files)
    {
        var me = await CurrentUser(u, db);
        var doc = await LoadDoc(db, id, track: false);
        return me == null || doc == null || !Access.CanSee(me, doc)
            ? NotFound("Document introuvable.")
            : Send(doc, doc.CurrentVersion(), files, false);
    }

    private static async Task<IResult> DownloadVersion(string id, string versionId, ClaimsPrincipal u, AppDbContext db, FileStorage files)
    {
        var me = await CurrentUser(u, db);
        var doc = await LoadDoc(db, id, track: false);
        var version = doc?.Versions.FirstOrDefault(v => v.Id == versionId);
        return me == null || doc == null || version == null || !Access.CanSee(me, doc)
            ? NotFound("Version introuvable.")
            : Send(doc, version, files, true);
    }
}
