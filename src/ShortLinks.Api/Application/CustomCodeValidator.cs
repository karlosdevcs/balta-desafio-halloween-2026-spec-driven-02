namespace ShortLinks.Api.Application;

// RF-03 e RN-05: código próprio de 4 a 20 caracteres, somente A-Z maiúsculo e 1 a 9.
public static class CustomCodeValidator
{
    public static string? Validate(string code)
    {
        if (code.Length is < LinkCode.MinCustomLength or > LinkCode.MaxCustomLength)
            return $"O código deve ter entre {LinkCode.MinCustomLength} e {LinkCode.MaxCustomLength} caracteres.";

        if (!code.All(LinkCode.Alphabet.Contains))
            return "O código deve conter apenas letras maiúsculas de A a Z e números de 1 a 9, sem espaços.";

        return null;
    }
}
