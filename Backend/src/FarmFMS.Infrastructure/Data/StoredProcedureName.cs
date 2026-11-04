using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

/*
 * Description  : Validates Stored Procedure names against the PMS naming standard and the dbo schema rule.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Infrastructure.Data;

/// <summary>
/// Stored Procedure reference rules:
/// <list type="bullet">
/// <item>Always schema-qualified with <c>dbo</c>: "dbo.Name" or "[dbo].[Name]". Unqualified names and other schemas are rejected.</item>
/// <item>PascalCase name with an operation prefix
/// Scr = Create, Sre = Read, Sup = Update, Sde = Delete, Sva = Validate.</item>
/// <item>Within SQL Server's 128-character identifier limit (there is no custom length limit).</item>
/// </list>
/// The executor rejects anything that is not a valid procedure reference, so inline SQL cannot be executed.
/// </summary>
public static partial class StoredProcedureName
{
    /// <summary>
    /// SQL Server's maximum identifier length (sysname). Not a Farm FMS naming rule.
    /// </summary>
    public const int MaxLength = 128;

    /// <summary>
    /// The only schema Stored Procedures may live in.
    /// </summary>
    public const string Schema = "dbo";

    /// <summary>
    /// Allowed operation prefixes (PMS standard).
    /// </summary>
    public static readonly IReadOnlyList<string> Prefixes = ["Scr", "Sre", "Sup", "Sde", "Sva"];

    /// <summary>
    /// True when <paramref name="value"/> is a valid procedure reference; otherwise <paramref name="error"/> explains why.
    /// </summary>
    public static bool TryValidate(string? value, [NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            error = "The Stored Procedure name is required.";
            return false;
        }

        var match = QualifiedNameRegex().Match(value);
        if (!match.Success)
        {
            error = $"'{Truncate(value)}' is not a Stored Procedure reference. Only Stored Procedures can be executed.";
            return false;
        }

        var schema = match.Groups["schema"];
        if (!schema.Success)
        {
            error = $"Stored Procedure '{match.Groups["name"].Value}' must be schema-qualified as '{Schema}.{match.Groups["name"].Value}'.";
            return false;
        }

        if (!string.Equals(schema.Value, Schema, StringComparison.Ordinal))
        {
            error = $"Stored Procedures must be in the '{Schema}' schema, not '{schema.Value}'.";
            return false;
        }

        var name = match.Groups["name"].Value;
        if (name.Length > MaxLength)
        {
            error = $"Stored Procedure name '{name}' exceeds {MaxLength} characters.";
            return false;
        }

        if (!OperationNameRegex().IsMatch(name))
        {
            error = $"Stored Procedure name '{name}' must start with one of {string.Join(", ", Prefixes)} followed by a PascalCase name.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> when <paramref name="value"/> is not a valid procedure reference.
    /// </summary>
    public static void EnsureValid(string? value)
    {
        if (!TryValidate(value, out var error))
            throw new ArgumentException(error, nameof(value));
    }

    private static string Truncate(string value) => value.Length <= 40 ? value : value[..40] + "...";

    #region Regex

    // [schema].[Name] | schema.Name | [schema].Name | schema.[Name] | Name  - identifiers are letters/digits only.
    [GeneratedRegex(
        @"^(?:\[?(?<schema>[A-Za-z][A-Za-z0-9]*)\]?\.)?\[?(?<name>[A-Za-z][A-Za-z0-9]*)\]?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex QualifiedNameRegex();

    [GeneratedRegex(@"^(?:Scr|Sre|Sup|Sde|Sva)[A-Z][A-Za-z0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex OperationNameRegex();

    #endregion
}
