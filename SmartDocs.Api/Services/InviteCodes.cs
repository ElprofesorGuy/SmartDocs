using System.Security.Cryptography;

namespace SmartDocs.Api.Services;

/// <summary>
/// Codes d'invitation : 10 caractères tirés au hasard (générateur cryptographique) dans un alphabet sans
/// caractères ambigus (ni 0/O, ni 1/I), soit 32^10 combinaisons. Affichés « XXXXX-XXXXX », stockés sans tiret.
/// </summary>
public static class InviteCodes
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    public const int Length = 10;

    public static string Generate()
    {
        var chars = new char[Length];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        return new string(chars);
    }

    /// <summary>Saisie tolérante : majuscules, tirets et espaces ignorés.</summary>
    public static string Normalize(string? input)
        => new((input ?? "").Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).Take(40).ToArray());

    public static string Format(string code)
        => code.Length == Length ? $"{code[..5]}-{code[5..]}" : code;
}
