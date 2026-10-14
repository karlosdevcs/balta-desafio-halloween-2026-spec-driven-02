using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace ShortLinks.Api.Domain;

public class Link
{
    public const int MaxCodeLength = 20;
    public const int MaxTargetUrlLength = 2048;

    private Link()
    {
    }

    public Link(string code, string targetUrl, DateTime createdAtUtc, DateOnly? expiresOn)
    {
        Code = code;
        TargetUrl = targetUrl;
        CreatedAtUtc = createdAtUtc;
        ExpiresOn = expiresOn;
    }

    public int Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string TargetUrl { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateOnly? ExpiresOn { get; private set; }
    public long AccessCount { get; private set; }

    // RF-06: o link vale até o fim da data de expiração em UTC.
    [MemberNotNullWhen(true, nameof(ExpiresOn))]
    public bool IsExpiredOn(DateOnly todayUtc) => ExpiresOn is { } expiresOn && todayUtc > expiresOn;

    // A mesma regra de IsExpiredOn, negada e traduzível para SQL. As duas precisam permanecer equivalentes.
    public static Expression<Func<Link, bool>> IsActiveOn(DateOnly todayUtc) =>
        link => link.ExpiresOn == null || link.ExpiresOn >= todayUtc;
}
