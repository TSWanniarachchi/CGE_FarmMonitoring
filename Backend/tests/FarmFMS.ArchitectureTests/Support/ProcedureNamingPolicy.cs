using System.Text.RegularExpressions;
using FarmFMS.Infrastructure.Persistence.Naming;

/*
 * Description  : Usr-vs-User rule for Stored Procedure names (spec names preserved, new names use Usr).
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.ArchitectureTests.Support;

/// <summary>
/// AD-08 (FarmFMS_Architecture_Decisions_v2.md):
/// <list type="bullet">
/// <item>Stored Procedure names defined by the Module 01 specification v0.2 are preserved exactly,
/// including those that use "User" (e.g. SupUserSessRev, ScrUserRole).</item>
/// <item>Newly introduced procedure names use "Usr" for the word User; the word "User" is not allowed in them.</item>
/// </list>
/// </summary>
internal static partial class ProcedureNamingPolicy
{
    /// <summary>
    /// Module 01 specification v0.2, sheet "Stored Procedures". Kept as a list so a change to the spec is noticed:
    /// a test compares it with the workbook.
    /// </summary>
    public static readonly IReadOnlySet<string> Module01SpecificationNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "ScrAppUser", "SreAppUserById", "SreAppUserList", "SupAppUser", "SvaAppUserLogin", "SvaUsrPerm",
        "ScrUserRole", "SdeUserRole", "ScrUserAccount", "ScrUserTerr", "SupUsrHier", "ScrLoginHist",
        "SupUserUnlock", "SupUserDeact", "SupUserSessRev", "SreUsrPwdView"
    };

    /// <summary>
    /// Violations for a procedure reference ("dbo.Name", "[dbo].[Name]" or "Name").
    /// </summary>
    public static IReadOnlyList<string> Check(string? procedureReference)
    {
        if (string.IsNullOrWhiteSpace(procedureReference)) return [];

        var name = NamePartRegex().Match(procedureReference).Groups["name"].Value;
        if (name.Length == 0 || Module01SpecificationNames.Contains(name)) return [];

        return DbAbbreviations.SplitWords(name).Contains("User", StringComparer.Ordinal)
            ? [$"New Stored Procedure name '{name}' must use 'Usr' instead of 'User' (only specification names keep 'User')."]
            : [];
    }

    [GeneratedRegex(@"(?:^|\.)\[?(?<name>[A-Za-z0-9]+)\]?$", RegexOptions.CultureInvariant)]
    private static partial Regex NamePartRegex();
}
