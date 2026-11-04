using FarmFMS.Infrastructure.Persistence.Naming;

namespace FarmFMS.UnitTests.Infrastructure;

public class NamingTests
{
    #region Abbreviations

    [Theory]
    [InlineData("UserTerritory", "UsrTerr")]
    [InlineData("UserHierarchy", "UsrHier")]
    [InlineData("LoginHistory", "LoginHist")]
    [InlineData("UserSession", "UsrSess")]
    [InlineData("UserRole", "UsrRol")]
    [InlineData("RoleId", "RolId")]
    [InlineData("AppUser", "AppUsr")]
    [InlineData("ASMAccountId", "ASMAccountId")]
    public void Abbreviations_are_applied_word_by_word(string identifier, string expected)
    {
        Assert.Equal(expected, DbAbbreviations.Abbreviate(identifier));
    }

    [Fact]
    public void Words_are_split_on_PascalCase_and_acronyms()
    {
        Assert.Equal(["ASM", "Account", "Id"], DbAbbreviations.SplitWords("ASMAccountId"));
    }

    #endregion

    #region Builders

    [Theory]
    [InlineData("AppUser", "PKAppUser")]
    [InlineData("UserTerritory", "PKUserTerritory")]
    [InlineData("RoleMenuAccess", "PKRoleMenuAccess")]   // full name; no abbreviation needed
    public void Primary_key_uses_PK_Table(string table, string expected)
    {
        Assert.Equal(expected, DbObjectNames.PrimaryKey(table));
    }

    [Fact]
    public void Unique_index_default_and_check_use_full_descriptive_names()
    {
        Assert.Equal("UQAppUser_Username", DbObjectNames.Unique("AppUser", "Username"));
        Assert.Equal("UQUserRole_UserIdRoleId", DbObjectNames.Unique("UserRole", "UserId", "RoleId"));
        Assert.Equal("IXMenu_ParentMenuIdDisplayOrder", DbObjectNames.Index("Menu", "ParentMenuId", "DisplayOrder"));
        Assert.Equal("DFUserAccount_CanAccessMobile", DbObjectNames.Default("UserAccount", "CanAccessMobile"));
        Assert.Equal("CKLoginHistory_EventType", DbObjectNames.Check("LoginHistory", "EventType"));
    }

    [Fact]
    public void Names_longer_than_15_characters_are_allowed()
    {
        var name = DbObjectNames.Unique("RoleMenuAccess", "RoleId", "MenuId", "PermissionId");

        Assert.Equal("UQRoleMenuAccess_RoleIdMenuIdPermissionId", name);
        Assert.True(name.Length > 15);
    }

    [Fact]
    public void Candidates_are_the_full_name_then_the_abbreviated_fallback()
    {
        Assert.Equal(
            ["UQUserRole_UserIdRoleId", "UQUsrRol_UsrIdRolId"],
            DbObjectNames.Candidates(DbObjectKind.UniqueConstraint, "UserRole", "UserId", "RoleId"));
    }

    [Fact]
    public void Abbreviations_are_used_only_beyond_the_SQL_Server_limit()
    {
        // Full name: "UQ" + table (121) + "_UserId" = 130 characters; abbreviated ("Usr" for "User") it is 128.
        var table = "UserTable" + new string('X', 112);

        var name = DbObjectNames.Unique(table, "UserId");

        Assert.Equal($"UQUsrTable{new string('X', 112)}_UsrId", name);
        Assert.True(name.Length <= DbObjectNames.MaxLength);
    }

    [Fact]
    public void Names_over_the_SQL_Server_limit_are_rejected_not_truncated()
    {
        var table = "Table" + new string('X', 130);

        var exception = Assert.Throws<InvalidOperationException>(() => DbObjectNames.PrimaryKey(table));

        Assert.Contains("never truncated", exception.Message);
    }

    [Fact]
    public void Names_are_deterministic()
    {
        Assert.Equal(
            DbObjectNames.Unique("UserRole", "UserId", "RoleId"),
            DbObjectNames.Unique("UserRole", "UserId", "RoleId"));
    }

    [Fact]
    public void Column_order_is_part_of_the_name()
    {
        Assert.NotEqual(
            DbObjectNames.Index("UserRole", "UserId", "RoleId"),
            DbObjectNames.Index("UserRole", "RoleId", "UserId"));
    }

    [Fact]
    public void Builders_require_columns()
    {
        Assert.Throws<ArgumentException>(() => DbObjectNames.Unique("Role"));
        Assert.Throws<ArgumentException>(() => DbObjectNames.Index("Role", " "));
    }

    #endregion

    #region Validation

    [Theory]
    [InlineData("PKAppUser", DbObjectKind.PrimaryKey, true)]
    [InlineData("PK_AppUser", DbObjectKind.PrimaryKey, false)]          // EF default form
    [InlineData("PKAppUser_Id", DbObjectKind.PrimaryKey, false)]        // PK{Table} has no column part
    [InlineData("UQUserRole_UserIdRoleId", DbObjectKind.UniqueConstraint, true)]
    [InlineData("UQUserRole", DbObjectKind.UniqueConstraint, false)]    // column part required
    [InlineData("AK_Role_Name", DbObjectKind.UniqueConstraint, false)]  // EF default form
    [InlineData("IXMenu_ParentMenuIdDisplayOrder", DbObjectKind.Index, true)]
    [InlineData("IX_Menu_Code", DbObjectKind.Index, false)]             // EF default form
    [InlineData("IXMenu_Code", DbObjectKind.CheckConstraint, false)]    // wrong prefix for the kind
    [InlineData("DFUserAccount_CanAccessMobile", DbObjectKind.DefaultConstraint, true)]
    [InlineData("DF__AppUser__IsAc__3B75D760", DbObjectKind.DefaultConstraint, false)] // SQL Server generated form
    [InlineData("CKLoginHistory_EventType", DbObjectKind.CheckConstraint, true)]
    [InlineData("CpkAppUsr_Id", DbObjectKind.PrimaryKey, false)]        // withdrawn PMS prefix
    [InlineData("", DbObjectKind.Index, false)]
    public void Names_are_validated_per_kind(string name, DbObjectKind kind, bool valid)
    {
        Assert.Equal(valid, DbObjectNames.IsValid(name, kind));
    }

    [Fact]
    public void Names_over_128_characters_are_invalid()
    {
        Assert.False(DbObjectNames.IsValid("PK" + new string('X', 127), DbObjectKind.PrimaryKey));
        Assert.True(DbObjectNames.IsValid("PK" + new string('X', 126), DbObjectKind.PrimaryKey));
    }

    #endregion
}
