using FarmFMS.Infrastructure.Persistence.Naming;
using Microsoft.EntityFrameworkCore;

namespace FarmFMS.Infrastructure.Persistence;

/// <summary>
/// EF Core context used ONLY for schema definition and migrations.
/// Application data access goes through Dapper + Stored Procedures, never through this context.
/// All objects live in the dbo schema; entity configurations must not set another schema.
/// </summary>
public sealed class FarmFmsDbContext(DbContextOptions<FarmFmsDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DbObjectNames.DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FarmFmsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
