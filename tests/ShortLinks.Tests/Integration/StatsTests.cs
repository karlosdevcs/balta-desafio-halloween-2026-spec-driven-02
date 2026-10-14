using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShortLinks.Tests.Integration;

public class StatsTests(ShortLinksApiFactory factory) : IClassFixture<ShortLinksApiFactory>
{
    [Fact]
    public async Task Anyone_with_the_code_can_read_the_access_count()
    {
        await factory.CreateLinkAsync(new { targetUrl = "https://balta.io", code = "ESTATS", expiresAt = "2026-12-31" });
        await factory.CreateNonRedirectingClient().GetAsync("/ESTATS");

        var response = await factory.CreateClient().GetAsync("/api/links/ESTATS/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stats = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ESTATS", stats.GetProperty("code").GetString());
        Assert.Equal(1, stats.GetProperty("accessCount").GetInt64());
        Assert.Equal("2026-12-31", stats.GetProperty("expiresAt").GetString());
        Assert.Equal(factory.Clock.UtcNow.UtcDateTime, stats.GetProperty("createdAtUtc").GetDateTime().ToUniversalTime());
    }

    [Fact]
    public async Task Unknown_code_returns_404_problem_details()
    {
        var response = await factory.CreateClient().GetAsync("/api/links/NAOEXISTE/stats");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
