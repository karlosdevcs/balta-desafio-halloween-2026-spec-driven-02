using ShortLinks.Api.Domain;

namespace ShortLinks.Api.Application;

public partial class LinkService(
    ILinkRepository repository,
    ICodeGenerator codeGenerator,
    TimeProvider timeProvider,
    ILogger<LinkService> logger)
{
    // D-05: limite de tentativas para a RN-06.
    public const int MaxGenerationAttempts = 5;

    public async Task<CreateLinkResult> CreateAsync(CreateLinkCommand command, CancellationToken cancellationToken = default)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var targetUrl = TargetUrlValidator.Validate(command.TargetUrl);
        var customCode = command.Code is null ? null : CustomCodeValidator.Validate(command.Code);
        var expiresOn = command.ExpiresAt is null
            ? null
            : ExpirationValidator.Validate(command.ExpiresAt, DateOnly.FromDateTime(nowUtc));

        var errors = new Dictionary<string, string[]>();
        AddErrorIfInvalid(errors, "targetUrl", targetUrl);
        AddErrorIfInvalid(errors, "code", customCode);
        AddErrorIfInvalid(errors, "expiresAt", expiresOn);

        if (errors.Count > 0)
            return new CreateLinkResult.Invalid(errors);

        Link NewLink(string code) => new(code, targetUrl.Value, nowUtc, expiresOn?.Value);

        var result = customCode is null
            ? await CreateWithGeneratedCodeAsync(NewLink, cancellationToken)
            : await CreateWithCustomCodeAsync(customCode.Value, NewLink, cancellationToken);

        if (result is CreateLinkResult.Created { Link: var link })
            Log.LinkCreated(logger, link.Code);

        return result;
    }

    public async Task<AccessLinkResult> AccessAsync(string code, CancellationToken cancellationToken = default)
    {
        if (!LinkCode.HasValidFormat(code))
            return new AccessLinkResult.NotFound();

        var link = await repository.FindByCodeAsync(code, cancellationToken);
        if (link is null)
            return new AccessLinkResult.NotFound();

        // RN-03: link expirado não redireciona e não conta acesso.
        var todayUtc = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        if (link.IsExpiredOn(todayUtc))
            return new AccessLinkResult.Expired(link.ExpiresOn.Value);

        // O UPDATE repete o filtro de expiração: se o dia virou entre a leitura e a escrita, o acesso não conta.
        if (!await repository.TryRegisterAccessAsync(code, todayUtc, cancellationToken))
            return new AccessLinkResult.Expired(link.ExpiresOn ?? todayUtc);

        return new AccessLinkResult.Redirect(link.TargetUrl);
    }

    // RF-07: qualquer pessoa que conheça o código pode consultar o total de acessos.
    public async Task<Link?> GetStatsAsync(string code, CancellationToken cancellationToken = default) =>
        LinkCode.HasValidFormat(code) ? await repository.FindByCodeAsync(code, cancellationToken) : null;

    private async Task<CreateLinkResult> CreateWithCustomCodeAsync(
        string code, Func<string, Link> newLink, CancellationToken cancellationToken)
    {
        // RN-01: a checagem dá uma resposta rápida; o índice único (D-04) cobre a corrida.
        if (await repository.CodeExistsAsync(code, cancellationToken))
            return new CreateLinkResult.CodeInUse(code);

        var link = newLink(code);
        return await repository.TryAddAsync(link, cancellationToken)
            ? new CreateLinkResult.Created(link)
            : new CreateLinkResult.CodeInUse(code);
    }

    private async Task<CreateLinkResult> CreateWithGeneratedCodeAsync(
        Func<string, Link> newLink, CancellationToken cancellationToken)
    {
        // RN-06: se o código gerado já estiver em uso, gera outro.
        for (var attempt = 1; attempt <= MaxGenerationAttempts; attempt++)
        {
            var code = codeGenerator.Generate();
            if (!await repository.CodeExistsAsync(code, cancellationToken))
            {
                var link = newLink(code);
                if (await repository.TryAddAsync(link, cancellationToken))
                    return new CreateLinkResult.Created(link);
            }

            Log.GeneratedCodeInUse(logger, code, attempt);
        }

        Log.CodeGenerationFailed(logger, MaxGenerationAttempts);
        return new CreateLinkResult.CodeGenerationFailed();
    }

    private static void AddErrorIfInvalid<T>(Dictionary<string, string[]> errors, string field, ValidationResult<T>? result)
    {
        if (result is not null && !result.IsValid)
            errors[field] = [result.Error];
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Link {Code} criado.")]
        public static partial void LinkCreated(ILogger logger, string code);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Código gerado {Code} já está em uso (tentativa {Attempt}).")]
        public static partial void GeneratedCodeInUse(ILogger logger, string code, int attempt);

        [LoggerMessage(Level = LogLevel.Error, Message = "Não foi possível gerar um código único após {Attempts} tentativas.")]
        public static partial void CodeGenerationFailed(ILogger logger, int attempts);
    }
}
