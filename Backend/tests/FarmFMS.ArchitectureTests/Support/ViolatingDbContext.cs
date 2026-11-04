using Microsoft.EntityFrameworkCore;

/*
 * Description  : A deliberately invalid EF Core model used to prove that every model rule detects violations.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.ArchitectureTests.Support;

/// <summary>
/// Breaks every database rule on purpose. Test-only; never part of the application.
/// </summary>
internal sealed class ViolatingDbContext(DbContextOptions<ViolatingDbContext> options) : DbContext(options)
{
    /// <summary>
    /// A table name over SQL Server's 128-character identifier limit (129 characters).
    /// </summary>
    public static readonly string TooLongTableName = "ChildTable" + new string('X', 119);

    /// <summary>
    /// A sequence name over SQL Server's 128-character identifier limit (130 characters).
    /// </summary>
    public static readonly string TooLongSequenceName = "Sequence" + new string('X', 122);

    public sealed class Parent
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int Customer_Id { get; set; }
        public bool IsActive { get; set; }
        public List<Child> Children { get; set; } = [];
    }

    public sealed class Child
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public Parent? Parent { get; set; }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>(TooLongSequenceName);

        modelBuilder.Entity<Parent>(entity =>
        {
            // Lk prefix + non-dbo schema + EF default check-constraint name.
            entity.ToTable("LkUserGroup", "sales", t => t.HasCheckConstraint("CK_Parent_Code", "[Code] <> ''"));

            entity.HasKey(p => p.Id);                          // default name PK_LkUserGroup
            entity.HasAlternateKey(p => p.Code);               // default name AK_LkUserGroup_Code
            entity.HasIndex(p => p.Customer_Id);               // default name IX_LkUserGroup_Customer_Id
            entity.Property(p => p.IsActive).HasDefaultValue(true); // unnamed DEFAULT constraint
        });

        modelBuilder.Entity<Child>(entity =>
        {
            // Valid PK name; a check constraint and an index that share one name (duplicate).
            entity.ToTable(TooLongTableName, "dbo", t => t.HasCheckConstraint("CKChild_Parent", "[ParentId] > 0"));
            entity.HasKey(c => c.Id).HasName("PKChild");
            entity.HasIndex(c => c.ParentId).HasDatabaseName("CKChild_Parent");
            entity.HasOne(c => c.Parent).WithMany(p => p.Children).HasForeignKey(c => c.ParentId);
        });
    }
}
