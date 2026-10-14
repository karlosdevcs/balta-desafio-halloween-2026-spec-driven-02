using ShortLinks.Api.Domain;

namespace ShortLinks.Api.Application;

public abstract record CreateLinkResult
{
    private CreateLinkResult()
    {
    }

    public sealed record Created(Link Link) : CreateLinkResult;

    public sealed record Invalid(IDictionary<string, string[]> Errors) : CreateLinkResult;

    public sealed record CodeInUse(string Code) : CreateLinkResult;

    public sealed record CodeGenerationFailed : CreateLinkResult;
}
