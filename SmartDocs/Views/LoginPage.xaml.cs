using SmartDocs.Helpers;
using SmartDocs.Services;

namespace SmartDocs.Views;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _auth;

    public LoginPage(AuthService auth)
    {
        InitializeComponent();
        _auth = auth;
        FieldFocus.Hook(EmailEntry, EmailBorder);
        FieldFocus.Hook(PasswordEntry, PasswordBorder);
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

        await _auth.LoginAsync(email, password); // SIMULÉ : aucun serveur

        LoginButton.IsEnabled = true;
        LoginButton.Text = "Se connecter";
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
            "La réinitialisation du mot de passe nécessiterait un serveur d'authentification. Fonction non disponible dans cette démonstration.", "OK");

    private async void OnMicrosoftTapped(object? sender, TappedEventArgs e)
        => await DisplayAlert("Connexion SSO",
            "La connexion Microsoft 365 / SAML / Okta nécessiterait un fournisseur d'identité. Non disponible dans cette démonstration : utilisez le formulaire.", "OK");

    private async void OnRegisterTapped(object? sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("register");
}
