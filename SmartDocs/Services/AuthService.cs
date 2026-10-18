using System.Globalization;

namespace SmartDocs.Services;

public class UserProfile
{
    public string FullName { get; init; } = "Alexandre Martin";
    public string Department { get; init; } = "Département RH";

    public string FirstName => FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? FullName;

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

/// <summary>
/// AUTHENTIFICATION SIMULÉE : aucun serveur, aucun contrôle de mot de passe.
/// Tout couple e-mail / mot de passe non vide est accepté. Rien n'est persisté.
/// </summary>
public class AuthService
{
    public UserProfile CurrentUser { get; private set; } = new();

    public async Task LoginAsync(string email, string password)
    {
        await Task.Delay(500); // simule un appel réseau
        var local = email.Split('@')[0];
        var parts = local.Split(new[] { '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        var text = CultureInfo.CurrentCulture.TextInfo;
        var name = parts.Length >= 2
            ? string.Join(' ', parts.Select(p => text.ToTitleCase(p.ToLowerInvariant())))
            : "Alexandre Martin";
        CurrentUser = new UserProfile { FullName = name };
    }

    public async Task RegisterAsync(string fullName, string email)
    {
        await Task.Delay(600);
        CurrentUser = new UserProfile { FullName = fullName.Trim() };
    }

    public void Logout() => CurrentUser = new UserProfile();
}
