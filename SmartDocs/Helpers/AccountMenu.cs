using SmartDocs.Services;

namespace SmartDocs.Helpers;

/// <summary>Menu du compte (profil, corbeille, journal, gestion des utilisateurs et invitations, déconnexion), partagé par plusieurs écrans.</summary>
public static class AccountMenu
{
    public static async Task ShowAsync(Page page, AuthService auth, DocumentService docs)
    {
        var user = auth.CurrentUser;
        const string profile = "Mon profil";
        const string trash = "Corbeille";
        const string users = "Gérer les utilisateurs";
        const string activity = "Journal d'activité";
        const string invitations = "Invitations";
        const string logout = "Se déconnecter";

        var options = new List<string> { profile };
        if (user.CanUseTrash) options.Add(trash);
        if (user.CanSeeActivity) options.Add(activity);
        if (user.IsAdmin) options.Add(users);
        if (user.IsAdmin) options.Add(invitations);
        options.Add(logout);

        var choice = await page.DisplayActionSheet($"{user.FullName} — {user.RoleLabel}", "Fermer", null, options.ToArray());
        if (choice == profile)
        {
            var service = string.IsNullOrEmpty(user.Department) ? "—" : user.Department;
            await page.DisplayAlert("Mon profil", $"{user.FullName}\n{user.JobTitle}\nRôle : {user.RoleLabel}\nService : {service}", "OK");
        }
        else if (choice == trash)
        {
            await Shell.Current.GoToAsync("trash");
        }
        else if (choice == activity)
        {
            await Shell.Current.GoToAsync("activity");
        }
        else if (choice == users)
        {
            await Shell.Current.GoToAsync("users");
        }
        else if (choice == invitations)
        {
            await Shell.Current.GoToAsync("invitations");
        }
        else if (choice == logout)
        {
            auth.Logout();
            docs.Clear(); // aucune donnée du compte précédent ne reste en mémoire
            await Shell.Current.GoToAsync("//login");
        }
    }
}
