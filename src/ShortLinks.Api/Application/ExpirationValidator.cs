using System.Globalization;

namespace ShortLinks.Api.Application;

// RF-06 e D-07: data AAAA-MM-DD em UTC; datas anteriores a hoje são recusadas.
public static class ExpirationValidator
{
    public static ValidationResult<DateOnly> Validate(string expiresAt, DateOnly todayUtc)
    {
        if (!DateOnly.TryParseExact(expiresAt, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiresOn))
            return ValidationResult<DateOnly>.Failure("A data de expiração deve estar no formato AAAA-MM-DD.");

        if (expiresOn < todayUtc)
            return ValidationResult<DateOnly>.Failure("A data de expiração não pode estar no passado.");

        return ValidationResult<DateOnly>.Success(expiresOn);
    }
}
