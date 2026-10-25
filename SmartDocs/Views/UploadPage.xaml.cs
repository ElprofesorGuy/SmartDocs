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
    private FileResult? _picked;
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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var user = _auth.CurrentUser;
        if (!user.CanDeposit)
        {
            await DisplayAlert("Accès restreint",
                $"Votre rôle ({user.RoleLabel}) ne permet pas de déposer des documents. Seuls les responsables de service et les collaborateurs le peuvent.",
                "OK");
            await Shell.Current.GoToAsync("//main/home");
            return;
        }
        // On ne dépose que dans son propre service
        CategoryPicker.ItemsSource = new[] { user.Department };
        CategoryPicker.SelectedIndex = 0;
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("//main/home");

    private async void OnHelpTapped(object? sender, TappedEventArgs e)
        => await DisplayAlert("Aide au dépôt",
            "Formats acceptés : PDF, DOCX, XLSX (10 Mo maximum).\n\nLe fichier est envoyé à l'API SmartDocs lorsque vous touchez « Publier le document ».",
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
            _picked = result;
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

    /// <summary>Animation de chargement local du fichier sélectionné (l'envoi réel a lieu à la publication).</summary>
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
        PickedStatus.Text = "Prêt à publier";
    }

    private void OnClearFile(object? sender, TappedEventArgs e)
    {
        _progressToken++;
        _picked = null;
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
        if (_picked == null || _pickedExt == null) { ShowError("Sélectionnez d'abord un fichier (PDF, DOCX ou XLSX)."); return; }
        if (string.IsNullOrWhiteSpace(NameEntry.Text)) { ShowError("Renseignez le nom du document."); return; }
        if (CategoryPicker.SelectedIndex < 0) { ShowError("Choisissez une catégorie."); return; }

        ErrorLabel.IsVisible = false;
        PublishButton.IsEnabled = false;
        PublishButton.Text = "Envoi en cours…";
        try
        {
            var category = (string)CategoryPicker.SelectedItem;
            await using var stream = await _picked.OpenReadAsync();
            var doc = await _docs.AddDocumentAsync(stream, _picked.FileName, NameEntry.Text, category,
                DocumentService.ConfidentialityLevels[_confIndex], _tags);

            await DisplayAlert("Document publié", $"« {doc.Name} » a été envoyé au serveur.", "OK");
            ResetForm();
            await Shell.Current.GoToAsync("//main/documents");
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            PublishButton.IsEnabled = true;
            PublishButton.Text = "Publier le document";
        }
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
