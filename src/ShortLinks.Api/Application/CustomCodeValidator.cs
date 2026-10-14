namespace ShortLinks.Api.Application;

// RF-03 e RN-05: código próprio de 4 a 20 caracteres, somente A-Z maiúsculo e 1 a 9.
public static class CustomCodeValidator
{
    public static ValidationResult<string> Validate(string code)
    {
        if (!LinkCode.HasValidLength(code))
            return ValidationResult<string>.Failure(
                $"O código deve ter entre {LinkCode.MinLength} e {LinkCode.MaxLength} caracteres.");

        if (!LinkCode.UsesOnlyAlphabet(code))
            return ValidationResult<string>.Failure(
                "O código deve conter apenas letras maiúsculas de A a Z e números de 1 a 9, sem espaços.");

        return ValidationResult<string>.Success(code);
    }
}
