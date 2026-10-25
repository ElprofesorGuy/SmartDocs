namespace SmartDocs.Helpers;

/// <summary>Libellés et couleurs des rôles (les clés correspondent à l'énumération Role de l'API).</summary>
public static class RoleInfo
{
    public static readonly (string Key, string Label)[] All =
    {
        ("Admin", "Administrateur"),
        ("Direction", "Direction"),
        ("Manager", "Responsable de service"),
        ("Collaborator", "Collaborateur"),
        ("Auditor", "Auditeur"),
        ("Guest", "Invité")
    };

    public static string Label(string role) => All.FirstOrDefault(r => r.Key == role).Label ?? role;

    public static bool NeedsDepartment(string role) => role is "Manager" or "Collaborator";

    public static (Color Fg, Color Bg) Colors(string role) => role switch
    {
        "Admin" => (Palette.Danger, Palette.DangerBg),
        "Direction" => (Palette.PrimaryDark, Color.FromArgb("#E0E7FF")),
        "Manager" => (Color.FromArgb("#1D4ED8"), Color.FromArgb("#DBEAFE")),
        "Collaborator" => (Palette.Success, Palette.SuccessBg),
        "Auditor" => (Color.FromArgb("#B45309"), Color.FromArgb("#FEF3C7")),
        _ => (Palette.TextSecondary, Palette.NeutralBg)
    };
}
