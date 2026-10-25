using SmartDocs.Helpers;
using SmartDocs.Models;
using SmartDocs.Services;

namespace SmartDocs.Views;

/// <summary>Invitations : création, partage et révocation des codes qui permettent de s'inscrire (réservé aux administrateurs).</summary>
public partial class InvitationsPage : ContentPage
{
    private static readonly (string Label, int Days)[] Validities = { ("24 heures", 1), ("7 jours", 7), ("30 jours", 30) };

    private readonly AdminService _admin;
    private readonly AuthService _auth;

    public InvitationsPage(AdminService admin, AuthService auth)
    {
        InitializeComponent();
        _admin = admin;
        _auth = auth;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_auth.CurrentUser.IsAdmin)
        {
            await DisplayAlert("Accès restreint", "La gestion des invitations est réservée aux administrateurs.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            InvitationsList.ItemsSource = await _admin.GetInvitationsAsync();
        }
        catch (ApiException ex)
        {
            await DocumentActions.HandleAsync(this, ex);
        }
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnAddTapped(object? sender, TappedEventArgs e)
    {
        // 1. Rôle (et service si nécessaire) que recevra le futur compte
        var pick = await DisplayActionSheet("Rôle du futur compte", "Annuler", null, RoleInfo.All.Select(r => r.Label).ToArray());
        if (pick == null || pick == "Annuler") return;
        var role = RoleInfo.All.First(r => r.Label == pick).Key;

        var department = "";
        if (RoleInfo.NeedsDepartment(role))
        {
            var service = await DisplayActionSheet("Service", "Annuler", null, DocumentService.Categories);
            if (service == null || service == "Annuler") return;
            department = service;
        }

        // 2. Poste et adresse e-mail (facultatifs)
        var title = await DisplayPromptAsync("Intitulé du poste", "Exemple : DRH, Contrôleur de gestion (facultatif)",
            "Suivant", "Annuler", maxLength: 60);
        if (title == null) return;

        var email = await DisplayPromptAsync("Adresse e-mail", "Facultatif : réserve l'invitation à cette adresse et lui envoie le code par e-mail si le serveur est configuré pour.",
            "Suivant", "Annuler", keyboard: Keyboard.Email);
        if (email == null) return;
        email = email.Trim();
        if (email.Length > 0 && !Validation.IsEmail(email))
        {
            await DisplayAlert("Adresse invalide", "Saisissez une adresse e-mail valide ou laissez le champ vide.", "OK");
            return;
        }

        // 3. Durée de validité
        var validity = await DisplayActionSheet("Durée de validité", "Annuler", null, Validities.Select(v => v.Label).ToArray());
        if (validity == null || validity == "Annuler") return;
        var days = Validities.First(v => v.Label == validity).Days;

        try
        {
            var result = await _admin.CreateInvitationAsync(role, department, title, email, days);
            await LoadAsync();
            switch (result.EmailStatus)
            {
                case "Sent":
                    await DisplayAlert("Invitation envoyée", $"Un e-mail contenant le code a été envoyé à {result.Item.Email}.", "OK");
                    break;
                case "Failed":
                    await DisplayAlert("Invitation créée, e-mail non envoyé", $"{result.EmailError}\nTransmettez le code vous-même ou réessayez.", "OK");
                    await ShowActionsAsync(result.Item);
                    break;
                case "NotConfigured":
                    await DisplayAlert("Invitation créée", "L'envoi d'e-mails n'est pas configuré sur le serveur : transmettez le code vous-même.", "OK");
                    await ShowActionsAsync(result.Item);
                    break;
                default:
                    await ShowActionsAsync(result.Item);
                    break;
            }
        }
        catch (ApiException ex)
        {
            await DocumentActions.HandleAsync(this, ex);
        }
    }

    private async void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is InvitationItem invitation)
            await ShowActionsAsync(invitation);
    }

    /// <summary>Copier ou partager le code, ou révoquer l'invitation tant qu'elle est active.</summary>
    private async Task ShowActionsAsync(InvitationItem invitation)
    {
        if (!invitation.IsActive)
        {
            await DisplayAlert($"Invitation {invitation.StatusLabel.ToLowerInvariant()}",
                $"{invitation.Code}\n{invitation.RoleText}\n{invitation.DateText}", "OK");
            return;
        }

        const string copy = "Copier le code";
        const string share = "Partager";
        const string revoke = "Révoquer l'invitation";
        var email = invitation.EmailSentAt.HasValue ? "Renvoyer l'e-mail" : "Envoyer par e-mail";
        var buttons = invitation.HasEmail ? new[] { copy, share, email } : new[] { copy, share };
        var choice = await DisplayActionSheet($"Code {invitation.Code}", "Fermer", revoke, buttons);

        if (choice == copy)
        {
            await Clipboard.Default.SetTextAsync(invitation.Code);
            await DisplayAlert("Copié", "Le code d'invitation est dans le presse-papiers.", "OK");
        }
        else if (choice == share)
        {
            try
            {
                await Share.Default.RequestAsync(new ShareTextRequest { Title = "Invitation SmartDocs", Text = invitation.ShareText });
            }
            catch (Exception)
            {
                // Partage indisponible sur cet appareil : on copie le message complet
                await Clipboard.Default.SetTextAsync(invitation.ShareText);
                await DisplayAlert("Copié", "Le partage n'est pas disponible ici : le message d'invitation est dans le presse-papiers.", "OK");
            }
        }
        else if (choice == email)
        {
            try
            {
                await _admin.SendInvitationEmailAsync(invitation.Id);
                await DisplayAlert("E-mail envoyé", $"Le code a été envoyé à {invitation.Email}.", "OK");
                await LoadAsync();
            }
            catch (ApiException ex)
            {
                await DocumentActions.HandleAsync(this, ex);
            }
        }
        else if (choice == revoke)
        {
            if (!await DisplayAlert("Révoquer l'invitation", $"Le code {invitation.Code} ne pourra plus être utilisé.", "Révoquer", "Annuler"))
                return;
            try
            {
                await _admin.RevokeInvitationAsync(invitation.Id);
                await LoadAsync();
            }
            catch (ApiException ex)
            {
                await DocumentActions.HandleAsync(this, ex);
            }
        }
    }
}
