using System.Collections.ObjectModel;
using SmartDocs.Helpers;
using SmartDocs.Models;
using SmartDocs.Services;

namespace SmartDocs.Views;

/// <summary>Journal d'activité : qui a fait quoi et quand. Réservé à la Direction, à l'Auditeur et à l'Administrateur.</summary>
public partial class ActivityPage : ContentPage
{
    // Filtres : libellé et actions de l'API correspondantes (null = tout)
    private static readonly (string? Actions, string Label)[] Filters =
    {
        (null, "Tout"),
        ("Deposit", "Dépôts"),
        ("Download", "Consultations"),
        ("AddVersion,RestoreVersion", "Versions"),
        ("Archive,Unarchive", "Archivage"),
        ("Trash,Undelete,Purge", "Corbeille"),
        ("Login,Register,RoleChange,InviteCreate,InviteEmail,InviteRevoke", "Comptes")
    };

    private readonly ActivityService _activity;
    private readonly AuthService _auth;
    private readonly ObservableCollection<ActivityItem> _items = new();
    private string? _actions;
    private int? _before;
    private bool _busy;

    public ActivityPage(ActivityService activity, AuthService auth)
    {
        InitializeComponent();
        _activity = activity;
        _auth = auth;
        ActivityList.ItemsSource = _items;
        FieldFocus.Hook(SearchEntry, SearchBorder);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_auth.CurrentUser.CanSeeActivity)
        {
            await DisplayAlert("Accès restreint",
                "Le journal d'activité est réservé à la Direction, à l'Auditeur et à l'Administrateur.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }
        ScopeLabel.Text = _auth.CurrentUser.Role == "Admin"
            ? "Les actions sur les documents confidentiels n'apparaissent pas pour l'administrateur."
            : "Toutes les actions enregistrées par le serveur, de la plus récente à la plus ancienne.";
        await LoadAsync(reset: true);
    }

    private void RefreshChips()
        => BindableLayout.SetItemsSource(FilterChips,
            Filters.Select(f => new ChipItem(f.Actions, f.Label, f.Actions == _actions)).ToList());

    private async Task LoadAsync(bool reset)
    {
        if (_busy) return;
        _busy = true;
        try
        {
            if (reset)
            {
                _items.Clear();
                _before = null;
                RefreshChips();
            }

            var batch = await _activity.GetAsync(_actions, SearchEntry.Text, _before);
            foreach (var item in batch) _items.Add(item);
            _before = batch.Count > 0 ? batch[^1].Id : _before;
            MoreButton.IsVisible = batch.Count >= ActivityService.PageSize;
        }
        catch (ApiException ex)
        {
            await DocumentActions.HandleAsync(this, ex);
        }
        finally
        {
            _busy = false;
        }
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnChipTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not ChipItem chip) return;
        _actions = chip.Key;
        await LoadAsync(reset: true);
    }

    private async void OnSearchCompleted(object? sender, EventArgs e) => await LoadAsync(reset: true);

    private async void OnMoreClicked(object? sender, EventArgs e) => await LoadAsync(reset: false);
}
