using System.Net;

namespace ShortLinks.Tests.Integration;

public class PersistenceTests
{
    [Fact]
    public async Task Links_survive_application_restart()
    {
        var databasePath = ShortLinksApiFactory.NewDatabasePath();
        try
        {
            await using (var beforeRestart = ShortLinksApiFactory.UsingExistingDatabase(databasePath))
            {
                await beforeRestart.CreateLinkAsync(new { targetUrl = "https://balta.io", code = "PERSISTE" });
                await beforeRestart.CreateNonRedirectingClient().GetAsync("/PERSISTE");
            }

            await using var afterRestart = ShortLinksApiFactory.UsingExistingDatabase(databasePath);
            var response = await afterRestart.CreateNonRedirectingClient().GetAsync("/PERSISTE");

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal("https://balta.io/", response.Headers.Location?.ToString());
            Assert.Equal(2, await afterRestart.GetAccessCountAsync("PERSISTE"));
        }
        finally
        {
            ShortLinksApiFactory.DeleteDatabase(databasePath);
        }
    }
}
