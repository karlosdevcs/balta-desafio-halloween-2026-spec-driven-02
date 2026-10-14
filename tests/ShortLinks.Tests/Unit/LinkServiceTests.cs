using ShortLinks.Api.Application;
using ShortLinks.Api.Domain;

namespace ShortLinks.Tests.Unit;

public class LinkServiceTests
{
    private const string TargetUrl = "https://balta.io/desafios/halloween-2026";
    private static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryLinkRepository _repository = new();
    private readonly FakeClock _clock = new(Now);

    private LinkService CreateService(params string[] generatedCodes) =>
        new(_repository, new SequenceCodeGenerator(generatedCodes), _clock);

    private void SeedLink(string code, DateOnly? expiresOn = null) =>
        _repository.Seed(new Link(code, TargetUrl, Now.UtcDateTime, expiresOn));

    // Criação

    [Fact]
    public async Task Creates_link_with_generated_code()
    {
        var result = await CreateService("ABC1234").CreateAsync(new CreateLinkCommand(TargetUrl, null, null));

        var created = Assert.IsType<CreateLinkResult.Created>(result);
        Assert.Equal("ABC1234", created.Link.Code);
        Assert.Equal(TargetUrl, created.Link.TargetUrl);
        Assert.Equal(Now.UtcDateTime, created.Link.CreatedAtUtc);
        Assert.Null(created.Link.ExpiresOn);
    }

    [Fact]
    public async Task Generates_another_code_when_generated_code_already_exists()
    {
        SeedLink("TAKEN11");
        var generator = new SequenceCodeGenerator("TAKEN11", "FREE111");
        var service = new LinkService(_repository, generator, _clock);

        var result = await service.CreateAsync(new CreateLinkCommand(TargetUrl, null, null));

        Assert.Equal("FREE111", Assert.IsType<CreateLinkResult.Created>(result).Link.Code);
        Assert.Equal(2, generator.Calls);
    }

    [Fact]
    public async Task Generates_another_code_when_insert_hits_unique_index()
    {
        _repository.CodesTakenOnInsert.Add("RACE111");

        var result = await CreateService("RACE111", "FREE111").CreateAsync(new CreateLinkCommand(TargetUrl, null, null));

        Assert.Equal("FREE111", Assert.IsType<CreateLinkResult.Created>(result).Link.Code);
    }

    [Fact]
    public async Task Gives_up_after_max_generation_attempts()
    {
        var codes = Enumerable.Range(1, LinkService.MaxGenerationAttempts).Select(i => $"TAKEN{i}{i}").ToArray();
        foreach (var code in codes)
            SeedLink(code);

        var result = await CreateService(codes).CreateAsync(new CreateLinkCommand(TargetUrl, null, null));

        Assert.IsType<CreateLinkResult.CodeGenerationFailed>(result);
    }

    [Fact]
    public async Task Creates_link_with_custom_code()
    {
        var result = await CreateService().CreateAsync(new CreateLinkCommand(TargetUrl, "HALLOWEEN", null));

        Assert.Equal("HALLOWEEN", Assert.IsType<CreateLinkResult.Created>(result).Link.Code);
    }

    [Fact]
    public async Task Rejects_custom_code_already_in_use()
    {
        SeedLink("HALLOWEEN");

        var result = await CreateService().CreateAsync(new CreateLinkCommand(TargetUrl, "HALLOWEEN", null));

        Assert.Equal("HALLOWEEN", Assert.IsType<CreateLinkResult.CodeInUse>(result).Code);
    }

    [Fact]
    public async Task Rejects_custom_code_taken_concurrently_by_unique_index()
    {
        _repository.CodesTakenOnInsert.Add("HALLOWEEN");

        var result = await CreateService().CreateAsync(new CreateLinkCommand(TargetUrl, "HALLOWEEN", null));

        Assert.IsType<CreateLinkResult.CodeInUse>(result);
    }

    [Fact]
    public async Task Creates_link_with_expiration_date()
    {
        var result = await CreateService("ABC1234").CreateAsync(new CreateLinkCommand(TargetUrl, null, "2026-10-31"));

        Assert.Equal(new DateOnly(2026, 10, 31), Assert.IsType<CreateLinkResult.Created>(result).Link.ExpiresOn);
    }

    [Fact]
    public async Task Reports_every_invalid_field_at_once()
    {
        var result = await CreateService().CreateAsync(new CreateLinkCommand("ftp://balta.io", "abc", "2026-10-13"));

        var invalid = Assert.IsType<CreateLinkResult.Invalid>(result);
        Assert.Equal(["code", "expiresAt", "targetUrl"], invalid.Errors.Keys.Order());
    }

    // Acesso

    [Fact]
    public async Task Redirects_and_counts_access_to_existing_link()
    {
        SeedLink("ABC1234");

        var result = await CreateService().AccessAsync("ABC1234");

        Assert.Equal(TargetUrl, Assert.IsType<AccessLinkResult.Redirect>(result).TargetUrl);
        Assert.Equal(1, _repository.AccessCounts["ABC1234"]);
    }

    [Fact]
    public async Task Counts_repeated_accesses()
    {
        SeedLink("ABC1234");
        var service = CreateService();

        for (var i = 0; i < 3; i++)
            await service.AccessAsync("ABC1234");

        Assert.Equal(3, _repository.AccessCounts["ABC1234"]);
    }

    [Theory]
    [InlineData("NOPE111")]
    [InlineData("abc1234")]
    [InlineData("favicon.ico")]
    public async Task Returns_not_found_for_unknown_or_malformed_code(string code)
    {
        SeedLink("ABC1234");

        var result = await CreateService().AccessAsync(code);

        Assert.IsType<AccessLinkResult.NotFound>(result);
        Assert.Equal(0, _repository.RegisterAccessCalls);
    }

    [Fact]
    public async Task Link_still_redirects_on_last_instant_of_expiration_date()
    {
        SeedLink("ABC1234", expiresOn: new DateOnly(2026, 10, 14));
        _clock.UtcNow = new DateTimeOffset(2026, 10, 14, 23, 59, 59, 999, TimeSpan.Zero);

        var result = await CreateService().AccessAsync("ABC1234");

        Assert.IsType<AccessLinkResult.Redirect>(result);
    }

    [Fact]
    public async Task Expired_link_does_not_redirect_nor_count_access()
    {
        SeedLink("ABC1234", expiresOn: new DateOnly(2026, 10, 13));

        var result = await CreateService().AccessAsync("ABC1234");

        Assert.Equal(new DateOnly(2026, 10, 13), Assert.IsType<AccessLinkResult.Expired>(result).ExpiresOn);
        Assert.Equal(0, _repository.RegisterAccessCalls);
        Assert.False(_repository.AccessCounts.ContainsKey("ABC1234"));
    }

    [Fact]
    public async Task Link_expiring_between_read_and_update_is_reported_as_expired()
    {
        SeedLink("ABC1234", expiresOn: new DateOnly(2026, 10, 14));
        _repository.RejectNextAccess = true;

        var result = await CreateService().AccessAsync("ABC1234");

        Assert.IsType<AccessLinkResult.Expired>(result);
    }

    // Estatísticas

    [Fact]
    public async Task Returns_stats_for_existing_link_even_when_expired()
    {
        SeedLink("ABC1234", expiresOn: new DateOnly(2026, 1, 1));

        Assert.NotNull(await CreateService().GetStatsAsync("ABC1234"));
    }

    [Fact]
    public async Task Returns_no_stats_for_unknown_code()
    {
        Assert.Null(await CreateService().GetStatsAsync("NOPE111"));
    }
}
