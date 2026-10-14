using System.Buffers;

namespace ShortLinks.Api.Application;

// RN-05: códigos automáticos e personalizados usam somente A-Z maiúsculo e os dígitos de 1 a 9.
public static class LinkCode
{
    public const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";
    public const int GeneratedLength = 7;
    public const int MinLength = 4;
    public const int MaxLength = 20;

    private static readonly SearchValues<char> AlphabetValues = SearchValues.Create(Alphabet);

    public static bool HasValidLength(string code) => code.Length is >= MinLength and <= MaxLength;

    public static bool UsesOnlyAlphabet(string code) => !code.AsSpan().ContainsAnyExcept(AlphabetValues);

    public static bool HasValidFormat(string code) => HasValidLength(code) && UsesOnlyAlphabet(code);
}
