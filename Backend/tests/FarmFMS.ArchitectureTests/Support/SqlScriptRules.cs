using System.Text.RegularExpressions;
using FarmFMS.Infrastructure.Data;

/*
 * Description  : Checks version-controlled SQL scripts (Database/) for prohibited constructs.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.ArchitectureTests.Support;

/// <summary>
/// Rules for SQL scripts in the Database folder:
/// no foreign-key constraints, and every procedure created / altered is a valid dbo-qualified PMS name
/// that follows the Usr rule for new names (<see cref="ProcedureNamingPolicy"/>).
/// Comments are removed before checking.
/// </summary>
internal static partial class SqlScriptRules
{
    public static IReadOnlyList<string> Check(string sql)
    {
        var code = StripComments(sql);
        var violations = new List<string>();

        if (ForeignKeyRegex().IsMatch(code))
            violations.Add("Physical foreign keys are prohibited (FOREIGN KEY / REFERENCES).");

        foreach (Match match in CreateProcedureRegex().Matches(code))
        {
            var name = match.Groups["name"].Value;
            if (!StoredProcedureName.TryValidate(name, out var error))
                violations.Add(error);

            violations.AddRange(ProcedureNamingPolicy.Check(name));
        }

        return violations;
    }

    private static string StripComments(string sql) =>
        LineCommentRegex().Replace(BlockCommentRegex().Replace(sql, " "), " ");

    #region Regex

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex BlockCommentRegex();

    [GeneratedRegex(@"--[^\r\n]*", RegexOptions.CultureInvariant)]
    private static partial Regex LineCommentRegex();

    [GeneratedRegex(@"\bFOREIGN\s+KEY\b|\bREFERENCES\s+\S", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ForeignKeyRegex();

    [GeneratedRegex(
        @"\b(CREATE|ALTER)\s+(OR\s+ALTER\s+)?PROC(EDURE)?\s+(?<name>[\[\]\w\.]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CreateProcedureRegex();

    #endregion
}
