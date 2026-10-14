using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShortLinks.Api.Data;

namespace ShortLinks.Tests.Integration;

public class ApiStartupTests(ShortLinksApiFactory factory) : IClassFixture<ShortLinksApiFactory>
{
    [Fact]
    public async Task Unknown_route_returns_problem_details_404()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/inexistente");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Migrations_are_applied_on_startup()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShortLinksDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Contains(await db.Database.GetAppliedMigrationsAsync(), m => m.EndsWith("_InitialCreate"));
    }
}
