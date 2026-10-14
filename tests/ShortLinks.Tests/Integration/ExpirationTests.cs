using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShortLinks.Tests.Integration;

public class ExpirationTests(ShortLinksApiFactory factory) : IClassFixture<ShortLinksApiFactory>
{
    [Fact]
    public async Task Link_works_until_end_of_expiration_date_in_utc_and_then_stops_counting()
    {
        factory.Clock.UtcNow = new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);
        var code = (await factory.CreateLinkAsync(new { targetUrl = "https://balta.io", expiresAt = "2026-10-14" }))
            .GetProperty("code").GetString()!;
        var client = factory.CreateNonRedirectingClient();

        Assert.Equal(HttpStatusCode.Found, (await client.GetAsync($"/{code}")).StatusCode);

        factory.Clock.UtcNow = new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero).AddDays(1).AddTicks(-1);
        Assert.Equal(HttpStatusCode.Found, (await client.GetAsync($"/{code}")).StatusCode);

        factory.Clock.UtcNow = new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero);
        var expired = await client.GetAsync($"/{code}");

        Assert.Equal(HttpStatusCode.Gone, expired.StatusCode);
        var problem = await expired.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal($"O link '{code}' expirou em 2026-10-14 (UTC).", problem.GetProperty("detail").GetString());
        Assert.Equal(2, await factory.GetAccessCountAsync(code));
    }
}
