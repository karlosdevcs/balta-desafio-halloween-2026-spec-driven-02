using ShortLinks.Api.Domain;

namespace ShortLinks.Api.Application;

public sealed record CreateLinkCommand(string? TargetUrl, string? Code, string? ExpiresAt);

public abstract record CreateLinkResult
{
    public sealed record Created(Link Link) : CreateLinkResult;
    public sealed record Invalid(Dictionary<string, string[]> Errors) : CreateLinkResult;
    public sealed record CodeInUse(string Code) : CreateLinkResult;
    public sealed record CodeGenerationFailed : CreateLinkResult;
}

public abstract record AccessLinkResult
{
    public sealed record Redirect(string TargetUrl) : AccessLinkResult;
    public sealed record NotFound : AccessLinkResult;
    public sealed record Expired(DateOnly ExpiresOn) : AccessLinkResult;
}
