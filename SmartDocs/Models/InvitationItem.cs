using System.Globalization;
using SmartDocs.Helpers;

namespace SmartDocs.Models;

/// <summary>Une invitation affichée dans l'écran d'administration.</summary>
public class InvitationItem
{
    private static readonly CultureInfo Fr = new("fr-FR");

    public int Id { get; init; }
    public string Code { get; init; } = "";
    public string Role { get; init; } = "Guest";
    public string Department { get; init; } = "";
    public string JobTitle { get; init; } = "";
    public string Email { get; init; } = "";
    public DateTime ExpiresAt { get; init; }
    public DateTime? UsedAt { get; init; }
    public string UsedBy { get; init; } = "";
    public DateTime? EmailSentAt { get; init; }
    /// <summary>Active, Used, Expired ou Revoked (calculé par le serveur).</summary>
    public string Status { get; init; } = "Active";

    public static InvitationItem From(InvitationDto d) => new()
    {
        Id = d.Id, Code = d.Code, Role = d.Role, Department = d.Department, JobTitle = d.JobTitle, Email = d.Email,
        ExpiresAt = d.ExpiresAt.ToLocalTime(), UsedAt = d.UsedAt?.ToLocalTime(), UsedBy = d.UsedBy, Status = d.Status, EmailSentAt = d.EmailSentAt?.ToLocalTime()
    };

    public bool IsActive => Status == "Active";

    public string RoleText => string.IsNullOrEmpty(Department) ? RoleInfo.Label(Role) : $"{RoleInfo.Label(Role)} • {Department}";
    public bool HasEmail => Email.Length > 0;
    public string Target => !HasEmail ? "Toute adresse e-mail"
        : EmailSentAt.HasValue ? $"Réservée à {Email} • e-mail envoyé le {EmailSentAt.Value.ToString("dd/MM/yyyy", Fr)}"
        : $"Réservée à {Email}";

    public string StatusLabel => Status switch
    {
        "Used" => "Utilisée",
        "Expired" => "Expirée",
        "Revoked" => "Révoquée",
        _ => "Active"
    };

    public Color StatusFg => Status switch
    {
        "Active" => Palette.Success,
        "Revoked" => Palette.Danger,
        _ => Palette.TextSecondary
    };

    public Color StatusBg => Status switch
    {
        "Active" => Palette.SuccessBg,
        "Revoked" => Palette.DangerBg,
        _ => Palette.NeutralBg
    };

    public string DateText => Status switch
    {
        "Used" => $"Utilisée par {UsedBy} le {UsedAt?.ToString("dd/MM/yyyy", Fr)}",
        "Revoked" => "Révoquée par un administrateur",
        "Expired" => $"Expirée le {ExpiresAt.ToString("dd/MM/yyyy HH:mm", Fr)}",
        _ => $"Valable jusqu'au {ExpiresAt.ToString("dd/MM/yyyy HH:mm", Fr)}"
    };

    /// <summary>Message prêt à être envoyé à la personne invitée.</summary>
    public string ShareText =>
        $"Vous êtes invité(e) à rejoindre SmartDocs.\n" +
        $"Code d'invitation : {Code}\n" +
        $"Rôle : {RoleText}\n" +
        (Email.Length > 0 ? $"À utiliser avec l'adresse e-mail : {Email}\n" : "") +
        $"Valable jusqu'au {ExpiresAt.ToString("dd/MM/yyyy HH:mm", Fr)}.\n" +
        "Dans l'application : Se connecter > S'inscrire, puis saisissez ce code.";
}
