using Microsoft.EntityFrameworkCore;
using ShortLinks.Api.Domain;

namespace ShortLinks.Api.Data;

public class ShortLinksDbContext(DbContextOptions<ShortLinksDbContext> options) : DbContext(options)
{
    public DbSet<Link> Links => Set<Link>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var link = modelBuilder.Entity<Link>();

        link.HasKey(l => l.Id);
        link.Property(l => l.Code).HasMaxLength(Link.MaxCodeLength).IsRequired();
        link.Property(l => l.TargetUrl).HasMaxLength(Link.MaxTargetUrlLength).IsRequired();
        link.Property(l => l.CreatedAtUtc)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        // D-04: a unicidade do código é garantida pelo banco, não só pelo serviço.
        link.HasIndex(l => l.Code).IsUnique();
    }
}
