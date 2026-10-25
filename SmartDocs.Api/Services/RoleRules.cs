using SmartDocs.Api.Models;

namespace SmartDocs.Api.Services;

/// <summary>Règles communes à l'attribution d'un rôle (changement de rôle d'un compte, invitation).</summary>
public static class RoleRules
{
    public static readonly string[] Departments = { "RH", "Finance", "Direction", "Juridique" };

    /// <summary>Valide le rôle demandé et en déduit le service : obligatoire pour Responsable et Collaborateur, « Direction » pour la Direction, vide sinon.</summary>
    public static bool TryResolve(string? roleText, string? departmentText, out Role role, out string department, out string error)
    {
        department = "";
        error = "";
        if (!Enum.TryParse<Role>(roleText, ignoreCase: false, out role) || !Enum.IsDefined(role))
        {
            error = "Rôle invalide.";
            return false;
        }

        var dept = (departmentText ?? "").Trim();
        switch (role)
        {
            case Role.Manager or Role.Collaborator:
                if (!Departments.Contains(dept))
                {
                    error = "Un service valide est obligatoire pour ce rôle.";
                    return false;
                }
                department = dept;
                break;
            case Role.Direction:
                department = "Direction";
                break;
        }
        return true;
    }

    public static string DefaultTitle(Role role) => role switch
    {
        Role.Admin => "Administrateur SI",
        Role.Direction => "Direction",
        Role.Manager => "Responsable de service",
        Role.Collaborator => "Collaborateur",
        Role.Auditor => "Auditeur interne",
        _ => "Invité"
    };

    /// <summary>Libellé français du rôle (e-mails d'invitation).</summary>
    public static string Label(Role role) => role switch
    {
        Role.Admin => "Administrateur",
        Role.Direction => "Direction",
        Role.Manager => "Responsable de service",
        Role.Collaborator => "Collaborateur",
        Role.Auditor => "Auditeur",
        _ => "Invité"
    };

    /// <summary>Libellé court pour le journal : « Collaborator (Finance) ».</summary>
    public static string Show(Role role, string department)
        => string.IsNullOrEmpty(department) ? role.ToString() : $"{role} ({department})";
}
