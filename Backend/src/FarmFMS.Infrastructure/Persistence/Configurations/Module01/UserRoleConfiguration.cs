/*
 * Description  : EF Core mapping of table UserRole (Module 01 database specification v0.2).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
using FarmFMS.Domain.Module01.Authorization;
using FarmFMS.Infrastructure.Persistence.Naming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmFMS.Infrastructure.Persistence.Configurations.Module01;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    private const string Table = "UserRole";

    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable(Table, DbObjectNames.DefaultSchema);
        AuditColumns.Configure(builder, Table);

        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.RoleId).IsRequired();

        builder.HasAlternateKey(e => new { e.UserId, e.RoleId })
            .HasName(DbObjectNames.Unique(Table, nameof(UserRole.UserId), nameof(UserRole.RoleId)));
    }
}
