using FarmFMS.ArchitectureTests.Support;
using FarmFMS.Infrastructure.Data;
using FarmFMS.Infrastructure.Persistence.Naming;

/*
 * Description  : Verifies the approved naming decisions against the specification workbook and the naming helpers.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.ArchitectureTests;

/// <summary>
/// AD-08 (FarmFMS_Architecture_Decisions_v2.md): approved abbreviations, preserved specification procedure names,
/// the Usr rule for new procedure names, and the documented composite-name example. No custom length limit.
/// </summary>
public class NamingPolicyTests
{
    private static readonly string[] ApprovedAbbreviations =
        ["Usr", "Pwd", "Hier", "Sess", "Terr", "Hist", "Perm", "Rev", "Deact", "Act", "Req", "Enc", "Rol"];

    [Fact]
    public void Abbreviation_list_is_exactly_the_approved_list()
    {
        Assert.Equal(
            ApprovedAbbreviations.Order(StringComparer.Ordinal),
            DbAbbreviations.Words.Values.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Preserved_procedure_names_match_the_Module_01_specification_exactly()
    {
        var specification = SpecificationWorkbook.FirstColumn("Stored Procedures");

        Assert.Equal(
            specification.Order(StringComparer.Ordinal),
            ProcedureNamingPolicy.Module01SpecificationNames.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Specification_procedure_names_are_valid_when_dbo_qualified()
    {
        Assert.All(ProcedureNamingPolicy.Module01SpecificationNames, name =>
        {
            var reference = $"{StoredProcedureName.Schema}.{name}";
            Assert.True(StoredProcedureName.TryValidate(reference, out var error), error);
            Assert.Empty(ProcedureNamingPolicy.Check(reference));
        });
    }

    [Theory]
    [InlineData("dbo.SupUserSessRev")]   // specification name with "User": preserved
    [InlineData("dbo.ScrUserRole")]
    [InlineData("dbo.SreUsrList")]       // new name with "Usr"
    [InlineData("dbo.SreRolList")]
    public void Usr_rule_accepts_specification_names_and_new_Usr_names(string reference)
    {
        Assert.Empty(ProcedureNamingPolicy.Check(reference));
    }

    [Theory]
    [InlineData("dbo.SreUserList")]      // new name with "User"
    [InlineData("dbo.SupAppUserName")]   // new name containing the AppUser word "User"
    [InlineData("[dbo].[SdeUserSess]")]
    public void Usr_rule_rejects_User_in_new_names(string reference)
    {
        Assert.NotEmpty(ProcedureNamingPolicy.Check(reference));
    }

    [Fact]
    public void Composite_example_from_the_decision_document_is_reproduced()
    {
        // Descriptive names (the 15-character limit was removed; the former compact form UQUsrRol_UsrRol is withdrawn).
        Assert.Equal("UQUserRole_UserIdRoleId", DbObjectNames.Unique("UserRole", "UserId", "RoleId"));
        Assert.Equal("IXUserRole_UserIdRoleId", DbObjectNames.Index("UserRole", "UserId", "RoleId"));
    }
}
