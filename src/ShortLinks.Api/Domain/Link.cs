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

    public bool IsExpiredOn(DateOnly todayUtc) => ExpiresOn is { } expiresOn && todayUtc > expiresOn;
}
