using SmartDocs.Helpers;
using SmartDocs.Models;
using SmartDocs.Services;

namespace SmartDocs.Views;

[QueryProperty(nameof(DocumentId), "id")]
public partial class DocumentDetailPage : ContentPage
{
    private readonly DocumentService _docs;
    private DocumentItem? _doc;
    private string? _id;

    public DocumentDetailPage(DocumentService docs)
    {
        InitializeComponent();
        _docs = docs;
    }

    public string DocumentId
    {
        set => _id = value;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await DocumentActions.RefreshAsync(this, _docs)) return;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _doc = _docs.GetById(_id);
        if (_doc == null) return;

        BindingContext = _doc;

        List<DocumentVersion> versions;
        try
        {
            versions = await _docs.GetVersionsAsync(_doc.Id);
        }
        catch (ApiException ex)
        {
            await DocumentActions.HandleAsync(this, ex);
            return;
        }

        // Anciennes versions (la version courante figure déjà dans « Informations clés »)
        var old = versions.Where(v => !v.IsCurrent).ToList();
        HistoryTitle.Text = $"Historique des versions ({old.Count})";
        BindableLayout.SetItemsSource(VersionsLayout, old.Take(3).ToList());

        DeleteButton.IsVisible = _doc.CanDelete;
        ArchiveButton.IsVisible = _doc.CanArchive;
        ArchiveButton.Text = _doc.IsArchived ? "Restaurer depuis les archives" : "Archiver ce document";
        ArchiveButton.Glyph = _doc.IsArchived ? Icons.Unarchive : Icons.Archive;
        ArchiveButton.Variant = _doc.IsArchived ? Controls.ButtonVariant.OutlinePrimary : Controls.ButtonVariant.DangerOutline;
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnFavoriteTapped(object? sender, TappedEventArgs e)
    {
        if (_doc == null) return;
        try
        {
            await _docs.ToggleFavoriteAsync(_doc);
        }
        catch (ApiException ex)
        {
            await DocumentActions.HandleAsync(this, ex);
        }
    }

    private async void OnShareTapped(object? sender, TappedEventArgs e)
    {
        if (_doc == null) return;
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Partager le document",
            Text = $"{_doc.Name} ({_doc.SizeText}) — {_doc.CategoryPath}"
        });
    }

    private async void OnViewClicked(object? sender, EventArgs e)
    {
        if (_doc != null) await DocumentActions.ViewAsync(this, _doc, _docs);
    }

    private async void OnDownloadClicked(object? sender, EventArgs e)
    {
        if (_doc != null) await DocumentActions.DownloadAsync(this, _doc, _docs);
    }

    private async void OnSeeAllVersions(object? sender, TappedEventArgs e)
    {
        if (_doc != null) await DocumentActions.OpenVersionsAsync(_doc);
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_doc == null) return;
        if (await DocumentActions.MoveToTrashAsync(this, _doc, _docs))
            await Shell.Current.GoToAsync("..");
    }

    private async void OnArchiveClicked(object? sender, EventArgs e)
    {
        if (_doc == null) return;
        if (await DocumentActions.ToggleArchiveAsync(this, _doc, _docs))
            await Shell.Current.GoToAsync("..");
    }
}
