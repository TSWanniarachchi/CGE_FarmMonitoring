/*
 * Description  : EF Core mapping of table Permission (Module 01 database specification v0.2).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
using FarmFMS.Domain.Module01;
using FarmFMS.Domain.Module01.Authorization;
using FarmFMS.Infrastructure.Persistence.Naming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmFMS.Infrastructure.Persistence.Configurations.Module01;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    private const string Table = "Permission";

    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable(Table, DbObjectNames.DefaultSchema);
        AuditColumns.Configure(builder, Table);

        builder.Property(e => e.Name).HasMaxLength(Module01FieldLengths.PermissionName).IsUnicode().IsRequired();
        builder.Property(e => e.Code).HasMaxLength(Module01FieldLengths.PermissionCode).IsUnicode().IsRequired();
        builder.Property(e => e.Description).HasMaxLength(Module01FieldLengths.PermissionDescription).IsUnicode();

        // Specification: "IsActive BIT DEFAULT(1)" (no NOT NULL stated); mapped NOT NULL like the other masters (see hand-off).
        AuditColumns.BitDefault(builder, Table, nameof(Permission.IsActive), true);

        builder.HasAlternateKey(e => e.Name)
            .HasName(DbObjectNames.Unique(Table, nameof(Permission.Name)));
        builder.HasAlternateKey(e => e.Code)
            .HasName(DbObjectNames.Unique(Table, nameof(Permission.Code)));
    }
}
