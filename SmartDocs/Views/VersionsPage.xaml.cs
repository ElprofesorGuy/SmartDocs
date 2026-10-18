using SmartDocs.Helpers;
using SmartDocs.Models;
using SmartDocs.Services;

namespace SmartDocs.Views;

[QueryProperty(nameof(DocumentId), "id")]
public partial class VersionsPage : ContentPage
{
    private readonly DocumentService _docs;
    private readonly AuthService _auth;
    private DocumentItem? _doc;
    private string? _id;

    public VersionsPage(DocumentService docs, AuthService auth)
    {
        InitializeComponent();
        _docs = docs;
        _auth = auth;
    }

    public string DocumentId
    {
        set { _id = value; Load(); }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Load();
    }

    private void Load()
    {
        _doc = _docs.GetById(_id);
        if (_doc == null) return;

        FileGlyphLabel.Text = _doc.FileGlyph;
        FileNameLabel.Text = _doc.Name;

        var versions = _docs.GetVersions(_doc.Id);
        var current = versions.FirstOrDefault(v => v.IsCurrent);
        if (current != null)
        {
            CurrentTitle.Text = current.HeaderText;
            CurrentPublished.Text = current.PublishedText;
            CurrentBy.Text = current.ByText;
            CurrentQuote.Text = current.QuoteText;
        }
        BindableLayout.SetItemsSource(OldVersionsLayout, versions.Where(v => !v.IsCurrent).ToList());
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnMoreTapped(object? sender, TappedEventArgs e)
    {
        if (_doc == null) return;
        if (await DocumentActions.ShowMenuAsync(this, _doc, _docs))
            await Shell.Current.GoToAsync("..");
    }

    private async void OnDownloadCurrent(object? sender, EventArgs e)
    {
        if (_doc != null) await DocumentActions.DownloadAsync(this, _doc);
    }

    private async void OnDownloadOld(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not DocumentVersion v || _doc == null) return;
        await DisplayAlert("Téléchargement simulé",
            $"{_doc.Name} — {v.Title} ({v.SizeText}).\n\nAucun fichier réel n'est téléchargé : cette démonstration n'a pas de serveur.", "OK");
    }

    private async void OnRestoreClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not DocumentVersion v || _doc == null) return;

        var ok = await DisplayAlert("Restaurer la version",
            $"Restaurer la {v.Title.ToLowerInvariant()} ? Une nouvelle version active sera créée ; l'historique est conservé.",
            "Restaurer", "Annuler");
        if (!ok) return;

        var created = _docs.RestoreVersion(_doc.Id, v.Id, _auth.CurrentUser.FullName);
        Load();
        if (created != null)
            await DisplayAlert("Version restaurée", $"La version {created.Number} est maintenant la version active.", "OK");
    }
}
