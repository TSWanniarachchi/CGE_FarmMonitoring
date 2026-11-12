/*
 * Description  : EF Core mapping of table Menu (Module 01 database specification v0.2).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
using FarmFMS.Domain.Module01;
using FarmFMS.Domain.Module01.Authorization;
using FarmFMS.Infrastructure.Persistence.Naming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmFMS.Infrastructure.Persistence.Configurations.Module01;

internal sealed class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    private const string Table = "Menu";

    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        // Platform 1 = Web, 2 = Mobile, 3 = Both (D-16).
        builder.ToTable(Table, DbObjectNames.DefaultSchema, t =>
            t.HasCheckConstraint(DbObjectNames.Check(Table, nameof(Menu.Platform)), "[Platform] IN (1, 2, 3)"));
        AuditColumns.Configure(builder, Table);

        // ParentMenuId is a logical self-reference only (no foreign key, AD-07).
        builder.Property(e => e.ParentMenuId);
        builder.Property(e => e.Name).HasMaxLength(Module01FieldLengths.MenuName).IsUnicode().IsRequired();
        builder.Property(e => e.Code).HasMaxLength(Module01FieldLengths.MenuCode).IsUnicode().IsRequired();
        builder.Property(e => e.Icon).HasMaxLength(Module01FieldLengths.MenuIcon).IsUnicode();
        builder.Property(e => e.Route).HasMaxLength(Module01FieldLengths.MenuRoute).IsUnicode();
        builder.Property(e => e.Platform).IsRequired();

        AuditColumns.IntDefault(builder, Table, nameof(Menu.DisplayOrder), 0);
        AuditColumns.BitDefault(builder, Table, nameof(Menu.IsActive), true);

        builder.HasAlternateKey(e => e.Code)
            .HasName(DbObjectNames.Unique(Table, nameof(Menu.Code)));
        builder.HasIndex(e => new { e.ParentMenuId, e.DisplayOrder })
            .HasDatabaseName(DbObjectNames.Index(Table, nameof(Menu.ParentMenuId), nameof(Menu.DisplayOrder)));
    }
}
