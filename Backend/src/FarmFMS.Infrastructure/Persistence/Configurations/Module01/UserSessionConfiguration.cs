/*
 * Description  : EF Core mapping of table UserSession (Module 01 database specification v0.2).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
using FarmFMS.Domain.Module01;
using FarmFMS.Domain.Module01.Authentication;
using FarmFMS.Infrastructure.Persistence.Naming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmFMS.Infrastructure.Persistence.Configurations.Module01;

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    private const string Table = "UserSession";

    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable(Table, DbObjectNames.DefaultSchema);
        AuditColumns.Configure(builder, Table);

        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.TokenHash).HasColumnType($"varbinary({Module01FieldLengths.TokenHash})").IsRequired();
        builder.Property(e => e.LastActDate).HasColumnType("datetime2").IsRequired();
        builder.Property(e => e.ExpiresDate).HasColumnType("datetime2").IsRequired();
        builder.Property(e => e.RevokedDate).HasColumnType("datetime2");
        builder.Property(e => e.RevokedReason).HasMaxLength(Module01FieldLengths.RevokedReason).IsUnicode();
        builder.Property(e => e.IPAddress).HasMaxLength(Module01FieldLengths.IPAddress).IsUnicode();
        builder.Property(e => e.DeviceInfo).HasMaxLength(Module01FieldLengths.DeviceInfo).IsUnicode();

        builder.HasAlternateKey(e => e.TokenHash)
            .HasName(DbObjectNames.Unique(Table, nameof(UserSession.TokenHash)));
    }
}
