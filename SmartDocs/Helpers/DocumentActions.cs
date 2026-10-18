using SmartDocs.Models;
using SmartDocs.Services;

namespace SmartDocs.Helpers;

/// <summary>Actions communes aux listes de documents (menu ⋮, simulations de consultation / téléchargement).</summary>
public static class DocumentActions
{
    public static async Task OpenDetailAsync(DocumentItem doc)
        => await Shell.Current.GoToAsync($"documentdetail?id={doc.Id}");

    public static async Task OpenVersionsAsync(DocumentItem doc)
        => await Shell.Current.GoToAsync($"versions?id={doc.Id}");

    public static Task DownloadAsync(Page page, DocumentItem doc)
        => page.DisplayAlert("Téléchargement simulé",
            $"« {doc.Name} » ({doc.SizeText}).\n\nCette version de démonstration ne contient pas de serveur de fichiers : aucun fichier réel n'est téléchargé.",
            "OK");

    public static Task ViewAsync(Page page, DocumentItem doc)
        => page.DisplayAlert("Consultation simulée",
            $"L'aperçu de « {doc.Name} » nécessiterait un backend de stockage et de rendu de documents.",
            "OK");

    /// <summary>Affiche le menu ⋮. Retourne true si les données ont changé (archivage / restauration).</summary>
    public static async Task<bool> ShowMenuAsync(Page page, DocumentItem doc, DocumentService docs)
    {
        const string details = "Voir les détails";
        const string versions = "Historique des versions";
        const string download = "Télécharger";
        var archiveLabel = doc.IsArchived ? "Restaurer depuis les archives" : "Archiver";

        var choice = await page.DisplayActionSheet(doc.Name, "Annuler", null, details, versions, download, archiveLabel);

        if (choice == details) { await OpenDetailAsync(doc); return false; }
        if (choice == versions) { await OpenVersionsAsync(doc); return false; }
        if (choice == download) { await DownloadAsync(page, doc); return false; }
        if (choice == archiveLabel) return await ToggleArchiveAsync(page, doc, docs);
        return false;
    }

    public static async Task<bool> ToggleArchiveAsync(Page page, DocumentItem doc, DocumentService docs)
    {
        if (doc.IsArchived)
        {
            docs.Unarchive(doc);
            await page.DisplayAlert("Document restauré", $"« {doc.Name} » est de nouveau disponible dans Documents.", "OK");
            return true;
        }

        var ok = await page.DisplayAlert("Archiver le document",
            $"Archiver « {doc.Name} » ? Il sera déplacé dans l'espace Archives et pourra être restauré.",
            "Archiver", "Annuler");
        if (!ok) return false;
        docs.Archive(doc);
        return true;
    }
}
