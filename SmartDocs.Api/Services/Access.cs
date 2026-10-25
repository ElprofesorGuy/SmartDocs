using System.Linq.Expressions;
using SmartDocs.Api.Models;

namespace SmartDocs.Api.Services;

/// <summary>
/// Règles d'accès (source de vérité unique, appliquée côté serveur).
/// Confidentialité : Public = tous ; Interne = tous sauf Invité ;
/// Confidentiel = Direction, Auditeur, membres du service concerné et propriétaire.
/// </summary>
public static class Access
{
    /// <summary>Filtre SQL : documents que l'utilisateur a le droit de voir.</summary>
    public static Expression<Func<Document, bool>> Visible(User u)
    {
        var dept = u.Department;
        var id = u.Id;
        return u.Role switch
        {
            Role.Guest => d => d.Confidentiality == "Public",
            Role.Direction or Role.Auditor => d => true,
            Role.Manager or Role.Collaborator => d => d.Confidentiality != "Confidentiel" || d.Category == dept || d.OwnerId == id,
            _ => d => d.Confidentiality != "Confidentiel" || d.OwnerId == id // Admin : pas de lecture des confidentiels d'autrui
        };
    }

    public static bool CanSee(User u, Document d) => Visible(u).Compile()(d);

    private static bool IsStaff(User u) => u.Role is Role.Manager or Role.Collaborator;

    /// <summary>Déposer un document : responsables et collaborateurs, uniquement dans leur service.</summary>
    public static bool CanDeposit(User u, string category) => IsStaff(u) && u.Department == category;

    /// <summary>Ajouter / restaurer une version : responsables et collaborateurs, documents de leur service.</summary>
    public static bool CanAddVersion(User u, Document d) => IsStaff(u) && u.Department == d.Category && CanSee(u, d);

    /// <summary>Archiver / restaurer depuis les archives : Direction (tout), responsable (son service), collaborateur (ses documents).</summary>
    public static bool CanArchive(User u, Document d) => u.Role switch
    {
        Role.Direction => true,
        Role.Manager => u.Department == d.Category,
        Role.Collaborator => u.Department == d.Category && d.OwnerId == u.Id,
        _ => false
    };

    /// <summary>Mettre à la corbeille / restaurer depuis la corbeille : mêmes droits que l'archivage.</summary>
    public static bool CanDelete(User u, Document d) => CanArchive(u, d);

    /// <summary>Suppression définitive (document déjà dans la corbeille) : Direction (tout) et Administrateur (documents qu'il peut voir).</summary>
    public static bool CanPurge(User u, Document d) => u.Role == Role.Direction || (u.Role == Role.Admin && CanSee(u, d));

    /// <summary>La corbeille ne montre que les documents sur lesquels l'utilisateur peut agir.</summary>
    public static bool CanSeeInTrash(User u, Document d) => CanSee(u, d) && (CanDelete(u, d) || CanPurge(u, d));

    /// <summary>Journal d'activité : Direction et Auditeur (tout), Administrateur (hors documents confidentiels).</summary>
    public static bool CanSeeActivity(User u) => u.Role is Role.Direction or Role.Auditor or Role.Admin;

    public static Expression<Func<ActivityLog, bool>> ActivityVisible(User u) => u.Role switch
    {
        Role.Direction or Role.Auditor => l => true,
        Role.Admin => l => l.Confidentiality != "Confidentiel", // l'administrateur ne lit pas les confidentiels, ni leur trace
        _ => l => false
    };

    public static bool IsAdmin(User u) => u.Role == Role.Admin;
}
