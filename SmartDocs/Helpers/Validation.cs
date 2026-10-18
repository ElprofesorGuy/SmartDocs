using System.Text.RegularExpressions;

namespace SmartDocs.Helpers;

public static class Validation
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);

    public static bool IsEmail(string? value) => !string.IsNullOrWhiteSpace(value) && EmailRegex.IsMatch(value.Trim());
    public static bool HasMinLength(string? value) => (value?.Length ?? 0) >= 8;
    public static bool HasUpper(string? value) => value?.Any(char.IsUpper) ?? false;
    public static bool HasDigit(string? value) => value?.Any(char.IsDigit) ?? false;
}
