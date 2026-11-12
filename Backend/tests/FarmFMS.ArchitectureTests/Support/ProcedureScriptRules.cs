using System.Text.RegularExpressions;

/*
 * Description  : Static analysis of Stored Procedure scripts: created names, THROW numbers, header statements, banned constructs.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.ArchitectureTests.Support;

/// <summary>
/// Step 0 specification section 5 / Database_Conventions.md section 6 (error contract). Static text rules only: nothing
/// is executed against a database (integration tests are Blocked, AD-23).
/// </summary>
internal static partial class ProcedureScriptRules
{
    public static string StripComments(string sql) =>
        LineCommentRegex().Replace(BlockCommentRegex().Replace(sql, " "), " ");

    /// <summary>
    /// Names (without schema) of the procedures a script creates or alters.
    /// </summary>
    public static IReadOnlyList<string> CreatedProcedures(string sql) =>
        CreateProcedureRegex().Matches(StripComments(sql))
            .Select(m => m.Groups["name"].Value.Replace("[", "").Replace("]", ""))
            .Select(name => name.Contains("dbo.", StringComparison.OrdinalIgnoreCase) ? name[(name.LastIndexOf('.') + 1)..] : $"?{name}")
            .ToList();

    /// <summary>
    /// The literal numbers of every THROW statement with arguments; <c>-1</c> for a THROW whose number is not a literal.
    /// A bare <c>THROW;</c> (re-throw) is not listed.
    /// </summary>
    public static IReadOnlyList<int> ThrowNumbers(string sql) =>
        ThrowRegex().Matches(StripComments(sql))
            .Select(m => m.Groups["number"].Success ? int.Parse(m.Groups["number"].Value) : -1)
            .ToList();

    /// <summary>
    /// Violations of the error contract (AD-09): a number in 50001-50999 must be registered; 50000 is the generic error;
    /// everything else is not allowed (numbers must be literals so they can be checked).
    /// </summary>
    public static IReadOnlyList<string> ThrowViolations(string sql, IReadOnlySet<int> registered)
    {
        var violations = new List<string>();

        foreach (var number in ThrowNumbers(sql))
        {
            if (number == -1)
                violations.Add("THROW must use a literal error number.");
            else if (number == 50000)
                continue;
            else if (number is >= 50001 and <= 50999)
            {
                if (!registered.Contains(number))
                    violations.Add($"THROW {number} is not registered in StoredProcedureErrors.Catalog.");
            }
            else
                violations.Add($"THROW {number} is outside the allowed range 50000-50999.");
        }

        return violations;
    }

    /// <summary>
    /// Violations of the script conventions of Step 0 spec section 5: SET NOCOUNT ON and SET XACT_ABORT ON, the database
    /// clock is SYSUTCDATETIME(), no dynamic SQL.
    /// </summary>
    public static IReadOnlyList<string> ConventionViolations(string sql)
    {
        var code = StripComments(sql);
        var violations = new List<string>();

        if (!NoCountRegex().IsMatch(code)) violations.Add("missing SET NOCOUNT ON.");
        if (!XactAbortRegex().IsMatch(code)) violations.Add("missing SET XACT_ABORT ON.");
        if (LocalClockRegex().IsMatch(code)) violations.Add("uses GETDATE / GETUTCDATE / SYSDATETIME (use SYSUTCDATETIME()).");
        if (DynamicSqlRegex().IsMatch(code)) violations.Add("uses dynamic SQL (EXEC(...) / sp_executesql).");

        return violations;
    }

    /// <summary>
    /// True when the script modifies or removes rows of <paramref name="table"/> (UPDATE, DELETE, MERGE, TRUNCATE).
    /// </summary>
    public static bool ModifiesTable(string sql, string table) =>
        Regex.IsMatch(
            StripComments(sql),
            $@"\b(UPDATE|DELETE(\s+FROM)?|MERGE(\s+INTO)?|TRUNCATE\s+TABLE)\s+(\[?dbo\]?\.)?\[?{Regex.Escape(table)}\]?\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex BlockCommentRegex();

    [GeneratedRegex(@"--[^\r\n]*", RegexOptions.CultureInvariant)]
    private static partial Regex LineCommentRegex();

    [GeneratedRegex(
        @"\b(CREATE|ALTER)\s+(OR\s+ALTER\s+)?PROC(EDURE)?\s+(?<name>[\[\]\w\.]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CreateProcedureRegex();

    [GeneratedRegex(@"\bTHROW\s+(?:(?<number>\d+)|[^;\s])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ThrowRegex();

    [GeneratedRegex(@"\bSET\s+NOCOUNT\s+ON\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NoCountRegex();

    [GeneratedRegex(@"\bSET\s+XACT_ABORT\s+ON\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex XactAbortRegex();

    [GeneratedRegex(@"\b(GETDATE|GETUTCDATE|SYSDATETIME|SYSDATETIMEOFFSET|CURRENT_TIMESTAMP)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LocalClockRegex();

    [GeneratedRegex(@"\bsp_executesql\b|\bEXEC(UTE)?\s*\(", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DynamicSqlRegex();
}
