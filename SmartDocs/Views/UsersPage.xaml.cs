using SmartDocs.Helpers;
using SmartDocs.Models;
using SmartDocs.Services;

namespace SmartDocs.Views;

/// <summary>Administration des comptes : attribution du rôle, du service et du poste (réservé aux administrateurs).</summary>
public partial class UsersPage : ContentPage
{
    private readonly AdminService _admin;

    public UsersPage(AdminService admin)
    {
        InitializeComponent();
        _admin = admin;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            UsersList.ItemsSource = await _admin.GetUsersAsync();
        }
        catch (ApiException ex)
        {
            await DocumentActions.HandleAsync(this, ex);
        }
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnInvitationsTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("invitations");

    private async void OnUserTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not UserItem user) return;

        var labels = RoleInfo.All.Select(r => r.Label).ToArray();
        var pick = await DisplayActionSheet($"Rôle de {user.FullName}", "Annuler", null, labels);
        if (pick == null || pick == "Annuler") return;
        var role = RoleInfo.All.First(r => r.Label == pick).Key;

        var department = "";
        if (RoleInfo.NeedsDepartment(role))
        {
            var service = await DisplayActionSheet("Service", "Annuler", null, DocumentService.Categories);
            if (service == null || service == "Annuler") return;
            department = service;
        }

        var title = await DisplayPromptAsync("Intitulé du poste", "Exemple : DRH, Contrôleur de gestion (facultatif)",
            "Enregistrer", "Annuler", initialValue: user.JobTitle, maxLength: 60);
        if (title == null) return;

        try
        {
            await _admin.UpdateUserAsync(user.Id, role, department, title);
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            await DocumentActions.HandleAsync(this, ex);
        }
    }
}
