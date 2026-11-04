using FarmFMS.ArchitectureTests.Support;
using FarmFMS.Infrastructure.Persistence;
using FarmFMS.Infrastructure.Persistence.Naming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

/*
 * Description  : Verifies the EF Core schema model: no foreign keys, dbo schema, approved descriptive identifier names.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.ArchitectureTests;

/// <summary>
/// Builds the EF Core design-time model (the one migrations are generated from; no database connection is opened)
/// and checks the mandatory database rules. Each rule is also run against <see cref="ViolatingDbContext"/>
/// to prove it detects violations.
/// </summary>
public class EfModelRuleTests
{
    private const string NoConnection = "Server=architecture-tests;Database=ArchitectureTests;Trusted_Connection=True";

    private static readonly Lazy<IModel> FarmFmsModel = new(() =>
    {
        using var context = new FarmFmsDbContext(
            new DbContextOptionsBuilder<FarmFmsDbContext>().UseSqlServer(NoConnection).Options);

        return context.GetService<IDesignTimeModel>().Model;
    });

    private static readonly Lazy<IModel> ViolatingModel = new(() =>
    {
        using var context = new ViolatingDbContext(
            new DbContextOptionsBuilder<ViolatingDbContext>().UseSqlServer(NoConnection).Options);

        return context.GetService<IDesignTimeModel>().Model;
    });

    #region FarmFmsDbContext

    [Fact]
    public void Default_schema_is_dbo()
    {
        Assert.Equal(DbObjectNames.DefaultSchema, FarmFmsModel.Value.GetDefaultSchema());
    }

    [Fact]
    public void Model_has_no_physical_foreign_keys() => AssertNone(ModelRuleChecker.ForeignKeys(FarmFmsModel.Value));

    [Fact]
    public void Model_has_no_navigation_properties() => AssertNone(ModelRuleChecker.Navigations(FarmFmsModel.Value));

    [Fact]
    public void All_tables_are_in_dbo() => AssertNone(ModelRuleChecker.Schemas(FarmFmsModel.Value));

    [Fact]
    public void Table_names_follow_the_naming_rules() => AssertNone(ModelRuleChecker.TableNames(FarmFmsModel.Value));

    [Fact]
    public void Column_names_follow_the_naming_rules() => AssertNone(ModelRuleChecker.ColumnNames(FarmFmsModel.Value));

    [Fact]
    public void Key_constraint_index_default_and_sequence_names_follow_the_naming_rules() =>
        AssertNone(ModelRuleChecker.KeyAndConstraintNames(FarmFmsModel.Value));

    [Fact]
    public void Object_names_are_unique() => AssertNone(ModelRuleChecker.DuplicateNames(FarmFmsModel.Value));

    #endregion

    #region Rules detect violations

    [Fact]
    public void Foreign_key_rule_detects_relationships()
    {
        Assert.Single(ModelRuleChecker.ForeignKeys(ViolatingModel.Value));
        Assert.NotEmpty(ModelRuleChecker.Navigations(ViolatingModel.Value));
    }

    [Fact]
    public void Schema_rule_detects_non_dbo_schema()
    {
        Assert.Contains(ModelRuleChecker.Schemas(ViolatingModel.Value), v => v.Contains("'sales'"));
    }

    [Fact]
    public void Table_rule_detects_Lk_prefix_and_names_over_the_SQL_Server_limit()
    {
        var violations = ModelRuleChecker.TableNames(ViolatingModel.Value);

        Assert.Contains(violations, v => v.Contains("LkUserGroup") && v.Contains("Lk prefix"));
        Assert.Contains(violations, v => v.Contains(ViolatingDbContext.TooLongTableName) && v.Contains("exceeds 128"));
    }

    [Fact]
    public void Column_rule_detects_underscore_foreign_key_style()
    {
        Assert.Contains(ModelRuleChecker.ColumnNames(ViolatingModel.Value), v => v.Contains("Customer_Id"));
    }

    [Fact]
    public void Object_name_rule_detects_default_EF_names_and_unnamed_defaults()
    {
        var violations = ModelRuleChecker.KeyAndConstraintNames(ViolatingModel.Value);

        Assert.Contains(violations, v => v.StartsWith("PrimaryKey PK_LkUserGroup "));          // underscore: not PK{Table}
        Assert.Contains(violations, v => v.StartsWith("UniqueConstraint AK_LkUserGroup_Code ")); // wrong prefix
        Assert.Contains(violations, v => v.StartsWith("Index IX_LkUserGroup_Customer_Id "));     // form + length
        Assert.Contains(violations, v => v.StartsWith("CheckConstraint CK_Parent_Code "));       // form
        Assert.Contains(violations, v => v.StartsWith("Index CKChild_Parent "));                 // index with CK prefix
        Assert.Contains(violations, v => v.StartsWith("DefaultConstraint on LkUserGroup.IsActive must be named"));
        // EF Core silently truncates an over-limit sequence name to 128 characters ending in "~";
        // the rule reports the truncated name, so EF truncation can never pass unnoticed.
        Assert.Contains(violations, v =>
            v.StartsWith($"Sequence {ViolatingDbContext.TooLongSequenceName[..100]}") && v.Contains('~'));

        Assert.DoesNotContain(violations, v => v.Contains("PKChild "));
        Assert.DoesNotContain(violations, v => v.StartsWith("CheckConstraint CKChild_Parent "));
    }

    [Fact]
    public void Duplicate_name_rule_detects_shared_names()
    {
        var duplicate = Assert.Single(ModelRuleChecker.DuplicateNames(ViolatingModel.Value));
        Assert.Contains("CKChild_Parent", duplicate);
    }

    #endregion

    private static void AssertNone(IReadOnlyList<string> violations) =>
        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
}
