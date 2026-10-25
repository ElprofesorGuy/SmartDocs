using System.Text.RegularExpressions;

namespace SmartDocs.Helpers;

public static class Validation
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);

    public static bool IsEmail(string? value) => !string.IsNullOrWhiteSpace(value) && EmailRegex.IsMatch(value.Trim());
    public static bool HasMinLength(string? value) => (value?.Length ?? 0) >= 8;
    public static bool HasUpper(string? value) => value?.Any(char.IsUpper) ?? false;
    /// <summary>Code d'invitation plausible (lettres et chiffres, tirets et espaces ignorés). Le serveur fait la vraie vérification.</summary>
    public static bool IsInviteCode(string? value) => (value?.Count(char.IsLetterOrDigit) ?? 0) >= 6;
    public static bool HasDigit(string? value) => value?.Any(char.IsDigit) ?? false;
}
