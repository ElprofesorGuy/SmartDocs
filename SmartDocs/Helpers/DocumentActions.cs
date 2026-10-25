using SmartDocs.Models;
using SmartDocs.Services;

namespace SmartDocs.Helpers;

/// <summary>Actions communes aux écrans : chargement, erreurs réseau, menu ⋮, consultation et téléchargement réels.</summary>
public static class DocumentActions
{
    /// <summary>Recharge les documents depuis l'API. Affiche l'erreur et retourne false en cas d'échec.</summary>
    public static async Task<bool> RefreshAsync(Page page, DocumentService docs)
    {
        try
        {
            await docs.RefreshAsync();
            return true;
        }
        catch (ApiException ex)
        {
            await HandleAsync(page, ex);
            return false;
        }
    }

    public static async Task HandleAsync(Page page, ApiException ex)
    {
        if (ex.StatusCode == 401)
        {
            await page.DisplayAlert("Session expirée", "Veuillez vous reconnecter.", "OK");
            await Shell.Current.GoToAsync("//login");
            return;
        }
        await page.DisplayAlert("Erreur", ex.Message, "OK");
    }

    public static async Task OpenDetailAsync(DocumentItem doc)
        => await Shell.Current.GoToAsync($"documentdetail?id={doc.Id}");

    public static async Task OpenVersionsAsync(DocumentItem doc)
        => await Shell.Current.GoToAsync($"versions?id={doc.Id}");

    /// <summary>Télécharge le fichier depuis l'API puis l'enregistre (Windows : dossier Téléchargements) ou le partage (Android).</summary>
    public static async Task DownloadAsync(Page page, DocumentItem doc, DocumentService docs, DocumentVersion? version = null)
    {
        try
        {
            var path = await docs.DownloadToCacheAsync(doc, version);
#if WINDOWS
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            Directory.CreateDirectory(folder);
            var destination = Path.Combine(folder, Path.GetFileName(path));
            File.Copy(path, destination, overwrite: true);
            await page.DisplayAlert("Téléchargement terminé", $"Fichier enregistré :\n{destination}", "OK");
#else
            await Share.Default.RequestAsync(new ShareFileRequest { Title = doc.Name, File = new ShareFile(path) });
#endif
        }
        catch (ApiException ex)
        {
            await HandleAsync(page, ex);
        }
        catch (Exception ex)
        {
            await page.DisplayAlert("Erreur", $"Impossible d'enregistrer le fichier : {ex.Message}", "OK");
        }
    }

    /// <summary>Télécharge le fichier puis l'ouvre avec l'application installée sur l'appareil.</summary>
    public static async Task ViewAsync(Page page, DocumentItem doc, DocumentService docs, DocumentVersion? version = null)
    {
        try
        {
            var path = await docs.DownloadToCacheAsync(doc, version);
            await Launcher.Default.OpenAsync(new OpenFileRequest(doc.Name, new ReadOnlyFile(path)));
        }
        catch (ApiException ex)
        {
            await HandleAsync(page, ex);
        }
        catch (Exception)
        {
            await page.DisplayAlert("Ouverture impossible",
                "Aucune application de l'appareil ne peut ouvrir ce type de fichier.", "OK");
        }
    }

    /// <summary>Affiche le menu ⋮. Retourne true si les données ont changé (archivage, restauration, suppression).</summary>
    public static async Task<bool> ShowMenuAsync(Page page, DocumentItem doc, DocumentService docs)
    {
        const string details = "Voir les détails";
        const string versions = "Historique des versions";
        const string download = "Télécharger";
        var archiveLabel = doc.IsArchived ? "Restaurer depuis les archives" : "Archiver";

        // « Archiver / Restaurer » n'est proposé qu'aux rôles autorisés (le serveur le contrôle aussi)
        const string delete = "Supprimer";
        var options = new List<string> { details, versions, download };
        if (doc.CanArchive) options.Add(archiveLabel);
        if (doc.CanDelete) options.Add(delete);
        var choice = await page.DisplayActionSheet(doc.Name, "Annuler", null, options.ToArray());

        if (choice == details) { await OpenDetailAsync(doc); return false; }
        if (choice == versions) { await OpenVersionsAsync(doc); return false; }
        if (choice == download) { await DownloadAsync(page, doc, docs); return false; }
        if (choice == archiveLabel) return await ToggleArchiveAsync(page, doc, docs);
        if (choice == delete) return await MoveToTrashAsync(page, doc, docs);
        return false;
    }

    /// <summary>Met le document à la corbeille après confirmation. Retourne true si le document a été supprimé.</summary>
    public static async Task<bool> MoveToTrashAsync(Page page, DocumentItem doc, DocumentService docs)
    {
        var ok = await page.DisplayAlert("Supprimer le document",
            $"Mettre « {doc.Name} » à la corbeille ? Il pourra être restauré depuis la corbeille.",
            "Supprimer", "Annuler");
        if (!ok) return false;
        try
        {
            await docs.MoveToTrashAsync(doc);
            return true;
        }
        catch (ApiException ex)
        {
            await HandleAsync(page, ex);
            return false;
        }
    }

    public static async Task<bool> ToggleArchiveAsync(Page page, DocumentItem doc, DocumentService docs)
    {
        try
        {
            if (doc.IsArchived)
            {
                await docs.UnarchiveAsync(doc);
                await page.DisplayAlert("Document restauré", $"« {doc.Name} » est de nouveau disponible dans Documents.", "OK");
                return true;
            }

            var ok = await page.DisplayAlert("Archiver le document",
                $"Archiver « {doc.Name} » ? Il sera déplacé dans l'espace Archives et pourra être restauré.",
                "Archiver", "Annuler");
            if (!ok) return false;
            await docs.ArchiveAsync(doc);
            return true;
        }
        catch (ApiException ex)
        {
            await HandleAsync(page, ex);
            return false;
        }
    }
}
