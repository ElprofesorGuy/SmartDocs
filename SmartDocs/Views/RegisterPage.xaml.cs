using SmartDocs.Controls;
using SmartDocs.Helpers;
using SmartDocs.Services;

namespace SmartDocs.Views;

public partial class RegisterPage : ContentPage
{
    private readonly AuthService _auth;

    public RegisterPage(AuthService auth)
    {
        InitializeComponent();
        _auth = auth;
        FieldFocus.Hook(NameEntry, NameBorder);
        FieldFocus.Hook(EmailEntry, EmailBorder);
        FieldFocus.Hook(PasswordEntry, PasswordBorder);
        FieldFocus.Hook(ConfirmEntry, ConfirmBorder);
        UpdateForm();
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("..");

    private void OnTogglePassword(object? sender, TappedEventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        ConfirmEntry.IsPassword = PasswordEntry.IsPassword;
        EyeLabel.Text = PasswordEntry.IsPassword ? Icons.Visibility : Icons.VisibilityOff;
    }

    private void OnFormChanged(object? sender, TextChangedEventArgs e) => UpdateForm();
    private void OnTermsChanged(object? sender, CheckedChangedEventArgs e) => UpdateForm();

    private static void SetRule(Badge badge, bool ok)
    {
        badge.BackgroundColor = ok ? Palette.SuccessBg : Palette.NeutralBg;
        badge.TextColor = ok ? Palette.Success : Palette.TextSecondary;
    }

    private void UpdateForm()
    {
        var pwd = PasswordEntry.Text ?? "";
        var emailOk = Validation.IsEmail(EmailEntry.Text);
        var lenOk = Validation.HasMinLength(pwd);
        var upOk = Validation.HasUpper(pwd);
        var digOk = Validation.HasDigit(pwd);
        var score = (lenOk ? 1 : 0) + (upOk ? 1 : 0) + (digOk ? 1 : 0);
        var confirmOk = pwd.Length > 0 && pwd == (ConfirmEntry.Text ?? "");

        // E-mail
        EmailOkHint.IsVisible = emailOk;
        EmailCheck.IsVisible = emailOk;
        EmailBorder.Stroke = emailOk ? Palette.Success : Palette.Border;

        // Règles du mot de passe
        SetRule(RuleLength, lenOk);
        SetRule(RuleUpper, upOk);
        SetRule(RuleDigit, digOk);

        // Jauge de robustesse
        var color = score switch { 3 => Palette.Success, 2 => Palette.Warning, _ => Palette.Danger };
        var bars = new[] { Bar1, Bar2, Bar3 };
        for (var i = 0; i < bars.Length; i++)
            bars[i].Color = pwd.Length > 0 && i < Math.Max(score, 1) ? color : Palette.Border;
        StrengthLabel.Text = pwd.Length == 0 ? "" : score switch { 3 => "Robuste", 2 => "Moyen", _ => "Faible" };
        StrengthLabel.TextColor = color;

        // Confirmation
        ConfirmCheck.IsVisible = confirmOk;
        ConfirmBorder.Stroke = confirmOk ? Palette.Success : Palette.Border;

        // Bouton actif uniquement si tout est valide
        SubmitButton.IsEnabled =
            !string.IsNullOrWhiteSpace(NameEntry.Text) && emailOk && score == 3 && confirmOk && TermsCheck.IsChecked;
    }

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        SubmitButton.IsEnabled = false;
        SubmitButton.Text = "Création…";
        await _auth.RegisterAsync(NameEntry.Text ?? "", EmailEntry.Text ?? ""); // SIMULÉ : aucun compte réel n'est créé
        await DisplayAlert("Compte créé (simulation)",
            "Aucune donnée n'a été envoyée ni enregistrée : cette démonstration n'a pas de serveur.", "Continuer");
        await Shell.Current.GoToAsync("//main/home");
    }
}
