/*
 * Description  : EF Core mapping of table UserAccount (Module 01 database specification v0.2).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
using FarmFMS.Domain.Module01.Users;
using FarmFMS.Infrastructure.Persistence.Naming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmFMS.Infrastructure.Persistence.Configurations.Module01;

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    private const string Table = "UserAccount";

    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable(Table, DbObjectNames.DefaultSchema);
        AuditColumns.Configure(builder, Table);

        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.ProcessId).IsRequired();

        AuditColumns.BitDefault(builder, Table, nameof(UserAccount.CanAccessWeb), false);
        AuditColumns.BitDefault(builder, Table, nameof(UserAccount.CanAccessMobile), false);
        AuditColumns.BitDefault(builder, Table, nameof(UserAccount.IsActive), true);

        builder.HasAlternateKey(e => e.UserId)
            .HasName(DbObjectNames.Unique(Table, nameof(UserAccount.UserId)));
    }
}
