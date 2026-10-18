using SmartDocs.Models;

namespace SmartDocs.Services;

/// <summary>
/// Dépôt de documents EN MÉMOIRE alimenté par des données de démonstration.
/// Aucune persistance : tout est réinitialisé à chaque lancement de l'application.
/// </summary>
public class DocumentService
{
    public static readonly string[] Categories = { "RH", "Finance", "Direction", "Juridique" };
    public static readonly string[] ConfidentialityLevels = { "Public", "Interne", "Confidentiel" };

    private readonly List<DocumentItem> _docs = new();
    private readonly Dictionary<string, List<DocumentVersion>> _versions = new();

    public DocumentService() => Seed();

    // ------------------------------------------------------------------ Lecture
    public DocumentItem? GetById(string? id) => _docs.FirstOrDefault(d => d.Id == id);

    public int CountActive => _docs.Count(d => !d.IsArchived);
    public int CountArchived => _docs.Count(d => d.IsArchived);
    public double ArchivedSizeMb => _docs.Where(d => d.IsArchived).Sum(d => d.SizeKb) / 1024.0;

    /// <summary>Nombre total de mises à jour (versions autres que la version initiale).</summary>
    public int VersionUpdates => _versions.Values.Sum(v => Math.Max(0, v.Count - 1));

    public List<DocumentItem> GetRecents(int take = 3)
        => _docs.Where(d => !d.IsArchived && d.RecentRank > 0).OrderByDescending(d => d.RecentRank).Take(take).ToList();

    public List<DocumentItem> GetDocumentsWithHistory()
        => _docs.Where(d => !d.IsArchived && GetVersions(d.Id).Count > 1).OrderBy(d => d.Name).ToList();

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

    public List<DocumentVersion> GetVersions(string documentId)
        => _versions.TryGetValue(documentId, out var list)
            ? list.OrderByDescending(v => v.Date).ToList()
            : new List<DocumentVersion>();

    // ------------------------------------------------------------------ Actions
    public void Archive(DocumentItem doc)
    {
        doc.IsArchived = true;
        doc.ArchivedAt = DateTime.Now;
        doc.Refresh();
    }

    public void Unarchive(DocumentItem doc)
    {
        doc.IsArchived = false;
        doc.ArchivedAt = null;
        doc.Refresh();
    }

    /// <summary>Restaure une ancienne version : crée une NOUVELLE version active, l'historique est conservé.</summary>
    public DocumentVersion? RestoreVersion(string documentId, string versionId, string author)
    {
        var doc = GetById(documentId);
        if (doc == null || !_versions.TryGetValue(documentId, out var list)) return null;
        var source = list.FirstOrDefault(v => v.Id == versionId);
        var current = list.FirstOrDefault(v => v.IsCurrent);
        if (source == null || current == null) return null;

        current.IsCurrent = false;
        var restored = new DocumentVersion
        {
            DocumentId = documentId,
            Number = NextNumber(current.Number),
            SizeKb = source.SizeKb,
            Date = DateTime.Now,
            Author = author,
            Comment = $"Restauration de la version {source.Number}",
            IsCurrent = true
        };
        list.Add(restored);

        doc.SizeKb = restored.SizeKb;
        doc.ModifiedAt = restored.Date;
        doc.ModifiedInfo = $"à l'instant par {author}";
        doc.VersionsCount = list.Count;
        doc.CurrentVersion = restored.Number;
        doc.Refresh();
        return restored;
    }

    public DocumentItem AddDocument(string name, string category, string extension, double sizeKb,
                                    string confidentiality, IEnumerable<string> tags, string owner)
    {
        var fileName = name.Trim();
        if (!fileName.EndsWith("." + extension, StringComparison.OrdinalIgnoreCase)) fileName += "." + extension;

        var doc = new DocumentItem
        {
            Name = fileName.Replace(' ', '_'),
            Category = category,
            CategoryPath = category,
            Extension = extension,
            SizeKb = sizeKb,
            Subtitle = "Ajouté depuis l'application",
            Description = category,
            Owner = owner,
            OwnerRole = "",
            Confidentiality = confidentiality,
            Tags = tags.ToList(),
            ModifiedAt = DateTime.Now,
            ModifiedInfo = $"à l'instant par {owner}",
            RecentRank = (_docs.Count == 0 ? 0 : _docs.Max(d => d.RecentRank)) + 1
        };
        AddWithVersions(doc, ("1.0", doc.SizeKb, doc.ModifiedAt, owner, "Version initiale"));
        return doc;
    }

    // ------------------------------------------------------------------ Interne
    private static string NextNumber(string current)
    {
        var parts = current.Split('.');
        return parts.Length == 2 && int.TryParse(parts[0], out var maj) && int.TryParse(parts[1], out var min)
            ? $"{maj}.{min + 1}"
            : current + ".1";
    }

    private void AddWithVersions(DocumentItem doc,
        params (string number, double kb, DateTime date, string author, string comment)[] versions)
    {
        _docs.Add(doc);
        var list = new List<DocumentVersion>();
        for (var i = 0; i < versions.Length; i++)
        {
            var v = versions[i];
            list.Add(new DocumentVersion
            {
                DocumentId = doc.Id,
                Number = v.number,
                SizeKb = v.kb,
                Date = v.date,
                Author = v.author,
                Comment = v.comment,
                IsInitial = i == 0,
                IsCurrent = i == versions.Length - 1
            });
        }
        _versions[doc.Id] = list;
        doc.VersionsCount = list.Count;
        doc.CurrentVersion = list[^1].Number;
        doc.SizeKb = list[^1].SizeKb;
    }

    private static double Mo(double mb) => mb * 1024;

    private DocumentItem Doc(string name, string cat, string ext, double kb, string subtitle, DateTime date,
                             string owner, string role = "", string path = "", string desc = "", string conf = "Interne",
                             string modifiedInfo = "", int recent = 0, params string[] tags)
        => new()
        {
            Name = name, Category = cat, Extension = ext, SizeKb = kb, Subtitle = subtitle, ModifiedAt = date,
            Owner = owner, OwnerRole = role, CategoryPath = string.IsNullOrEmpty(path) ? cat : path,
            Description = string.IsNullOrEmpty(desc) ? subtitle : desc, Confidentiality = conf,
            ModifiedInfo = modifiedInfo, RecentRank = recent, Tags = tags.ToList()
        };

    private void Seed()
    {
        // ----- Documents actifs -----
        var sec = Doc("Politique_Securite_SI.pdf", "Direction", "pdf", Mo(3.2), "Validé SSI", new DateTime(2024, 10, 14, 10, 45, 0),
            "Thomas Dubois", "CISO", "Direction / Sécurité", "Système d'Information & Gouvernance", "Confidentiel - Interne",
            tags: new[] { "#Sécurité", "#Gouvernance" });
        AddWithVersions(sec,
            ("1.0", Mo(2.8), new DateTime(2024, 1, 10, 9, 0, 0), "Direction Générale", "Version inaugurale approuvée"),
            ("2.2", Mo(2.9), new DateTime(2024, 6, 15, 14, 0, 0), "Sophie Laurent", "Révision semestrielle des accès"),
            ("2.3", Mo(3.1), new DateTime(2024, 9, 22, 11, 30, 0), "Thomas Dubois", "Ajout des clauses télétravail"),
            ("2.4", Mo(3.2), new DateTime(2024, 10, 14, 10, 45, 0), "Thomas Dubois", "Mise à jour de la charte de télétravail et des accès distants"));

        Add1(Doc("Note_de_Frais_Modele.xlsx", "Finance", "xlsx", 450, "Gabarit Q4 • DAF", new DateTime(2024, 10, 10, 9, 15, 0), "Marie Lefèvre", "DAF"));
        Add1(Doc("Onboarding_Collaborateur.docx", "RH", "docx", Mo(1.8), "Livret d'accueil • Équipe People", new DateTime(2024, 10, 8, 16, 0, 0), "Équipe People"));
        Add1(Doc("Bilan_Carbone_2024.pdf", "Direction", "pdf", Mo(5.6), "Rapport RSE", new DateTime(2024, 10, 1, 10, 0, 0), "Julien Moreau", "RSE"));
        Add1(Doc("Rapport_Financier_Q3.pdf", "Finance", "pdf", Mo(2.4), "Clôture trimestrielle", new DateTime(2024, 10, 13, 8, 30, 0),
            "Claire Durand", "Contrôle de gestion", modifiedInfo: "il y a 2h par Claire D.", recent: 3));
        Add1(Doc("Contrat_Cadre_Prestation.docx", "Juridique", "docx", Mo(1.1), "Contrat prestataire", new DateTime(2024, 10, 12, 17, 0, 0),
            "Marc Lambert", "Juriste", modifiedInfo: "hier par Marc L.", recent: 2));
        Add1(Doc("Grille_Salaires_2025.xlsx", "RH", "xlsx", 840, "Grille de rémunération", new DateTime(2024, 10, 12, 9, 0, 0),
            "Équipe RH", conf: "Confidentiel", modifiedInfo: "le 12 Oct par RH", recent: 1));
        Add1(Doc("Charte_Graphique_2024.pdf", "Direction", "pdf", Mo(6.1), "Communication", new DateTime(2024, 9, 28, 14, 0, 0), "Nadia Benali", "Communication"));
        Add1(Doc("Budget_Previsionnel_2025.xlsx", "Finance", "xlsx", Mo(1.3), "Version DAF", new DateTime(2024, 9, 25, 11, 0, 0), "Marie Lefèvre", "DAF"));
        Add1(Doc("Reglement_Interieur.docx", "RH", "docx", 720, "Mise à jour légale", new DateTime(2024, 9, 18, 10, 30, 0), "Équipe People"));
        Add1(Doc("Politique_Teletravail.pdf", "RH", "pdf", Mo(1.5), "Accord d'entreprise", new DateTime(2024, 9, 5, 15, 0, 0), "Sophie Laurent", "DRH"));
        Add1(Doc("Modele_NDA.docx", "Juridique", "docx", 310, "Accord de confidentialité", new DateTime(2024, 8, 30, 9, 45, 0), "Marc Lambert", "Juriste"));

        // ----- Documents archivés -----
        Arch("Bilan_Comptable_2022.pdf", "Finance", "pdf", Mo(4.8), new DateTime(2024, 1, 15));
        Arch("Contrat_Fournisseur_Telecom.docx", "Juridique", "docx", 890, new DateTime(2023, 11, 2));
        Arch("Registre_Personnel_2022.xlsx", "RH", "xlsx", Mo(1.2), new DateTime(2023, 8, 14));
        Arch("Audit_Interne_2022.pdf", "Direction", "pdf", Mo(3.9), new DateTime(2024, 2, 20));
        Arch("Declaration_TVA_2023.xlsx", "Finance", "xlsx", 640, new DateTime(2024, 3, 8));
        Arch("Avenant_Bail_2021.docx", "Juridique", "docx", 520, new DateTime(2022, 12, 5));
        Arch("Rapport_Annuel_2021.pdf", "Direction", "pdf", Mo(7.4), new DateTime(2022, 6, 30));
        Arch("Paie_Archive_2022.xlsx", "RH", "xlsx", Mo(2.2), new DateTime(2023, 3, 21));
    }

    private void Add1(DocumentItem d) => AddWithVersions(d, ("1.0", d.SizeKb, d.ModifiedAt, d.Owner, "Version initiale"));

    private void Arch(string name, string cat, string ext, double kb, DateTime archivedAt)
    {
        var d = Doc(name, cat, ext, kb, "Pièce archivée", archivedAt.AddMonths(-6), "Archives " + cat);
        d.IsArchived = true;
        d.ArchivedAt = archivedAt;
        Add1(d);
    }
}
