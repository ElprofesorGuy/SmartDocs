using SmartDocs.Helpers;
using SmartDocs.Services;

namespace SmartDocs.Views;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _auth;
    private readonly ApiClient _api;
    private bool _restoreTried;

    public LoginPage(AuthService auth, ApiClient api)
    {
        InitializeComponent();
        _auth = auth;
        _api = api;
        FieldFocus.Hook(EmailEntry, EmailBorder);
        FieldFocus.Hook(PasswordEntry, PasswordBorder);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_restoreTried) PasswordEntry.Text = string.Empty; // retour après une déconnexion
        await CheckServerAsync();

        // Reprise automatique de la session si un jeton valide est stocké
        if (!_restoreTried)
        {
            _restoreTried = true;
            if (await _auth.TryRestoreAsync())
                await Shell.Current.GoToAsync("//main/home");
        }
    }

    private async Task CheckServerAsync()
    {
        PortalLabel.Text = "Connexion…";
        PortalDot.Color = Color.FromArgb("#94A3B8");
        var ok = await _api.PingAsync();
        PortalLabel.Text = ok ? "Portail Actif" : "Hors ligne";
        PortalDot.Color = ok ? Color.FromArgb("#10B981") : Palette.Danger;
    }

    /// <summary>Touche sur la pastille d'état : permet de changer l'adresse de l'API (ex. téléphone sur le Wi-Fi).</summary>
    private async void OnServerTapped(object? sender, TappedEventArgs e)
    {
        var url = await DisplayPromptAsync("Adresse du serveur",
            "Adresse de l'API SmartDocs (exemple : http://192.168.1.20:5080)",
            "Enregistrer", "Annuler", initialValue: _api.BaseUrl, keyboard: Keyboard.Url);
        if (string.IsNullOrWhiteSpace(url)) return;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            await DisplayAlert("Adresse invalide", "Saisissez une adresse commençant par http:// ou https://", "OK");
            return;
        }
        _api.BaseUrl = url;
        await CheckServerAsync();
    }

    private void OnTogglePassword(object? sender, TappedEventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        EyeLabel.Text = PasswordEntry.IsPassword ? Icons.Visibility : Icons.VisibilityOff;
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text ?? "";

        if (!Validation.IsEmail(email))
        {
            ShowError("Saisissez une adresse e-mail professionnelle valide.");
            return;
        }
        if (password.Length == 0)
        {
            ShowError("Saisissez votre mot de passe.");
            return;
        }

        ErrorLabel.IsVisible = false;
        LoginButton.IsEnabled = false;
        LoginButton.Text = "Connexion…";
        try
        {
            await _auth.LoginAsync(email, password);
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
            return;
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Text = "Se connecter";
        }

        PasswordEntry.Text = string.Empty;
        await Shell.Current.GoToAsync("//main/home");
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private async void OnForgotPassword(object? sender, TappedEventArgs e)
        => await DisplayAlert("Mot de passe oublié",
            "La réinitialisation du mot de passe n'est pas encore disponible : elle nécessiterait l'envoi d'un e-mail par le serveur.", "OK");

    private async void OnMicrosoftTapped(object? sender, TappedEventArgs e)
        => await DisplayAlert("Connexion SSO",
            "La connexion Microsoft 365 / SAML / Okta n'est pas implémentée : utilisez le formulaire e-mail et mot de passe.", "OK");

    private async void OnRegisterTapped(object? sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("register");
}
