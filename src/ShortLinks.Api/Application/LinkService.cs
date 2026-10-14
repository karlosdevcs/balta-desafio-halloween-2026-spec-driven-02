using ShortLinks.Api.Domain;

namespace ShortLinks.Api.Application;

public class LinkService(ILinkRepository repository, ICodeGenerator codeGenerator, TimeProvider timeProvider)
{
    // D-05: limite de tentativas para a RN-06.
    public const int MaxGenerationAttempts = 5;

    public async Task<CreateLinkResult> CreateAsync(CreateLinkCommand command, CancellationToken cancellationToken = default)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var errors = new Dictionary<string, string[]>();

        if (TargetUrlValidator.Validate(command.TargetUrl) is { } urlError)
            errors["targetUrl"] = [urlError];

        if (command.Code is not null && CustomCodeValidator.Validate(command.Code) is { } codeError)
            errors["code"] = [codeError];

        DateOnly? expiresOn = null;
        if (command.ExpiresAt is not null)
        {
            if (ExpirationValidator.Validate(command.ExpiresAt, DateOnly.FromDateTime(nowUtc), out var parsed) is { } expirationError)
                errors["expiresAt"] = [expirationError];
            else
                expiresOn = parsed;
        }

        if (errors.Count > 0)
            return new CreateLinkResult.Invalid(errors);

        return command.Code is not null
            ? await CreateWithCustomCodeAsync(command.Code, command.TargetUrl!, nowUtc, expiresOn, cancellationToken)
            : await CreateWithGeneratedCodeAsync(command.TargetUrl!, nowUtc, expiresOn, cancellationToken);
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
            return new AccessLinkResult.Expired(link.ExpiresOn!.Value);

        // O UPDATE também filtra a expiração: se o dia virou entre a leitura e a escrita, não conta.
        if (!await repository.TryRegisterAccessAsync(code, todayUtc, cancellationToken))
            return new AccessLinkResult.Expired(link.ExpiresOn!.Value);

        return new AccessLinkResult.Redirect(link.TargetUrl);
    }

    // RF-07: qualquer pessoa que conheça o código pode consultar o total de acessos.
    public async Task<Link?> GetStatsAsync(string code, CancellationToken cancellationToken = default) =>
        LinkCode.HasValidFormat(code) ? await repository.FindByCodeAsync(code, cancellationToken) : null;

    private async Task<CreateLinkResult> CreateWithCustomCodeAsync(
        string code, string targetUrl, DateTime nowUtc, DateOnly? expiresOn, CancellationToken cancellationToken)
    {
        // RN-01: a checagem dá uma resposta rápida; o índice único (D-04) cobre a corrida.
        if (await repository.CodeExistsAsync(code, cancellationToken))
            return new CreateLinkResult.CodeInUse(code);

        var link = new Link(code, targetUrl, nowUtc, expiresOn);
        return await repository.TryAddAsync(link, cancellationToken)
            ? new CreateLinkResult.Created(link)
            : new CreateLinkResult.CodeInUse(code);
    }

    private async Task<CreateLinkResult> CreateWithGeneratedCodeAsync(
        string targetUrl, DateTime nowUtc, DateOnly? expiresOn, CancellationToken cancellationToken)
    {
        // RN-06: se o código gerado já estiver em uso, gera outro.
        for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            var code = codeGenerator.Generate();
            if (await repository.CodeExistsAsync(code, cancellationToken))
                continue;

            var link = new Link(code, targetUrl, nowUtc, expiresOn);
            if (await repository.TryAddAsync(link, cancellationToken))
                return new CreateLinkResult.Created(link);
        }

        return new CreateLinkResult.CodeGenerationFailed();
    }
}
