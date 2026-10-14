using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShortLinks.Tests.Fakes;

namespace ShortLinks.Tests.Integration;

public sealed class ShortLinksApiFactory : WebApplicationFactory<Program>
{
    public const string BaseUrl = "https://sho.rt";

    private readonly bool _deleteDatabaseOnDispose;

    public ShortLinksApiFactory()
        : this(NewDatabasePath(), deleteDatabaseOnDispose: true)
    {
    }

    private ShortLinksApiFactory(string databasePath, bool deleteDatabaseOnDispose)
    {
        DatabasePath = databasePath;
        _deleteDatabaseOnDispose = deleteDatabaseOnDispose;
    }

    public string DatabasePath { get; }

    public FakeClock Clock { get; } = new(new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero));

    // Simula reinícios da aplicação: cada instância é um host novo sobre o mesmo arquivo.
    public static ShortLinksApiFactory UsingExistingDatabase(string databasePath) =>
        new(databasePath, deleteDatabaseOnDispose: false);

    public static string NewDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"shortlinks-tests-{Guid.NewGuid():N}.db");

    public static void DeleteDatabase(string databasePath)
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm", "-journal" })
            File.Delete(databasePath + suffix);
    }

    public HttpClient CreateNonRedirectingClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public async Task<JsonElement> CreateLinkAsync(object request)
    {
        var response = await CreateClient().PostAsJsonAsync("/api/links", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    public async Task<long> GetAccessCountAsync(string code)
    {
        var stats = await CreateClient().GetFromJsonAsync<JsonElement>($"/api/links/{code}/stats");
        return stats.GetProperty("accessCount").GetInt64();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:ShortLinks"] = $"Data Source={DatabasePath}",
                ["ShortLinks:BaseUrl"] = BaseUrl
            }));
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock));
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (_deleteDatabaseOnDispose)
            DeleteDatabase(DatabasePath);
    }
}
