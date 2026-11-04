using System.Text.RegularExpressions;

/*
 * Description  : Central list of the abbreviations used to shorten database identifiers.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Infrastructure.Persistence.Naming;

/// <summary>
/// The approved abbreviations (Architecture Decision AD-08 in FarmFMS_Architecture_Decisions_v2.md):
/// Usr, Pwd, Hier, Sess, Terr, Hist, Perm, Rev, Deact, Act, Req, Enc, Rol.
/// <para>
/// Readability comes first: names are descriptive and are not abbreviated unnecessarily. The list is used where an
/// abbreviation is required by an approved rule (e.g. "Usr" in newly introduced Stored Procedure names) and by
/// <see cref="DbObjectNames"/> only as a fallback when a full name would exceed SQL Server's 128-character limit.
/// Table, column and Stored Procedure names defined by the Module 01 specification are used unchanged.
/// Do not add an entry without an approved decision (see Documentation/Database/Database_Conventions.md).
/// </para>
/// </summary>
public static partial class DbAbbreviations
{
    /// <summary>
    /// Word -> abbreviation. Words are PascalCase segments of an identifier.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Words { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Activity"] = "Act",
        ["Deactivate"] = "Deact",
        ["Encrypted"] = "Enc",
        ["Hierarchy"] = "Hier",
        ["History"] = "Hist",
        ["Password"] = "Pwd",
        ["Permission"] = "Perm",
        ["Required"] = "Req",
        ["Revoke"] = "Rev",
        ["Role"] = "Rol",
        ["Session"] = "Sess",
        ["Territory"] = "Terr",
        ["User"] = "Usr"
    };

    /// <summary>
    /// Replaces every word that has an approved abbreviation. Example: "UserTerritory" -> "UsrTerr".
    /// </summary>
    public static string Abbreviate(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        return string.Concat(SplitWords(identifier).Select(word => Words.GetValueOrDefault(word, word)));
    }

    /// <summary>
    /// Splits a PascalCase identifier into words. Example: "ASMAccountId" -> "ASM", "Account", "Id".
    /// </summary>
    public static IEnumerable<string> SplitWords(string identifier) =>
        WordRegex().Matches(identifier).Select(m => m.Value);

    [GeneratedRegex(@"[A-Z]+(?![a-z])|[A-Z][a-z0-9]*|[a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();
}
