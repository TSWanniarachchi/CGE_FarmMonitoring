/*
 * Description  : EF Core mapping of table Role (Module 01 database specification v0.2).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
using FarmFMS.Domain.Module01;
using FarmFMS.Domain.Module01.Authorization;
using FarmFMS.Infrastructure.Persistence.Naming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmFMS.Infrastructure.Persistence.Configurations.Module01;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    private const string Table = "Role";

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable(Table, DbObjectNames.DefaultSchema);
        AuditColumns.Configure(builder, Table);

        builder.Property(e => e.Name).HasMaxLength(Module01FieldLengths.RoleName).IsUnicode().IsRequired();
        builder.Property(e => e.Description).HasMaxLength(Module01FieldLengths.RoleDescription).IsUnicode();

        AuditColumns.BitDefault(builder, Table, nameof(Role.IsSystemRole), false);
        AuditColumns.BitDefault(builder, Table, nameof(Role.IsActive), true);

        builder.HasAlternateKey(e => e.Name)
            .HasName(DbObjectNames.Unique(Table, nameof(Role.Name)));
    }
}
