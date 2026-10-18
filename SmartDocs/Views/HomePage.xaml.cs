using SmartDocs.Helpers;
using SmartDocs.Models;
using SmartDocs.Services;

namespace SmartDocs.Views;

public partial class HomePage : ContentPage
{
    private readonly DocumentService _docs;
    private readonly AuthService _auth;

    public HomePage(DocumentService docs, AuthService auth)
    {
        InitializeComponent();
        _docs = docs;
        _auth = auth;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
    }

    private void Refresh()
    {
        var user = _auth.CurrentUser;
        GreetingLabel.Text = $"Bonjour, {user.FirstName}";
        DepartmentLabel.Text = user.Department;
        AvatarLabel.Text = user.Initials;

        DocsCountBadge.Text = _docs.CountActive.ToString();
        ArchivesCountBadge.Text = _docs.CountArchived.ToString();
        VersionsCountBadge.Text = $"{_docs.VersionUpdates} maj";

        BindableLayout.SetItemsSource(RecentsLayout, _docs.GetRecents());
    }

    private async void OnMenuTapped(object? sender, TappedEventArgs e)
    {
        var choice = await DisplayActionSheet("SmartDocs", "Fermer", null, "Se déconnecter");
        if (choice == "Se déconnecter")
        {
            _auth.Logout();
            await Shell.Current.GoToAsync("//login");
        }
    }

    private async Task GoSearchAsync()
    {
        var q = SearchEntry.Text?.Trim() ?? "";
        SearchEntry.Text = string.Empty;
        await Shell.Current.GoToAsync(q.Length > 0
            ? $"//main/documents?q={Uri.EscapeDataString(q)}"
            : "//main/documents");
    }

    private async void OnSearchCompleted(object? sender, EventArgs e) => await GoSearchAsync();
    private async void OnSearchIconTapped(object? sender, TappedEventArgs e) => await GoSearchAsync();
    private async void OnDocumentsTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/documents");
    private async void OnUploadTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/upload");
    private async void OnArchivesTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/archives");

    private async void OnVersionsTapped(object? sender, TappedEventArgs e)
    {
        var withHistory = _docs.GetDocumentsWithHistory();
        if (withHistory.Count == 0)
        {
            await DisplayAlert("Versions", "Aucun document ne possède encore d'historique de versions.", "OK");
            return;
        }
        var names = withHistory.Select(d => d.Name).ToArray();
        var choice = await DisplayActionSheet("Historique des versions — choisir un document", "Annuler", null, names);
        var doc = withHistory.FirstOrDefault(d => d.Name == choice);
        if (doc != null) await DocumentActions.OpenVersionsAsync(doc);
    }

    private static DocumentItem? ItemOf(object? sender) => (sender as BindableObject)?.BindingContext as DocumentItem;

    private async void OnRecentTapped(object? sender, TappedEventArgs e)
    {
        var doc = ItemOf(sender);
        if (doc != null) await DocumentActions.OpenDetailAsync(doc);
    }

    private async void OnRecentMoreTapped(object? sender, TappedEventArgs e)
    {
        var doc = ItemOf(sender);
        if (doc == null) return;
        if (await DocumentActions.ShowMenuAsync(this, doc, _docs)) Refresh();
    }
}
