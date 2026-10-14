using ShortLinks.Api.Domain;

namespace ShortLinks.Api.Application;

public interface ILinkRepository
{
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);

    /// <summary>Retorna false quando o código já existe no banco (índice único, D-04).</summary>
    Task<bool> TryAddAsync(Link link, CancellationToken cancellationToken);

    Task<Link?> FindByCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>Incrementa o contador de forma atômica se o link existe e não expirou (D-02).</summary>
    Task<bool> TryRegisterAccessAsync(string code, DateOnly todayUtc, CancellationToken cancellationToken);
}
