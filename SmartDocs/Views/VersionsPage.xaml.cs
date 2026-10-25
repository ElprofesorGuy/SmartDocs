using SmartDocs.Helpers;
using SmartDocs.Models;
using SmartDocs.Services;

namespace SmartDocs.Views;

[QueryProperty(nameof(DocumentId), "id")]
public partial class VersionsPage : ContentPage
{
    private readonly DocumentService _docs;
    private DocumentItem? _doc;
    private string? _id;

    public VersionsPage(DocumentService docs)
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

        FileGlyphLabel.Text = _doc.FileGlyph;
        FileNameLabel.Text = _doc.Name;

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

        var current = versions.FirstOrDefault(v => v.IsCurrent);
        if (current != null)
        {
            CurrentTitle.Text = current.HeaderText;
            CurrentPublished.Text = current.PublishedText;
            CurrentBy.Text = current.ByText;
            CurrentQuote.Text = current.QuoteText;
        }
        NewVersionButton.IsVisible = _doc.CanAddVersion;
        foreach (var v in versions) v.CanRestore = _doc.CanAddVersion;
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
        if (_doc != null) await DocumentActions.DownloadAsync(this, _doc, _docs);
    }

    private async void OnDownloadOld(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not DocumentVersion v || _doc == null) return;
        await DocumentActions.DownloadAsync(this, _doc, _docs, v);
    }

    private async void OnNewVersionClicked(object? sender, EventArgs e)
    {
        if (_doc == null) return;
        const long maxBytes = 10L * 1024 * 1024;
        try
        {
            // Le serveur exige le même format que le document : le sélecteur est limité à celui-ci
            var ext = _doc.Extension.ToLowerInvariant();
            var mime = ext switch
            {
                "pdf" => "application/pdf",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
            var types = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { mime } },
                { DevicePlatform.WinUI, new[] { "." + ext } }
            });
            var picked = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = $"Nouvelle version ({ext.ToUpperInvariant()})",
                FileTypes = types
            });
            if (picked == null) return;

            if (!string.Equals(Path.GetExtension(picked.FileName).TrimStart('.'), ext, StringComparison.OrdinalIgnoreCase))
            {
                await DisplayAlert("Format incorrect", $"La nouvelle version doit être un fichier {ext.ToUpperInvariant()}.", "OK");
                return;
            }

            await using var stream = await picked.OpenReadAsync();
            if (stream.CanSeek && stream.Length > maxBytes)
            {
                await DisplayAlert("Fichier trop volumineux", "Le fichier dépasse la taille maximale de 10 Mo.", "OK");
                return;
            }

            // Commentaire facultatif (null = annulation de l'opération)
            var comment = await DisplayPromptAsync("Nouvelle version", "Décrivez brièvement la modification (facultatif).",
                "Publier", "Annuler", "Ex. : correction des montants", 300);
            if (comment == null) return;

            NewVersionButton.IsEnabled = false;
            NewVersionButton.Text = "Envoi en cours…";
            var created = await _docs.AddVersionAsync(_doc.Id, stream, picked.FileName, comment.Trim());
            await LoadAsync();
            await DisplayAlert("Version ajoutée", $"La version {created.Number} est maintenant la version active.", "OK");
        }
        catch (ApiException ex)
        {
            await DocumentActions.HandleAsync(this, ex);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erreur", $"Impossible d'ajouter la version : {ex.Message}", "OK");
        }
        finally
        {
            NewVersionButton.IsEnabled = true;
            NewVersionButton.Text = "Ajouter une nouvelle version";
        }
    }

    private async void OnRestoreClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not DocumentVersion v || _doc == null) return;

        var ok = await DisplayAlert("Restaurer la version",
            $"Restaurer la {v.Title.ToLowerInvariant()} ? Une nouvelle version active sera créée ; l'historique est conservé.",
            "Restaurer", "Annuler");
        if (!ok) return;

        try
        {
            var created = await _docs.RestoreVersionAsync(_doc.Id, v.Id);
            await LoadAsync();
            await DisplayAlert("Version restaurée", $"La version {created.Number} est maintenant la version active.", "OK");
        }
        catch (ApiException ex)
        {
            await DocumentActions.HandleAsync(this, ex);
        }
    }
}
