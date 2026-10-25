using SmartDocs.Helpers;
using SmartDocs.Models;
using SmartDocs.Services;

namespace SmartDocs.Views;

/// <summary>Corbeille : restauration (qui a le droit de supprimer) et suppression définitive (Direction, administrateur).</summary>
public partial class TrashPage : ContentPage
{
    private readonly DocumentService _docs;

    public TrashPage(DocumentService docs)
    {
        InitializeComponent();
        _docs = docs;
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
            var items = await _docs.GetTrashAsync();
            TrashList.ItemsSource = items;
            CountLabel.Text = items.Count <= 1 ? $"{items.Count} document" : $"{items.Count} documents";
        }
        catch (ApiException ex)
        {
            await DocumentActions.HandleAsync(this, ex);
        }
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("..");

    private static DocumentItem? ItemOf(object? sender) => (sender as BindableObject)?.BindingContext as DocumentItem;

    private async void OnRestoreClicked(object? sender, EventArgs e)
    {
        var doc = ItemOf(sender);
        if (doc == null) return;
        try
        {
            await _docs.UndeleteAsync(doc);
        }
        catch (ApiException ex)
        {
            await DocumentActions.HandleAsync(this, ex);
            return;
        }
        await LoadAsync();
        await DisplayAlert("Document restauré", $"« {doc.Name} » a été restauré.", "OK");
    }

    private async void OnPurgeClicked(object? sender, EventArgs e)
    {
        var doc = ItemOf(sender);
        if (doc == null) return;

        var ok = await DisplayAlert("Suppression définitive",
            $"Supprimer définitivement « {doc.Name} » et toutes ses versions ? Cette action est irréversible.",
            "Supprimer définitivement", "Annuler");
        if (!ok) return;

        try
        {
            await _docs.PurgeAsync(doc);
        }
        catch (ApiException ex)
        {
            await DocumentActions.HandleAsync(this, ex);
            return;
        }
        await LoadAsync();
    }
}
