using System.Reflection;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.ArchitectureTests.Support;
using FarmFMS.Infrastructure.Data;
using FarmFMS.Infrastructure.Persistence.Naming;
using FarmFMS.Infrastructure.Repositories.Module01;

/*
 * Description  : Step 0 (spec 5, 10.2): Module01Procedures constants vs the specification, the SQL scripts and the error catalogue.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.ArchitectureTests;

/// <summary>
/// Step 0 specification section 5 (procedure inventory N1-N8 plus the specification procedures), section 5.4 (error catalogue),
/// AD-08 (names), AD-09 (error contract), Database_Conventions.md section 6. Static checks only: no script is executed
/// (SQL Server integration tests are Blocked, AD-23).
/// <para>
/// The generic rules over every <c>*Procedures</c> class (valid dbo-qualified names, "Usr" for new names) are in
/// <c>DataAccessSourceRuleTests</c>; this class adds the Module 01 specifics.
/// </para>
/// </summary>
public class Module01ProcedureInventoryTests
{
    /// <summary>Specification procedures Step 0 uses (spec 5.1, 5.3): names preserved exactly, including "User".</summary>
    private static readonly string[] SpecificationProcedures =
    [
        "ScrAppUser", "ScrUserAccount", "SreAppUserById", "SvaAppUserLogin", "SvaUsrPerm", "ScrLoginHist", "SupUserSessRev"
    ];

    /// <summary>New procedures of spec 5.2: N1-N8 (DBG-012).</summary>
    private static readonly string[] NewProcedures =
    [
        "ScrUsrSess",               // N1
        "SupAppUsrLoginSuccess",    // N2
        "SupAppUsrLoginFailure",    // N3
        "SreUsrSessByTokenHash",    // N4
        "ScrUsrSessRotate",         // N5
        "SvaUsrSess",               // N6
        "SupAppUsrPwd",             // N7
        "SreAppUsrSystemId"         // N8
    ];

    private static IReadOnlyDictionary<string, string> Constants() =>
        typeof(Module01Procedures)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToDictionary(f => f.Name, f => (string)f.GetRawConstantValue()!);

    private static IReadOnlyList<SolutionFiles.SourceFile> Scripts() =>
        SolutionFiles.SqlScripts.Where(s => s.RelativePath.StartsWith("StoredProcedures/Module01/", StringComparison.Ordinal)).ToList();

    private static string ScriptName(SolutionFiles.SourceFile script) =>
        Path.GetFileNameWithoutExtension(script.RelativePath)["dbo.".Length..];

    private static readonly IReadOnlySet<int> RegisteredErrors = StoredProcedureErrors.Catalog.Keys.ToHashSet();

    #region Constants

    [Fact]
    public void Module01Procedures_has_exactly_the_Step_0_inventory()
    {
        var expected = SpecificationProcedures.Concat(NewProcedures).Order(StringComparer.Ordinal).ToList();

        Assert.Equal(15, expected.Count);
        Assert.Equal(expected, Constants().Keys.Order(StringComparer.Ordinal).ToList());
    }

    [Fact]
    public void Every_constant_is_the_dbo_qualified_name_of_its_field()
    {
        var offenders = Constants().Where(c => c.Value != $"{StoredProcedureName.Schema}.{c.Key}").Select(c => $"{c.Key} = \"{c.Value}\"").ToList();

        Assert.True(offenders.Count == 0, string.Join("; ", offenders));
        Assert.All(Constants().Values, value =>
        {
            Assert.True(StoredProcedureName.TryValidate(value, out var error), error);
            Assert.StartsWith("dbo.", value, StringComparison.Ordinal);
        });
        Assert.Equal(Constants().Count, Constants().Values.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Specification_procedure_names_are_unchanged_and_come_from_the_workbook()
    {
        var workbook = SpecificationWorkbook.FirstColumn("Stored Procedures").ToHashSet(StringComparer.Ordinal);
        var constants = Constants();

        Assert.All(SpecificationProcedures, name =>
        {
            Assert.Contains(name, workbook);                                    // the approved workbook (v0.2)
            Assert.Contains(name, ProcedureNamingPolicy.Module01SpecificationNames);
            Assert.True(constants.TryGetValue(name, out var value), $"Module01Procedures.{name} is missing.");
            Assert.Equal($"dbo.{name}", value);
        });

        // The two spec names that keep "User" are preserved (they must not have been renamed to "Usr").
        Assert.Equal("dbo.SupUserSessRev", constants["SupUserSessRev"]);
        Assert.Equal("dbo.ScrUserAccount", constants["ScrUserAccount"]);
        Assert.Equal("dbo.SreAppUserById", constants["SreAppUserById"]);
        Assert.Equal("dbo.SvaAppUserLogin", constants["SvaAppUserLogin"]);
    }

    [Fact]
    public void New_procedure_names_are_not_specification_names_and_contain_no_User()
    {
        var constants = Constants();

        Assert.All(NewProcedures, name =>
        {
            Assert.True(constants.ContainsKey(name), $"Module01Procedures.{name} is missing.");
            Assert.DoesNotContain(name, SpecificationProcedures);
            Assert.DoesNotContain(name, ProcedureNamingPolicy.Module01SpecificationNames);
            Assert.DoesNotContain("User", DbAbbreviations.SplitWords(name));    // AD-08: new names use Usr
            Assert.Empty(ProcedureNamingPolicy.Check(constants[name]));
        });

        // Anything that is not a specification name is a new name, and every new name is on the approved N1-N8 list.
        var unplanned = constants.Keys.Except(SpecificationProcedures).Except(NewProcedures).ToList();
        Assert.True(unplanned.Count == 0, $"Procedures outside spec 5.1 / 5.2 / 5.3: {string.Join(", ", unplanned)}");
    }

    #endregion

    #region Scripts <-> constants

    [Fact]
    public void Every_constant_has_a_script_and_every_script_has_a_constant()
    {
        var scripts = Scripts();
        Assert.NotEmpty(scripts);

        var scriptNames = scripts.Select(ScriptName).Order(StringComparer.Ordinal).ToList();
        var constantNames = Constants().Keys.Order(StringComparer.Ordinal).ToList();

        Assert.Equal(constantNames, scriptNames);
    }

    [Fact]
    public void Each_script_creates_exactly_the_procedure_named_by_its_file()
    {
        var offenders = new List<string>();

        foreach (var script in Scripts())
        {
            var created = ProcedureScriptRules.CreatedProcedures(script.Text);
            if (created.Count != 1 || created[0] != ScriptName(script))
                offenders.Add($"{script.RelativePath} creates [{string.Join(", ", created)}]");
        }

        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void Every_script_declares_CREATE_OR_ALTER()
    {
        var offenders = Scripts()
            .Where(s => !System.Text.RegularExpressions.Regex.IsMatch(ProcedureScriptRules.StripComments(s.Text), @"\bCREATE\s+OR\s+ALTER\s+PROC", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            .Select(s => s.RelativePath)
            .ToList();

        Assert.True(offenders.Count == 0, $"Scripts without CREATE OR ALTER: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Scripts_follow_the_Step_0_conventions()
    {
        var offenders = Scripts()
            .SelectMany(s => ProcedureScriptRules.ConventionViolations(s.Text).Select(v => $"{s.RelativePath}: {v}"))
            .ToList();

        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));
    }

    #endregion

    #region THROW numbers <-> error catalogue

    [Fact]
    public void Every_THROW_in_a_Module01_script_uses_the_generic_number_or_a_registered_business_number()
    {
        var offenders = Scripts()
            .SelectMany(s => ProcedureScriptRules.ThrowViolations(s.Text, RegisteredErrors).Select(v => $"{s.RelativePath}: {v}"))
            .ToList();

        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void The_scripts_do_raise_numbers_so_the_THROW_scan_is_not_vacuous()
    {
        var raised = Scripts().SelectMany(s => ProcedureScriptRules.ThrowNumbers(s.Text)).ToHashSet();

        // Spec 5.4: 50001 (N5), 50002 (N1), 50003 and 50004 (ScrLoginHist, N7).
        Assert.Superset(new HashSet<int> { 50000, 50001, 50002, 50003, 50004 }, raised.ToHashSet());
    }

    [Fact]
    public void Every_Module01_catalogue_entry_is_raised_by_a_script()
    {
        // A registered number nobody raises is dead configuration (catalogue entries are added "only when the raising
        // Stored Procedure is approved").
        var raised = Scripts().SelectMany(s => ProcedureScriptRules.ThrowNumbers(s.Text)).ToHashSet();
        var module01 = StoredProcedureErrors.Catalog.Keys.Where(StoredProcedureErrorRange.Module01.Contains).ToList();

        Assert.NotEmpty(module01);
        var dead = module01.Where(n => !raised.Contains(n)).ToList();
        Assert.True(dead.Count == 0, $"Catalogue entries without a raising script: {string.Join(", ", dead)}");
    }

    [Fact]
    public void The_Step_0_catalogue_entries_match_spec_5_4()
    {
        var catalog = StoredProcedureErrors.Catalog;

        Assert.Equal("Auth.SessionInvalid", catalog[50001].Code);
        Assert.Equal("Auth.UserNotActive", catalog[50002].Code);
        Assert.Equal("LoginHistory.EventTypeInvalid", catalog[50003].Code);
        Assert.Equal("User.NotFound", catalog[50004].Code);
        Assert.Equal("Invalid username or password.", catalog[50002].Message); // generic: no enumeration oracle
    }

    #endregion

    #region LoginHistory is append-only (AC-08)

    [Fact]
    public void No_script_updates_or_deletes_LoginHistory_rows()
    {
        var scripts = Scripts();
        Assert.Contains(scripts, s => ScriptName(s) == "ScrLoginHist"); // the insert procedure exists

        var offenders = scripts.Where(s => ProcedureScriptRules.ModifiesTable(s.Text, "LoginHistory")).Select(s => s.RelativePath).ToList();

        Assert.True(offenders.Count == 0, $"LoginHistory is append-only (spec 'Audit history'): {string.Join(", ", offenders)}");
    }

    [Fact]
    public void The_login_history_repository_contract_only_creates()
    {
        var methods = typeof(ILoginHistoryRepository).GetMethods().Select(m => m.Name).ToList();

        Assert.Equal(["CreateAsync"], methods);
    }

    [Fact]
    public void Only_ScrLoginHist_is_a_LoginHistory_procedure()
    {
        Assert.DoesNotContain(Constants().Keys, name => name.Contains("LoginHist", StringComparison.Ordinal) && name != "ScrLoginHist");
    }

    #endregion

    #region Rules detect violations

    [Theory]
    [InlineData("CREATE OR ALTER PROCEDURE dbo.ScrX AS BEGIN THROW 50999, N'x', 1; END", false)]
    [InlineData("THROW 50000, N'generic', 1;", false)]
    [InlineData("THROW 50004, N'registered', 1;", false)]
    [InlineData("-- THROW 59999, N'comment', 1;\nTHROW 50001, N'x', 1;", false)]
    [InlineData("BEGIN CATCH THROW; END CATCH", false)]                      // re-throw has no number
    [InlineData("THROW 50777, N'unregistered', 1;", true)]
    [InlineData("THROW 51000, N'outside the range', 1;", true)]
    [InlineData("THROW 49999, N'below the range', 1;", true)]
    [InlineData("DECLARE @n INT = 50001; THROW @n, N'variable', 1;", true)]
    public void Throw_rule_detects_unregistered_out_of_range_and_non_literal_numbers(string sql, bool violates)
    {
        var registered = new HashSet<int> { 50001, 50004, 50999 };
        var violations = ProcedureScriptRules.ThrowViolations(sql, registered);

        Assert.Equal(violates, violations.Count > 0);
    }

    [Theory]
    [InlineData("CREATE OR ALTER PROCEDURE dbo.ScrX AS SELECT 1;", "ScrX")]
    [InlineData("CREATE PROCEDURE [dbo].[ScrX] AS SELECT 1;", "ScrX")]
    [InlineData("-- CREATE PROCEDURE dbo.Hidden\nALTER PROC dbo.SupY AS SELECT 1;", "SupY")]
    public void Created_procedure_rule_reads_the_name(string sql, string expected)
    {
        Assert.Equal([expected], ProcedureScriptRules.CreatedProcedures(sql));
    }

    [Fact]
    public void Created_procedure_rule_flags_an_unqualified_name_and_several_procedures()
    {
        Assert.Equal(["?ScrX"], ProcedureScriptRules.CreatedProcedures("CREATE PROCEDURE ScrX AS SELECT 1;"));
        Assert.Equal(2, ProcedureScriptRules.CreatedProcedures("CREATE PROC dbo.A AS SELECT 1; GO CREATE PROC dbo.B AS SELECT 1;").Count);
    }

    [Theory]
    [InlineData("AS BEGIN SET XACT_ABORT ON; SELECT SYSUTCDATETIME(); END", "NOCOUNT")]
    [InlineData("AS BEGIN SET NOCOUNT ON; SELECT 1; END", "XACT_ABORT")]
    [InlineData("AS BEGIN SET NOCOUNT ON; SET XACT_ABORT ON; SELECT GETDATE(); END", "GETDATE")]
    [InlineData("AS BEGIN SET NOCOUNT ON; SET XACT_ABORT ON; SELECT GETUTCDATE(); END", "GETUTCDATE")]
    [InlineData("AS BEGIN SET NOCOUNT ON; SET XACT_ABORT ON; EXEC sp_executesql @sql; END", "dynamic SQL")]
    [InlineData("AS BEGIN SET NOCOUNT ON; SET XACT_ABORT ON; EXEC('SELECT 1'); END", "dynamic SQL")]
    public void Convention_rule_detects_violations(string sql, string expectedFragment)
    {
        Assert.Contains(ProcedureScriptRules.ConventionViolations(sql), v => v.Contains(expectedFragment));
    }

    [Fact]
    public void Convention_rule_accepts_a_conforming_script_and_ignores_comments()
    {
        const string sql = """
            -- uses GETDATE() and EXEC('x') only in this comment
            AS
            BEGIN
                SET NOCOUNT ON;
                SET XACT_ABORT ON;
                SELECT SYSUTCDATETIME();
                EXEC dbo.SomeProcedure @Id = 1;
            END
            """;

        Assert.Empty(ProcedureScriptRules.ConventionViolations(sql));
    }

    [Theory]
    [InlineData("UPDATE dbo.LoginHistory SET FailureReason = NULL;", true)]
    [InlineData("update [dbo].[LoginHistory] set x = 1", true)]
    [InlineData("DELETE FROM dbo.LoginHistory WHERE Id = 1;", true)]
    [InlineData("DELETE dbo.LoginHistory;", true)]
    [InlineData("MERGE INTO dbo.LoginHistory AS t USING x ON 1 = 1 WHEN MATCHED THEN DELETE;", true)]
    [InlineData("TRUNCATE TABLE dbo.LoginHistory;", true)]
    [InlineData("INSERT INTO dbo.LoginHistory (UserId) VALUES (1);", false)]
    [InlineData("SELECT * FROM dbo.LoginHistory;", false)]
    [InlineData("UPDATE dbo.UserSession SET RevokedDate = SYSUTCDATETIME();", false)]
    [InlineData("-- UPDATE dbo.LoginHistory SET x = 1", false)]
    public void Append_only_rule_detects_modification_of_a_table(string sql, bool modifies)
    {
        Assert.Equal(modifies, ProcedureScriptRules.ModifiesTable(sql, "LoginHistory"));
    }

    #endregion
}
