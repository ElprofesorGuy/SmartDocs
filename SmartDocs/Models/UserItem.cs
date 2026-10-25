using SmartDocs.Helpers;

namespace SmartDocs.Models;

/// <summary>Compte utilisateur affiché dans l'écran d'administration.</summary>
public class UserItem
{
    public int Id { get; init; }
    public string FullName { get; init; } = "";
    public string Email { get; init; } = "";
    public string Role { get; init; } = "Guest";
    public string Department { get; init; } = "";
    public string JobTitle { get; init; } = "";

    public string RoleLabel => RoleInfo.Label(Role);
    public Color RoleFg => RoleInfo.Colors(Role).Fg;
    public Color RoleBg => RoleInfo.Colors(Role).Bg;
    public string Meta => string.IsNullOrEmpty(Department) ? JobTitle : $"{JobTitle} • {Department}";

    public string Initials
    {
        get
        {
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            return parts.Length == 1 ? parts[0][..1].ToUpperInvariant() : $"{parts[0][..1]}{parts[^1][..1]}".ToUpperInvariant();
        }
    }
}
