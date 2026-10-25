using System.Globalization;
using SmartDocs.Helpers;
using SmartDocs.Models;
using SmartDocs.Services;

namespace SmartDocs.Views;

public partial class ArchivesPage : ContentPage
{
    private readonly DocumentService _docs;
    private readonly AuthService _auth;
    private int? _year;
    private bool _ascending;

    public ArchivesPage(DocumentService docs, AuthService auth)
    {
        InitializeComponent();
        _docs = docs;
        _auth = auth;
        FieldFocus.Hook(SearchEntry, SearchBorder);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Reload();
        await DocumentActions.RefreshAsync(this, _docs);
        Reload();
    }

    private void Reload()
    {
        var count = _docs.CountArchived;
        TotalLabel.Text = count <= 1 ? $"{count} document" : $"{count} documents";
        UsedBadge.Text = $"{_docs.ArchivedSizeMb.ToString("0.0", CultureInfo.InvariantCulture)} Mo utilisés";

        BindableLayout.SetItemsSource(YearChips, _docs.BuildYearChips(_year));
        ArchiveList.ItemsSource = _docs.Query(archived: true, text: SearchEntry.Text, archiveYear: _year, ascendingDate: _ascending);

        SortArrow.Text = _ascending ? Icons.ArrowUpward : Icons.ArrowDownward;
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e) => Reload();

    private void OnChipTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not ChipItem chip) return;
        _year = int.TryParse(chip.Key, out var y) ? y : null;
        Reload();
    }

    private void OnSortTapped(object? sender, TappedEventArgs e)
    {
        _ascending = !_ascending;
        Reload();
    }

    private async void OnMicTapped(object? sender, TappedEventArgs e)
        => await DisplayAlert("Recherche vocale", "La dictée vocale n'est pas implémentée dans cette démonstration.", "OK");

    private async void OnHelpTapped(object? sender, TappedEventArgs e)
        => await DisplayAlert("Archives",
            "Les pièces archivées sont conservées en lecture seule. « Restaurer » les replace dans la liste Documents.\n\nLa durée de rétention affichée est indicative : aucune règle de rétention n'est appliquée dans cette démonstration.",
            "OK");

    private async void OnMenuTapped(object? sender, TappedEventArgs e)
        => await AccountMenu.ShowAsync(this, _auth, _docs);

    private static DocumentItem? ItemOf(object? sender) => (sender as BindableObject)?.BindingContext as DocumentItem;

    private async void OnCardTapped(object? sender, TappedEventArgs e)
    {
        var doc = ItemOf(sender);
        if (doc != null) await DocumentActions.OpenDetailAsync(doc);
    }

    private async void OnMoreTapped(object? sender, TappedEventArgs e)
    {
        var doc = ItemOf(sender);
        if (doc == null) return;
        if (await DocumentActions.ShowMenuAsync(this, doc, _docs)) Reload();
    }

    private async void OnRestoreClicked(object? sender, EventArgs e)
    {
        var doc = ItemOf(sender);
        if (doc == null) return;
        try
        {
            await _docs.UnarchiveAsync(doc);
        }
        catch (ApiException ex)
        {
            await DocumentActions.HandleAsync(this, ex);
            return;
        }
        Reload();
        await DisplayAlert("Document restauré", $"« {doc.Name} » est de nouveau disponible dans Documents.", "OK");
    }
}
