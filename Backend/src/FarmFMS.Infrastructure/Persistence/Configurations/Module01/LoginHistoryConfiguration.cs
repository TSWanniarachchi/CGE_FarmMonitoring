/*
 * Description  : EF Core mapping of table LoginHistory (Module 01 database specification v0.2).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
using FarmFMS.Domain.Module01;
using FarmFMS.Domain.Module01.Authentication;
using FarmFMS.Infrastructure.Persistence.Naming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmFMS.Infrastructure.Persistence.Configurations.Module01;

internal sealed class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
{
    private const string Table = "LoginHistory";

    public void Configure(EntityTypeBuilder<LoginHistory> builder)
    {
        // EventType 1 = Success, 2 = Failed, 3 = Logout (D-16).
        builder.ToTable(Table, DbObjectNames.DefaultSchema, t =>
            t.HasCheckConstraint(DbObjectNames.Check(Table, nameof(LoginHistory.EventType)), "[EventType] IN (1, 2, 3)"));
        AuditColumns.Configure(builder, Table);

        builder.Property(e => e.UserId);
        builder.Property(e => e.Username).HasMaxLength(Module01FieldLengths.Username).IsUnicode().IsRequired();
        builder.Property(e => e.EventType).IsRequired();
        builder.Property(e => e.EventDate).HasColumnType("datetime2").IsRequired();
        builder.Property(e => e.IPAddress).HasMaxLength(Module01FieldLengths.IPAddress).IsUnicode();
        builder.Property(e => e.DeviceInfo).HasMaxLength(Module01FieldLengths.DeviceInfo).IsUnicode();
        builder.Property(e => e.FailureReason).HasMaxLength(Module01FieldLengths.FailureReason).IsUnicode();

        builder.HasIndex(e => new { e.UserId, e.EventDate })
            .HasDatabaseName(DbObjectNames.Index(Table, nameof(LoginHistory.UserId), nameof(LoginHistory.EventDate)));
    }
}
