using SmartDocs.Helpers;
using SmartDocs.Models;

namespace SmartDocs.Services;

public class UserProfile
{
    public string FullName { get; init; } = "";
    public string Department { get; init; } = "";
    public string Role { get; init; } = "Guest";
    public string JobTitle { get; init; } = "";

    public string FirstName => FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? FullName;
    public string RoleLabel => RoleInfo.Label(Role);

    /// <summary>Confort d'affichage uniquement : le serveur refuse de toute façon les actions non autorisées.</summary>
    public bool CanDeposit => Role is "Manager" or "Collaborator";
    /// <summary>Corbeille : Direction, responsables, collaborateurs (restauration) et administrateur (suppression définitive).</summary>
    public bool CanUseTrash => Role is "Direction" or "Manager" or "Collaborator" or "Admin";
    /// <summary>Journal d'activité : Direction, Auditeur et Administrateur (le serveur contrôle aussi).</summary>
    public bool CanSeeActivity => Role is "Direction" or "Auditor" or "Admin";
    public bool IsAdmin => Role == "Admin";

    public string Initials
    {
        get
        {
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            return parts.Length == 1 ? parts[0][..1].ToUpperInvariant() : $"{parts[0][..1]}{parts[^1][..1]}".ToUpperInvariant();
        }
    }

    public static UserProfile From(UserDto d)
        => new() { FullName = d.FullName, Department = d.Department, Role = d.Role, JobTitle = d.JobTitle };
}

/// <summary>Authentification réelle via l'API (JWT). Le jeton est conservé dans le stockage sécurisé de l'appareil.</summary>
public class AuthService(ApiClient api)
{
    private const string TokenKey = "smartdocs_token";

    public UserProfile CurrentUser { get; private set; } = new();

    public async Task LoginAsync(string email, string password)
        => await AcceptAsync(await api.PostAsync<AuthResponse>("/api/auth/login", new { email, password }, auth: false));

    /// <summary>Inscription sur invitation : le code est obligatoire et fixe le rôle du nouveau compte.</summary>
    public async Task RegisterAsync(string fullName, string email, string password, string inviteCode)
        => await AcceptAsync(await api.PostAsync<AuthResponse>("/api/auth/register", new { fullName, email, password, inviteCode }, auth: false));

    private async Task AcceptAsync(AuthResponse response)
    {
        api.Token = response.Token;
        CurrentUser = UserProfile.From(response.User);
        try { await SecureStorage.Default.SetAsync(TokenKey, response.Token); }
        catch { /* stockage sécurisé indisponible : la session reste valable jusqu'à la fermeture */ }
    }

    /// <summary>Reprend la session précédente si un jeton valide est stocké.</summary>
    public async Task<bool> TryRestoreAsync()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync(TokenKey);
            if (string.IsNullOrEmpty(token)) return false;
            api.Token = token;
            CurrentUser = UserProfile.From(await api.GetAsync<UserDto>("/api/auth/me"));
            return true;
        }
        catch
        {
            api.Token = null;
            return false;
        }
    }

    public void Logout()
    {
        api.Token = null;
        CurrentUser = new UserProfile();
        try { SecureStorage.Default.Remove(TokenKey); } catch { }
    }
}
