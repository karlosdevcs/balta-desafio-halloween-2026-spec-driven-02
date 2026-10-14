using ShortLinks.Api.Application;
using ShortLinks.Api.Domain;

namespace ShortLinks.Tests;

public sealed class FakeClock(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;

    public override DateTimeOffset GetUtcNow() => UtcNow;
}

public sealed class SequenceCodeGenerator(params string[] codes) : ICodeGenerator
{
    private readonly Queue<string> _codes = new(codes);

    public int Calls { get; private set; }

    public string Generate()
    {
        Calls++;
        return _codes.Dequeue();
    }
}

public sealed class InMemoryLinkRepository : ILinkRepository
{
    private readonly Dictionary<string, Link> _links = new();

    public Dictionary<string, long> AccessCounts { get; } = new();

    // Simula outro processo gravando o mesmo código entre a checagem e o INSERT.
    public HashSet<string> CodesTakenOnInsert { get; } = new();

    // Simula o dia virando entre a leitura do link e o UPDATE do contador.
    public bool RejectNextAccess { get; set; }

    public int RegisterAccessCalls { get; private set; }

    public void Seed(Link link) => _links[link.Code] = link;

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(_links.ContainsKey(code));

    public Task<bool> TryAddAsync(Link link, CancellationToken cancellationToken)
    {
        if (_links.ContainsKey(link.Code) || CodesTakenOnInsert.Remove(link.Code))
            return Task.FromResult(false);

        _links[link.Code] = link;
        return Task.FromResult(true);
    }

    public Task<Link?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(_links.GetValueOrDefault(code));

    public Task<bool> TryRegisterAccessAsync(string code, DateOnly todayUtc, CancellationToken cancellationToken)
    {
        RegisterAccessCalls++;
        if (RejectNextAccess || !_links.TryGetValue(code, out var link) || link.IsExpiredOn(todayUtc))
        {
            RejectNextAccess = false;
            return Task.FromResult(false);
        }

        AccessCounts[code] = AccessCounts.GetValueOrDefault(code) + 1;
        return Task.FromResult(true);
    }
}
