using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ShortLinks.Api.Application;

namespace ShortLinks.Tests.Integration;

public class CreateLinkTests(ShortLinksApiFactory factory) : IClassFixture<ShortLinksApiFactory>
{
    private const string TargetUrl = "https://balta.io/desafios/halloween-2026";

    [Fact]
    public async Task Creates_link_with_generated_code()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/links", new { targetUrl = TargetUrl });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var code = body.GetProperty("code").GetString()!;
        Assert.Equal(7, code.Length);
        Assert.All(code, c => Assert.Contains(c, LinkCode.Alphabet));
        Assert.Equal($"{ShortLinksApiFactory.BaseUrl}/{code}", body.GetProperty("shortUrl").GetString());
        Assert.Equal(TargetUrl, body.GetProperty("targetUrl").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("expiresAt").ValueKind);
        Assert.Equal($"{ShortLinksApiFactory.BaseUrl}/{code}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Creates_link_with_custom_code()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/links", new { targetUrl = TargetUrl, code = "BALTA" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("BALTA", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Rejects_duplicate_custom_code_with_409()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/links", new { targetUrl = TargetUrl, code = "DUPLICADO" });

        var response = await client.PostAsJsonAsync("/api/links", new { targetUrl = "https://outro.com", code = "DUPLICADO" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal("O código 'DUPLICADO' já está em uso. Escolha outro código.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Creates_link_with_expiration_date()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/links", new { targetUrl = TargetUrl, expiresAt = "2026-10-31" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2026-10-31", body.GetProperty("expiresAt").GetString());
    }

    [Fact]
    public async Task Rejects_past_expiration_date_with_400()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/links", new { targetUrl = TargetUrl, expiresAt = "2026-10-13" });

        await AssertValidationErrorAsync(response, "expiresAt", "A data de expiração não pode estar no passado.");
    }

    [Fact]
    public async Task Rejects_invalid_target_url_with_400()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/links", new { targetUrl = "http://localhost/admin" });

        await AssertValidationErrorAsync(
            response, "targetUrl", "A URL de destino deve apontar para um endereço publicamente acessível.");
    }

    [Fact]
    public async Task Rejects_target_url_longer_than_2048_characters_with_400()
    {
        var url = "https://balta.io/" + new string('a', 2048);

        var response = await factory.CreateClient().PostAsJsonAsync("/api/links", new { targetUrl = url });

        await AssertValidationErrorAsync(response, "targetUrl", "A URL de destino deve ter no máximo 2048 caracteres.");
    }

    [Fact]
    public async Task Rejects_invalid_custom_code_with_400()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/links", new { targetUrl = TargetUrl, code = "meu-link" });

        await AssertValidationErrorAsync(
            response, "code", "O código deve conter apenas letras maiúsculas de A a Z e números de 1 a 9, sem espaços.");
    }

    [Fact]
    public async Task Rejects_malformed_body_with_400_problem_details()
    {
        var content = new StringContent("{ isso não é json", Encoding.UTF8, "application/json");

        var response = await factory.CreateClient().PostAsync("/api/links", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static async Task AssertValidationErrorAsync(HttpResponseMessage response, string field, string message)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal(message, problem.GetProperty("errors").GetProperty(field)[0].GetString());
    }

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
