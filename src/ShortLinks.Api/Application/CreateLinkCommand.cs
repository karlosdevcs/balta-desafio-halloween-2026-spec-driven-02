namespace ShortLinks.Api.Application;

public sealed record CreateLinkCommand(string? TargetUrl, string? Code, string? ExpiresAt);
