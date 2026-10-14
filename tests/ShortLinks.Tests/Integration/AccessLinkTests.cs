using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShortLinks.Tests.Integration;

public class AccessLinkTests(ShortLinksApiFactory factory) : IClassFixture<ShortLinksApiFactory>
{
    private const string TargetUrl = "https://balta.io/desafios/halloween-2026";

    [Fact]
    public async Task Redirects_to_target_with_302()
    {
        var code = (await factory.CreateLinkAsync(new { targetUrl = TargetUrl })).GetProperty("code").GetString();

        var response = await factory.CreateNonRedirectingClient().GetAsync($"/{code}");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(TargetUrl, response.Headers.Location?.ToString());
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Theory]
    [InlineData("NAOEXISTE")]
    [InlineData("naoexiste")]
    public async Task Unknown_code_returns_404_problem_details(string code)
    {
        var response = await factory.CreateNonRedirectingClient().GetAsync($"/{code}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal($"O link '{code}' não existe.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Every_access_increments_counter()
    {
        var code = (await factory.CreateLinkAsync(new { targetUrl = TargetUrl })).GetProperty("code").GetString()!;
        var client = factory.CreateNonRedirectingClient();

        for (var i = 0; i < 3; i++)
            await client.GetAsync($"/{code}");

        Assert.Equal(3, await factory.GetAccessCountAsync(code));
    }

    [Fact]
    public async Task Concurrent_accesses_do_not_lose_counts()
    {
        const int accesses = 100;
        var code = (await factory.CreateLinkAsync(new { targetUrl = TargetUrl })).GetProperty("code").GetString()!;
        var client = factory.CreateNonRedirectingClient();

        var responses = await Task.WhenAll(Enumerable.Range(0, accesses).Select(_ => client.GetAsync($"/{code}")));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Found, r.StatusCode));
        Assert.Equal(accesses, await factory.GetAccessCountAsync(code));
    }
}
