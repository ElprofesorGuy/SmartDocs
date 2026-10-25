using System.Globalization;
using SmartDocs.Helpers;

namespace SmartDocs.Models;

/// <summary>Une ligne du journal d'activité, prête à l'affichage.</summary>
public class ActivityItem
{
    private static readonly CultureInfo Fr = new("fr-FR");

    public int Id { get; init; }
    public DateTime At { get; init; }
    public string UserName { get; init; } = "";
    public string UserRole { get; init; } = "";
    public string Action { get; init; } = "";
    public string DocumentName { get; init; } = "";
    public string Details { get; init; } = "";

    private (string Label, string Glyph, Color Fg, Color Bg) Style => Action switch
    {
        "Deposit" => ("Dépôt", Icons.CloudUpload, Palette.Primary, Palette.AccentLight),
        "Download" => ("Consultation ou téléchargement", Icons.Download, Palette.TextSecondary, Palette.NeutralBg),
        "AddVersion" => ("Nouvelle version", Icons.FileUpload, Palette.Primary, Palette.AccentLight),
        "RestoreVersion" => ("Version restaurée", Icons.Restore, Palette.Success, Palette.SuccessBg),
        "Archive" => ("Archivage", Icons.Archive, Palette.TextSecondary, Palette.NeutralBg),
        "Unarchive" => ("Restauration depuis les archives", Icons.Unarchive, Palette.Success, Palette.SuccessBg),
        "Trash" => ("Mise à la corbeille", Icons.Delete, Palette.Danger, Palette.DangerBg),
        "Undelete" => ("Restauration depuis la corbeille", Icons.Restore, Palette.Success, Palette.SuccessBg),
        "Purge" => ("Suppression définitive", Icons.DeleteForever, Palette.Danger, Palette.DangerBg),
        "Login" => ("Connexion", Icons.Lock, Palette.TextSecondary, Palette.NeutralBg),
        "Register" => ("Inscription", Icons.PersonOutline, Palette.Primary, Palette.AccentLight),
        "InviteCreate" => ("Invitation créée", Icons.MailOutline, Palette.Primary, Palette.AccentLight),
        "InviteEmail" => ("Invitation envoyée par e-mail", Icons.MailOutline, Palette.Success, Palette.SuccessBg),
        "InviteRevoke" => ("Invitation révoquée", Icons.MailOutline, Palette.Danger, Palette.DangerBg),
        "RoleChange" => ("Changement de rôle", Icons.PersonOutline, Palette.Warning, Color.FromArgb("#FEF3C7")),
        _ => (Action, Icons.History, Palette.TextSecondary, Palette.NeutralBg)
    };

    public string ActionLabel => Style.Label;
    public string Glyph => Style.Glyph;
    public Color Fg => Style.Fg;
    public Color Bg => Style.Bg;

    /// <summary>Le document concerné ; à défaut (connexion, rôle), le détail de l'action.</summary>
    public string Headline => DocumentName.Length > 0 ? DocumentName : Details;
    public bool HasHeadline => Headline.Length > 0;
    public string DetailText => DocumentName.Length > 0 ? Details : "";
    public bool HasDetail => DetailText.Length > 0;

    public string WhoText
    {
        get
        {
            var who = UserName.Length > 0 ? UserName : "Système";
            var role = UserRole.Length > 0 ? RoleInfo.Label(UserRole) : "";
            var date = At.ToString("dd/MM/yyyy HH:mm", Fr);
            return string.Join(" · ", new[] { who, role, date }.Where(s => s.Length > 0));
        }
    }
}
