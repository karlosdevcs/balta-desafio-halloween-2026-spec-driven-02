using ShortLinks.Api.Application;

namespace ShortLinks.Tests.Unit;

public class ExpirationValidatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 14);

    [Fact]
    public void Accepts_today()
    {
        Assert.Null(ExpirationValidator.Validate("2026-10-14", Today, out var expiresOn));
        Assert.Equal(Today, expiresOn);
    }

    [Fact]
    public void Accepts_future_date()
    {
        Assert.Null(ExpirationValidator.Validate("2026-10-31", Today, out var expiresOn));
        Assert.Equal(new DateOnly(2026, 10, 31), expiresOn);
    }

    [Fact]
    public void Rejects_past_date()
    {
        Assert.Equal(
            "A data de expiração não pode estar no passado.",
            ExpirationValidator.Validate("2026-10-13", Today, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("14/10/2026")]
    [InlineData("2026-10-14T10:00:00Z")]
    [InlineData("2026-02-30")]
    public void Rejects_invalid_format(string expiresAt)
    {
        Assert.Equal(
            "A data de expiração deve estar no formato AAAA-MM-DD.",
            ExpirationValidator.Validate(expiresAt, Today, out _));
    }
}
