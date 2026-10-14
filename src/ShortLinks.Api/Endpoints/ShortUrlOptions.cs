namespace ShortLinks.Api.Endpoints;

// D-10: URL base do link curto; vazia usa o esquema e o host da requisição.
public sealed class ShortUrlOptions
{
    public const string SectionName = "ShortLinks";

    public string? BaseUrl { get; set; }

    public bool HasValidBaseUrl() =>
        string.IsNullOrWhiteSpace(BaseUrl)
        || (Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));

    public string ResolveBaseUrl(HttpRequest request) =>
        string.IsNullOrWhiteSpace(BaseUrl)
            ? $"{request.Scheme}://{request.Host}{request.PathBase}"
            : BaseUrl.TrimEnd('/');
}
