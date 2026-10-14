using ShortLinks.Api.Domain;

namespace ShortLinks.Tests.Unit;

public class LinkTests
{
    private static readonly DateOnly ExpiresOn = new(2026, 10, 31);

    [Fact]
    public void Link_without_expiration_never_expires()
    {
        var link = new Link("ABCD", "https://balta.io", DateTime.UtcNow, expiresOn: null);

        Assert.False(link.IsExpiredOn(DateOnly.MaxValue));
    }

    [Fact]
    public void Link_is_valid_on_its_expiration_date()
    {
        var link = new Link("ABCD", "https://balta.io", DateTime.UtcNow, ExpiresOn);

        Assert.False(link.IsExpiredOn(ExpiresOn));
    }

    [Fact]
    public void Link_expires_on_the_day_after_its_expiration_date()
    {
        var link = new Link("ABCD", "https://balta.io", DateTime.UtcNow, ExpiresOn);

        Assert.True(link.IsExpiredOn(ExpiresOn.AddDays(1)));
    }
}
