using FarmFMS.Infrastructure.Data;

namespace FarmFMS.UnitTests.Infrastructure;

public class StoredProcedureNameTests
{
    [Theory]
    // Names from the Module 01 database specification v0.2, dbo-qualified.
    [InlineData("dbo.ScrAppUser")]
    [InlineData("dbo.SreUsrPwdView")]
    [InlineData("dbo.SupUserSessRev")]
    [InlineData("dbo.SvaAppUserLogin")]
    [InlineData("dbo.SdeUserRole")]
    [InlineData("[dbo].[ScrUserRole]")]
    [InlineData("[dbo].SreAppUserList")]
    public void Dbo_qualified_names_are_accepted(string name)
    {
        Assert.True(StoredProcedureName.TryValidate(name, out var error), error);
    }

    [Theory]
    [InlineData("ScrAppUser")]
    [InlineData("[ScrAppUser]")]
    public void Unqualified_names_are_rejected(string name)
    {
        Assert.False(StoredProcedureName.TryValidate(name, out var error));
        Assert.Contains("schema-qualified", error);
    }

    [Theory]
    [InlineData("sales.ScrAppUser")]
    [InlineData("DBO.ScrAppUser")]
    [InlineData("[security].[ScrAppUser]")]
    public void Other_schemas_are_rejected(string name)
    {
        Assert.False(StoredProcedureName.TryValidate(name, out var error));
        Assert.Contains("'dbo' schema", error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SELECT * FROM AppUser")]
    [InlineData("dbo.ScrAppUser; DROP TABLE AppUser")]
    [InlineData("dbo.ScrAppUser --")]
    [InlineData("dbo.sp_who")]
    [InlineData("dbo.GetUsers")]                 // no operation prefix
    [InlineData("dbo.Screen")]                   // prefix not followed by a PascalCase name
    [InlineData("dbo.Scr_AppUser")]              // underscore
    public void Invalid_names_are_rejected(string? name)
    {
        Assert.False(StoredProcedureName.TryValidate(name, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Long_descriptive_names_are_accepted()
    {
        // No custom length limit (the 15-character rule was removed).
        Assert.True(StoredProcedureName.TryValidate("dbo.SreUsrTerritoryAssignmentHistory", out var error), error);
    }

    [Fact]
    public void Names_over_the_SQL_Server_limit_are_rejected()
    {
        var name = "Sre" + new string('X', 126); // 129 characters

        Assert.False(StoredProcedureName.TryValidate($"dbo.{name}", out var error));
        Assert.Contains("128", error);
    }

    [Fact]
    public void EnsureValid_throws_for_inline_SQL()
    {
        Assert.Throws<ArgumentException>(() => StoredProcedureName.EnsureValid("SELECT 1"));
    }
}
