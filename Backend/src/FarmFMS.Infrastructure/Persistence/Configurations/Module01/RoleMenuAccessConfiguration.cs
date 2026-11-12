/*
 * Description  : EF Core mapping of table RoleMenuAccess (Module 01 database specification v0.2).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
using FarmFMS.Domain.Module01.Authorization;
using FarmFMS.Infrastructure.Persistence.Naming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmFMS.Infrastructure.Persistence.Configurations.Module01;

internal sealed class RoleMenuAccessConfiguration : IEntityTypeConfiguration<RoleMenuAccess>
{
    private const string Table = "RoleMenuAccess";

    public void Configure(EntityTypeBuilder<RoleMenuAccess> builder)
    {
        builder.ToTable(Table, DbObjectNames.DefaultSchema);
        AuditColumns.Configure(builder, Table);

        builder.Property(e => e.RoleId).IsRequired();
        builder.Property(e => e.MenuId).IsRequired();
        builder.Property(e => e.PermissionId).IsRequired();

        builder.HasAlternateKey(e => new { e.RoleId, e.MenuId, e.PermissionId })
            .HasName(DbObjectNames.Unique(Table,
                nameof(RoleMenuAccess.RoleId), nameof(RoleMenuAccess.MenuId), nameof(RoleMenuAccess.PermissionId)));
        builder.HasIndex(e => new { e.RoleId, e.MenuId })
            .HasDatabaseName(DbObjectNames.Index(Table, nameof(RoleMenuAccess.RoleId), nameof(RoleMenuAccess.MenuId)));
    }
}
