using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ShortLinks.Api.Application;
using ShortLinks.Api.Domain;

namespace ShortLinks.Api.Data;

public class LinkRepository(ShortLinksDbContext db) : ILinkRepository
{
    private const int SqliteConstraintUnique = 2067;

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
        db.Links.AnyAsync(l => l.Code == code, cancellationToken);

    public async Task<bool> TryAddAsync(Link link, CancellationToken cancellationToken)
    {
        db.Links.Add(link);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteConstraintUnique })
        {
            db.Entry(link).State = EntityState.Detached;
            return false;
        }
    }

    public Task<Link?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
        db.Links.AsNoTracking().SingleOrDefaultAsync(l => l.Code == code, cancellationToken);

    public async Task<bool> TryRegisterAccessAsync(string code, DateOnly todayUtc, CancellationToken cancellationToken)
    {
        // D-02: UPDATE Links SET AccessCount = AccessCount + 1 WHERE ..., sem ler e reescrever na aplicação.
        var affected = await db.Links
            .Where(l => l.Code == code && (l.ExpiresOn == null || l.ExpiresOn >= todayUtc))
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.AccessCount, l => l.AccessCount + 1), cancellationToken);

        return affected == 1;
    }
}
