/*
 * Description  : EF Core mapping of table AppUser (Module 01 database specification v0.2).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
using FarmFMS.Domain.Module01;
using FarmFMS.Domain.Module01.Users;
using FarmFMS.Infrastructure.Persistence.Naming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmFMS.Infrastructure.Persistence.Configurations.Module01;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    private const string Table = "AppUser";

    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable(Table, DbObjectNames.DefaultSchema);
        AuditColumns.Configure(builder, Table);

        builder.Property(e => e.Username).HasMaxLength(Module01FieldLengths.Username).IsUnicode().IsRequired();
        builder.Property(e => e.EncPassword).HasColumnType($"varbinary({Module01FieldLengths.EncPassword})").IsRequired();
        builder.Property(e => e.PasswordNonce).HasColumnType($"varbinary({Module01FieldLengths.PasswordNonce})").IsRequired();
        builder.Property(e => e.PasswordTag).HasColumnType($"varbinary({Module01FieldLengths.PasswordTag})").IsRequired();
        builder.Property(e => e.KeyVersion).HasMaxLength(Module01FieldLengths.KeyVersion).IsUnicode().IsRequired();
        builder.Property(e => e.FirstName).HasMaxLength(Module01FieldLengths.FirstName).IsUnicode().IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(Module01FieldLengths.LastName).IsUnicode();
        builder.Property(e => e.Email).HasMaxLength(Module01FieldLengths.Email).IsUnicode();
        builder.Property(e => e.MobileNo).HasMaxLength(Module01FieldLengths.MobileNo).IsUnicode();
        builder.Property(e => e.LockedDate).HasColumnType("datetime2");
        builder.Property(e => e.LastLoginDate).HasColumnType("datetime2");

        AuditColumns.BitDefault(builder, Table, nameof(AppUser.IsActive), true);
        AuditColumns.IntDefault(builder, Table, nameof(AppUser.FailedLogCount), 0);
        AuditColumns.BitDefault(builder, Table, nameof(AppUser.IsLocked), false);
        AuditColumns.BitDefault(builder, Table, nameof(AppUser.PwdChangeReq), false);

        builder.HasAlternateKey(e => e.Username)
            .HasName(DbObjectNames.Unique(Table, nameof(AppUser.Username)));
    }
}
