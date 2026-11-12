/*
 * Description  : Shared mapping of the primary key and audit columns for Module 01 tables.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
using FarmFMS.Domain.Common;
using FarmFMS.Infrastructure.Persistence.Naming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmFMS.Infrastructure.Persistence.Configurations.Module01;

/// <summary>
/// Table name, <c>Id INT IDENTITY(1,1)</c> primary key (<c>PK{Table}</c>) and the four audit columns exactly as in the
/// approved Module 01 specification: CreatedBy INT NOT NULL, CreatedDate DATETIME2 NOT NULL (no DEFAULT),
/// ModifiedBy INT NULL, ModifiedDate DATETIME2 NULL. DATETIME2 uses the default precision (7).
/// </summary>
internal static class AuditColumns
{
    public static void Configure<T>(EntityTypeBuilder<T> builder, string table) where T : AuditableEntity
    {
        builder.HasKey(e => e.Id).HasName(DbObjectNames.PrimaryKey(table));
        builder.Property(e => e.Id).UseIdentityColumn(1, 1);

        builder.Property(e => e.CreatedBy).IsRequired();
        builder.Property(e => e.CreatedDate).HasColumnType("datetime2").IsRequired();
        builder.Property(e => e.ModifiedBy);
        builder.Property(e => e.ModifiedDate).HasColumnType("datetime2");
    }

    /// <summary>
    /// NOT NULL BIT column with a named DEFAULT constraint <c>DF{Table}_{Column}</c>.
    /// </summary>
    public static void BitDefault<T>(EntityTypeBuilder<T> builder, string table, string column, bool value)
        where T : class
    {
        var property = builder.Property<bool>(column).IsRequired().HasDefaultValue(value);
        property.Metadata.SetDefaultConstraintName(DbObjectNames.Default(table, column));
    }

    /// <summary>
    /// NOT NULL INT column with a named DEFAULT constraint <c>DF{Table}_{Column}</c>.
    /// </summary>
    public static void IntDefault<T>(EntityTypeBuilder<T> builder, string table, string column, int value)
        where T : class
    {
        var property = builder.Property<int>(column).IsRequired().HasDefaultValue(value);
        property.Metadata.SetDefaultConstraintName(DbObjectNames.Default(table, column));
    }
}
