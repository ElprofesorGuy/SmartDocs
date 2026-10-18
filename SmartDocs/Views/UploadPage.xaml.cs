using System.Collections.ObjectModel;
using SmartDocs.Helpers;
using SmartDocs.Services;

namespace SmartDocs.Views;

public partial class UploadPage : ContentPage
{
    private const long MaxBytes = 10L * 1024 * 1024;
    private static readonly string[] AllowedExtensions = { "pdf", "docx", "xlsx" };

    private readonly DocumentService _docs;
    private readonly AuthService _auth;
    private readonly ObservableCollection<string> _tags = new();
    private int _confIndex = 1; // « Interne » par défaut
    private string? _pickedExt;
    private double _pickedKb;
    private int _progressToken;

    public UploadPage(DocumentService docs, AuthService auth)
    {
        InitializeComponent();
        _docs = docs;
        _auth = auth;
        CategoryPicker.ItemsSource = DocumentService.Categories;
        BindableLayout.SetItemsSource(TagsLayout, _tags);
        FieldFocus.Hook(NameEntry, NameBorder);
        UpdateConfidentiality();
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/home");

    private async void OnHelpTapped(object? sender, TappedEventArgs e)
        => await DisplayAlert("Aide au dépôt",
            "Formats acceptés : PDF, DOCX, XLSX (10 Mo maximum).\n\nDans cette démonstration, le fichier est lu pour obtenir son nom et sa taille, mais son contenu n'est pas envoyé : aucun serveur n'existe.",
            "OK");

    private async void OnBrowseClicked(object? sender, EventArgs e)
    {
        try
        {
            var types = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[]
                    {
                        "application/pdf",
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    } },
                { DevicePlatform.WinUI, new[] { ".pdf", ".docx", ".xlsx" } }
            });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Choisir un document",
                FileTypes = types
            });
            if (result == null) return; // annulé

            var ext = Path.GetExtension(result.FileName).TrimStart('.').ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                ShowError("Format non accepté. Choisissez un fichier PDF, DOCX ou XLSX.");
                return;
            }

            long bytes = 0;
            try
            {
                await using var stream = await result.OpenReadAsync();
                bytes = stream.CanSeek ? stream.Length : 0;
            }
            catch { /* taille inconnue */ }

            if (bytes > MaxBytes)
            {
                ShowError("Le fichier dépasse la taille maximale de 10 Mo.");
                return;
            }

            ErrorLabel.IsVisible = false;
            _pickedExt = ext;
            _pickedKb = bytes / 1024.0;

            var tmp = new Models.DocumentItem { Extension = ext, SizeKb = _pickedKb };
            PickedTile.Glyph = tmp.FileGlyph;
            PickedTile.Fg = tmp.FileColor;
            PickedTile.Bg = tmp.FileBg;
            PickedTile.Ext = tmp.ExtensionUpper;
            PickedTile.ShowExt = true;
            PickedName.Text = result.FileName;
            PickedSize.Text = bytes > 0 ? tmp.SizeText : "Taille inconnue";

            if (string.IsNullOrWhiteSpace(NameEntry.Text))
                NameEntry.Text = Path.GetFileNameWithoutExtension(result.FileName).Replace('_', ' ');

            SelectedSection.IsVisible = true;
            await AnimateProgressAsync();
        }
        catch (Exception ex)
        {
            ShowError($"Impossible d'ouvrir le sélecteur de fichiers : {ex.Message}");
        }
    }

    /// <summary>Animation de progression : le fichier est simplement « chargé » en local (rien n'est envoyé).</summary>
    private async Task AnimateProgressAsync()
    {
        var token = ++_progressToken;
        for (var i = 0; i <= 10; i++)
        {
            if (token != _progressToken) return;
            UploadProgress.Progress = i / 10.0;
            PickedStatus.Text = $"{i * 10} % chargé";
            await Task.Delay(60);
        }
    }

    private void OnClearFile(object? sender, TappedEventArgs e)
    {
        _progressToken++;
        _pickedExt = null;
        _pickedKb = 0;
        UploadProgress.Progress = 0;
        SelectedSection.IsVisible = false;
    }

    private void OnConfidentialityTapped(object? sender, TappedEventArgs e)
    {
        _confIndex = (_confIndex + 1) % DocumentService.ConfidentialityLevels.Length;
        UpdateConfidentiality();
    }

    private void UpdateConfidentiality()
    {
        var level = DocumentService.ConfidentialityLevels[_confIndex];
        ConfLabel.Text = level;
        ConfIcon.Text = level == "Public" ? Icons.Visibility : Icons.Lock;
        var danger = level == "Confidentiel";
        ConfBadge.BackgroundColor = danger ? Palette.DangerBg : Palette.AccentLight;
        ConfBadge.Stroke = danger ? Color.FromArgb("#FECACA") : Palette.AccentBorder;
        ConfLabel.TextColor = danger ? Palette.Danger : Palette.Primary;
        ConfIcon.TextColor = danger ? Palette.Danger : Palette.Primary;
    }

    private void OnTagCompleted(object? sender, EventArgs e)
    {
        var raw = TagEntry.Text?.Trim().TrimStart('#').Replace(' ', '_') ?? "";
        TagEntry.Text = string.Empty;
        if (raw.Length == 0) return;
        var tag = "#" + raw;
        if (!_tags.Contains(tag, StringComparer.OrdinalIgnoreCase)) _tags.Add(tag);
    }

    private void OnTagTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is string tag) _tags.Remove(tag);
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private async void OnPublishClicked(object? sender, EventArgs e)
    {
        if (_pickedExt == null) { ShowError("Sélectionnez d'abord un fichier (PDF, DOCX ou XLSX)."); return; }
        if (string.IsNullOrWhiteSpace(NameEntry.Text)) { ShowError("Renseignez le nom du document."); return; }
        if (CategoryPicker.SelectedIndex < 0) { ShowError("Choisissez une catégorie."); return; }

        ErrorLabel.IsVisible = false;
        var category = DocumentService.Categories[CategoryPicker.SelectedIndex];
        var doc = _docs.AddDocument(NameEntry.Text, category, _pickedExt, _pickedKb,
            DocumentService.ConfidentialityLevels[_confIndex], _tags, _auth.CurrentUser.FullName);

        await DisplayAlert("Document publié (simulation)",
            $"« {doc.Name} » a été ajouté à la liste locale. Le fichier lui-même n'est ni envoyé ni stocké : il faudrait un backend.",
            "OK");
        ResetForm();
        await Shell.Current.GoToAsync("//main/documents");
    }

    private void ResetForm()
    {
        OnClearFile(null, null!);
        NameEntry.Text = string.Empty;
        CategoryPicker.SelectedIndex = -1;
        _tags.Clear();
        _confIndex = 1;
        UpdateConfidentiality();
    }
}
