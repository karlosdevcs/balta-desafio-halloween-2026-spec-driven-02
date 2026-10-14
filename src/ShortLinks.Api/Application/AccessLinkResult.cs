namespace ShortLinks.Api.Application;

public abstract record AccessLinkResult
{
    private AccessLinkResult()
    {
    }

    public sealed record Redirect(string TargetUrl) : AccessLinkResult;

    public sealed record NotFound : AccessLinkResult;

    public sealed record Expired(DateOnly ExpiresOn) : AccessLinkResult;
}
