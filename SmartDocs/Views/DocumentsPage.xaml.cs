using SmartDocs.Controls;
using SmartDocs.Helpers;
using SmartDocs.Models;
using SmartDocs.Services;

namespace SmartDocs.Views;

[QueryProperty(nameof(SearchQuery), "q")]
public partial class DocumentsPage : ContentPage
{
    private readonly DocumentService _docs;
    private readonly AuthService _auth;
    private string? _category;
    private string? _extension;
    private string _sort = "recent";
    private string? _pendingQuery;
    private bool _loading;

    public DocumentsPage(DocumentService docs, AuthService auth)
    {
        InitializeComponent();
        _docs = docs;
        _auth = auth;
        FieldFocus.Hook(SearchEntry, (Border)SearchEntry.Parent.Parent);
    }

    private async void OnAccountTapped(object? sender, TappedEventArgs e)
        => await AccountMenu.ShowAsync(this, _auth, _docs);

    /// <summary>Reçu depuis la page d'accueil (« //main/documents?q=... »).</summary>
    public string SearchQuery
    {
        set => _pendingQuery = Uri.UnescapeDataString(value ?? string.Empty);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Fab.IsVisible = _auth.CurrentUser.CanDeposit;
        _loading = true;
        if (_pendingQuery != null)
        {
            SearchEntry.Text = _pendingQuery;
            _pendingQuery = null;
        }
        _loading = false;
        Reload(); // cache courant (vide après une déconnexion) avant la réponse du serveur
        await DocumentActions.RefreshAsync(this, _docs);
        Reload();
    }

    private void Reload()
    {
        BindableLayout.SetItemsSource(ChipsLayout, _docs.BuildCategoryChips(_category));

        var results = _docs.Query(archived: false, text: SearchEntry.Text, category: _category,
                                  extension: _extension, sort: _sort);
        DocsList.ItemsSource = results;
        CountLabel.Text = results.Count <= 1 ? $"{results.Count} document trouvé" : $"{results.Count} documents trouvés";
        SortLabel.Text = _sort switch { "name" => "Nom A-Z", "size" => "Taille", _ => "Récents" };

        // Le bouton de filtre est « allumé » lorsqu'un filtre de type est actif
        FilterButton.BackgroundColor = _extension == null ? Palette.Surface : Palette.AccentLight;
        FilterButton.Stroke = _extension == null ? Palette.Border : Palette.Primary;
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_loading) Reload();
    }

    private void OnChipTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not ChipItem chip) return;
        _category = chip.Key;
        Reload();
    }

    private async void OnSortTapped(object? sender, TappedEventArgs e)
    {
        var choice = await DisplayActionSheet("Trier par", "Annuler", null, "Récents", "Nom A-Z", "Taille");
        if (choice == null || choice == "Annuler") return;
        _sort = choice switch { "Nom A-Z" => "name", "Taille" => "size", _ => "recent" };
        Reload();
    }

    private async void OnFilterTapped(object? sender, TappedEventArgs e)
    {
        var choice = await DisplayActionSheet("Filtrer par type de fichier", "Annuler", null,
            "Tous les types", "PDF", "Word (DOCX)", "Excel (XLSX)");
        if (choice == null || choice == "Annuler") return;
        _extension = choice switch { "PDF" => "pdf", "Word (DOCX)" => "docx", "Excel (XLSX)" => "xlsx", _ => null };
        Reload();
    }

    private static DocumentItem? ItemOf(object? sender) => (sender as BindableObject)?.BindingContext as DocumentItem;

    private async void OnCardTapped(object? sender, TappedEventArgs e)
    {
        var doc = ItemOf(sender);
        if (doc != null) await DocumentActions.OpenDetailAsync(doc);
    }

    private async void OnViewClicked(object? sender, EventArgs e)
    {
        var doc = ItemOf(sender);
        if (doc != null) await DocumentActions.OpenDetailAsync(doc);
    }

    private async void OnDownloadClicked(object? sender, EventArgs e)
    {
        var doc = ItemOf(sender);
        if (doc != null) await DocumentActions.DownloadAsync(this, doc, _docs);
    }

    private async void OnMoreTapped(object? sender, TappedEventArgs e)
    {
        var doc = ItemOf(sender);
        if (doc == null) return;
        if (await DocumentActions.ShowMenuAsync(this, doc, _docs)) Reload();
    }

    private async void OnFabTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/upload");
}
