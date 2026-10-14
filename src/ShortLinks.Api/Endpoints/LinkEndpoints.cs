using System.Diagnostics;
using ShortLinks.Api.Application;

namespace ShortLinks.Api.Endpoints;

public static class LinkEndpoints
{
    public sealed record CreateLinkRequest(string? TargetUrl, string? Code, string? ExpiresAt);

    public sealed record LinkResponse(string ShortUrl, string Code, string TargetUrl, DateOnly? ExpiresAt);

    public sealed record LinkStatsResponse(string Code, long AccessCount, DateTime CreatedAtUtc, DateOnly? ExpiresAt);

    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/links", CreateAsync);
        app.MapGet("/api/links/{code}/stats", GetStatsAsync);
        app.MapGet("/{code}", RedirectAsync);
        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateLinkRequest request, LinkService service, HttpRequest httpRequest, IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            new CreateLinkCommand(request.TargetUrl, request.Code, request.ExpiresAt), cancellationToken);

        switch (result)
        {
            case CreateLinkResult.Created { Link: var link }:
                var shortUrl = $"{BaseUrl(httpRequest, configuration)}/{link.Code}";
                return Results.Created(shortUrl, new LinkResponse(shortUrl, link.Code, link.TargetUrl, link.ExpiresOn));
            case CreateLinkResult.Invalid invalid:
                return Results.ValidationProblem(
                    invalid.Errors, title: "Dados inválidos", detail: "Um ou mais campos são inválidos.");
            case CreateLinkResult.CodeInUse inUse:
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Código em uso",
                    detail: $"O código '{inUse.Code}' já está em uso. Escolha outro código.");
            case CreateLinkResult.CodeGenerationFailed:
                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Falha ao gerar código",
                    detail: "Não foi possível gerar um código único. Tente novamente.");
            default:
                throw new UnreachableException();
        }
    }

    private static async Task<IResult> RedirectAsync(
        string code, LinkService service, HttpResponse httpResponse, CancellationToken cancellationToken)
    {
        var result = await service.AccessAsync(code, cancellationToken);

        switch (result)
        {
            case AccessLinkResult.Redirect redirect:
                // RF-05: todo acesso precisa chegar ao servidor para ser contado.
                httpResponse.Headers.CacheControl = "no-store";
                return Results.Redirect(redirect.TargetUrl);
            case AccessLinkResult.NotFound:
                return LinkNotFound(code);
            case AccessLinkResult.Expired expired:
                return Results.Problem(
                    statusCode: StatusCodes.Status410Gone,
                    title: "Link expirado",
                    detail: $"O link '{code}' expirou em {expired.ExpiresOn:yyyy-MM-dd} (UTC).");
            default:
                throw new UnreachableException();
        }
    }

    private static async Task<IResult> GetStatsAsync(string code, LinkService service, CancellationToken cancellationToken)
    {
        var link = await service.GetStatsAsync(code, cancellationToken);

        return link is null
            ? LinkNotFound(code)
            : Results.Ok(new LinkStatsResponse(link.Code, link.AccessCount, link.CreatedAtUtc, link.ExpiresOn));
    }

    private static IResult LinkNotFound(string code) => Results.Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Link não encontrado",
        detail: $"O link '{code}' não existe.");

    // D-10: URL base configurável, com fallback para o host da requisição.
    private static string BaseUrl(HttpRequest request, IConfiguration configuration)
    {
        var configured = configuration["ShortLinks:BaseUrl"];
        return string.IsNullOrWhiteSpace(configured)
            ? $"{request.Scheme}://{request.Host}{request.PathBase}"
            : configured.TrimEnd('/');
    }
}
