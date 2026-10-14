namespace ShortLinks.Api.Application;

// RN-05: códigos automáticos e personalizados usam somente A-Z maiúsculo e os dígitos de 1 a 9.
public static class LinkCode
{
    public const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";
    public const int GeneratedLength = 7;
    public const int MinCustomLength = 4;
    public const int MaxCustomLength = 20;

    public static bool HasValidFormat(string code) =>
        code.Length is >= MinCustomLength and <= MaxCustomLength && code.All(Alphabet.Contains);
}
