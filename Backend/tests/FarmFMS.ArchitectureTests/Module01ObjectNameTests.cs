using FarmFMS.Infrastructure.Persistence.Naming;

/*
 * Description  : Generates the names of the 40 Module 01 keys, constraints and indexes and checks them for collisions.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.ArchitectureTests;

/// <summary>
/// The Module 01 objects implied by the database specification v0.2 (table definitions and the
/// "Indexes &amp; Rules" sheet), named with <see cref="DbObjectNames"/>. Nothing is created in a database:
/// this pins the expected descriptive names (documented in Database_Conventions.md §3.3) and proves they are
/// deterministic, valid and collision-free before the schema is built.
/// <para>
/// DEFAULT constraints on CreatedDate are not listed: the specification does not say whether those columns get one.
/// The two check constraints apply only if check constraints are used for the "(proposed)" code lists.
/// </para>
/// </summary>
public class Module01ObjectNameTests
{
    /// <summary>
    /// (Expected name, generator). Order follows Database_Conventions.md §3.3.
    /// </summary>
    public static TheoryData<string, string> Objects => new()
    {
        // Primary keys (11)
        { "PKAppUser", DbObjectNames.PrimaryKey("AppUser") },
        { "PKRole", DbObjectNames.PrimaryKey("Role") },
        { "PKPermission", DbObjectNames.PrimaryKey("Permission") },
        { "PKMenu", DbObjectNames.PrimaryKey("Menu") },
        { "PKUserRole", DbObjectNames.PrimaryKey("UserRole") },
        { "PKRoleMenuAccess", DbObjectNames.PrimaryKey("RoleMenuAccess") },
        { "PKUserAccount", DbObjectNames.PrimaryKey("UserAccount") },
        { "PKUserTerritory", DbObjectNames.PrimaryKey("UserTerritory") },
        { "PKUserHierarchy", DbObjectNames.PrimaryKey("UserHierarchy") },
        { "PKLoginHistory", DbObjectNames.PrimaryKey("LoginHistory") },
        { "PKUserSession", DbObjectNames.PrimaryKey("UserSession") },

        // Unique constraints (10)
        { "UQAppUser_Username", DbObjectNames.Unique("AppUser", "Username") },
        { "UQRole_Name", DbObjectNames.Unique("Role", "Name") },
        { "UQPermission_Name", DbObjectNames.Unique("Permission", "Name") },
        { "UQPermission_Code", DbObjectNames.Unique("Permission", "Code") },
        { "UQMenu_Code", DbObjectNames.Unique("Menu", "Code") },
        { "UQUserRole_UserIdRoleId", DbObjectNames.Unique("UserRole", "UserId", "RoleId") },
        { "UQRoleMenuAccess_RoleIdMenuIdPermissionId", DbObjectNames.Unique("RoleMenuAccess", "RoleId", "MenuId", "PermissionId") },
        { "UQUserAccount_UserId", DbObjectNames.Unique("UserAccount", "UserId") },
        { "UQUserTerritory_UserAccountIdTerritoryId", DbObjectNames.Unique("UserTerritory", "UserAccountId", "TerritoryId") },
        { "UQUserSession_TokenHash", DbObjectNames.Unique("UserSession", "TokenHash") },

        // Indexes (4); the UserHierarchy one is a filtered unique index (an index, not a constraint)
        { "IXUserHierarchy_ASMAccountId", DbObjectNames.Index("UserHierarchy", "ASMAccountId") },
        { "IXLoginHistory_UserIdEventDate", DbObjectNames.Index("LoginHistory", "UserId", "EventDate") },
        { "IXMenu_ParentMenuIdDisplayOrder", DbObjectNames.Index("Menu", "ParentMenuId", "DisplayOrder") },
        { "IXRoleMenuAccess_RoleIdMenuId", DbObjectNames.Index("RoleMenuAccess", "RoleId", "MenuId") },

        // Default constraints (13)
        { "DFAppUser_IsActive", DbObjectNames.Default("AppUser", "IsActive") },
        { "DFAppUser_FailedLogCount", DbObjectNames.Default("AppUser", "FailedLogCount") },
        { "DFAppUser_IsLocked", DbObjectNames.Default("AppUser", "IsLocked") },
        { "DFAppUser_PwdChangeReq", DbObjectNames.Default("AppUser", "PwdChangeReq") },
        { "DFRole_IsSystemRole", DbObjectNames.Default("Role", "IsSystemRole") },
        { "DFRole_IsActive", DbObjectNames.Default("Role", "IsActive") },
        { "DFPermission_IsActive", DbObjectNames.Default("Permission", "IsActive") },
        { "DFMenu_DisplayOrder", DbObjectNames.Default("Menu", "DisplayOrder") },
        { "DFMenu_IsActive", DbObjectNames.Default("Menu", "IsActive") },
        { "DFUserAccount_CanAccessWeb", DbObjectNames.Default("UserAccount", "CanAccessWeb") },
        { "DFUserAccount_CanAccessMobile", DbObjectNames.Default("UserAccount", "CanAccessMobile") },
        { "DFUserAccount_IsActive", DbObjectNames.Default("UserAccount", "IsActive") },
        { "DFUserHierarchy_IsActive", DbObjectNames.Default("UserHierarchy", "IsActive") },

        // Check constraints (2)
        { "CKMenu_Platform", DbObjectNames.Check("Menu", "Platform") },
        { "CKLoginHistory_EventType", DbObjectNames.Check("LoginHistory", "EventType") }
    };

    [Theory]
    [MemberData(nameof(Objects))]
    public void Name_is_the_full_descriptive_name(string expected, string generated)
    {
        Assert.Equal(expected, generated);
    }

    [Fact]
    public void All_40_names_are_generated()
    {
        Assert.Equal(40, Objects.Count());
    }

    [Fact]
    public void No_two_objects_share_a_name()
    {
        var duplicates = Objects
            .Select(row => (string)row[1])
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.True(duplicates.Count == 0, $"Duplicate names: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void All_names_are_valid_and_unabbreviated()
    {
        var kinds = new Dictionary<string, DbObjectKind>
        {
            ["PK"] = DbObjectKind.PrimaryKey,
            ["UQ"] = DbObjectKind.UniqueConstraint,
            ["IX"] = DbObjectKind.Index,
            ["DF"] = DbObjectKind.DefaultConstraint,
            ["CK"] = DbObjectKind.CheckConstraint
        };

        Assert.All(Objects.Select(row => (string)row[1]), name =>
        {
            Assert.True(DbObjectNames.IsValid(name, kinds[name[..2]]), name);
            Assert.True(name.Length <= DbObjectNames.MaxLength, name);
            // Readability: full names are used, so the "User" abbreviation never appears.
            Assert.DoesNotContain("Usr", name);
        });
    }
}
