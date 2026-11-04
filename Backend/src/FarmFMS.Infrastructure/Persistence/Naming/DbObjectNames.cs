using System.Text.RegularExpressions;

/*
 * Description  : Builds and validates the approved, descriptive names for keys, constraints and indexes.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Infrastructure.Persistence.Naming;

/// <summary>
/// Kind of named database object.
/// </summary>
public enum DbObjectKind
{
    PrimaryKey,
    UniqueConstraint,
    Index,
    DefaultConstraint,
    CheckConstraint
}

/// <summary>
/// Approved database object names (Architecture Decision AD-08, FarmFMS_Architecture_Decisions_v2.md):
/// <list type="table">
/// <item><term>Primary key</term><description><c>PK{Table}</c></description></item>
/// <item><term>Unique constraint</term><description><c>UQ{Table}_{ColumnOrColumns}</c></description></item>
/// <item><term>Index</term><description><c>IX{Table}_{ColumnOrColumns}</c></description></item>
/// <item><term>Default constraint</term><description><c>DF{Table}_{Column}</c></description></item>
/// <item><term>Check constraint</term><description><c>CK{Table}_{Column}</c></description></item>
/// </list>
/// Names are meaningful and descriptive: the full table and column names, columns concatenated in their order
/// (e.g. <c>UQUserRole_UserIdRoleId</c>). There is no custom length limit; the only limit is SQL Server's
/// identifier maximum of 128 characters. Only if a full name would exceed it are the approved abbreviations
/// (<see cref="DbAbbreviations"/>) applied; if it still doesn't fit, the builder throws. Names are never truncated.
/// Names are deterministic; uniqueness across the model is checked by the architecture tests.
/// Foreign keys are prohibited, so there is no FK name.
/// </summary>
public static partial class DbObjectNames
{
    /// <summary>
    /// SQL Server's maximum identifier length (sysname). Not a Farm FMS naming rule.
    /// </summary>
    public const int MaxLength = 128;

    /// <summary>
    /// Default (and only) schema.
    /// </summary>
    public const string DefaultSchema = "dbo";

    public const string PrimaryKeyPrefix = "PK";
    public const string UniqueConstraintPrefix = "UQ";
    public const string IndexPrefix = "IX";
    public const string DefaultConstraintPrefix = "DF";
    public const string CheckConstraintPrefix = "CK";

    #region Builders

    /// <summary>
    /// Primary key, e.g. PrimaryKey("RoleMenuAccess") -> "PKRoleMenuAccess".
    /// </summary>
    public static string PrimaryKey(string table) =>
        Build(DbObjectKind.PrimaryKey, table, []);

    /// <summary>
    /// Unique constraint, e.g. Unique("UserRole", "UserId", "RoleId") -> "UQUserRole_UserIdRoleId".
    /// </summary>
    public static string Unique(string table, params string[] columns) =>
        Build(DbObjectKind.UniqueConstraint, table, RequireColumns(columns));

    /// <summary>
    /// Index, e.g. Index("LoginHistory", "UserId", "EventDate") -> "IXLoginHistory_UserIdEventDate".
    /// </summary>
    public static string Index(string table, params string[] columns) =>
        Build(DbObjectKind.Index, table, RequireColumns(columns));

    /// <summary>
    /// Default constraint, e.g. Default("AppUser", "IsActive") -> "DFAppUser_IsActive".
    /// </summary>
    public static string Default(string table, string column) =>
        Build(DbObjectKind.DefaultConstraint, table, RequireColumns([column]));

    /// <summary>
    /// Check constraint, e.g. Check("LoginHistory", "EventType") -> "CKLoginHistory_EventType".
    /// </summary>
    public static string Check(string table, string column) =>
        Build(DbObjectKind.CheckConstraint, table, RequireColumns([column]));

    /// <summary>
    /// The candidate names in the order they are tried: the full descriptive name, then (only needed beyond
    /// SQL Server's 128-character limit) the name with approved abbreviations. Exposed for tests and diagnostics.
    /// </summary>
    public static IReadOnlyList<string> Candidates(DbObjectKind kind, string table, params string[] columns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(table);

        var prefix = Prefix(kind);

        return new[]
        {
            Compose(prefix, table, columns),
            Compose(prefix, DbAbbreviations.Abbreviate(table), columns.Select(DbAbbreviations.Abbreviate))
        }.Distinct(StringComparer.Ordinal).ToList();
    }

    private static string Build(DbObjectKind kind, string table, string[] columns)
    {
        var candidates = Candidates(kind, table, columns);

        var name = candidates.FirstOrDefault(c => c.Length <= MaxLength)
            ?? throw new InvalidOperationException(
                $"The {kind} name for {table}({string.Join(", ", columns)}) exceeds SQL Server's {MaxLength}-character " +
                $"identifier limit even with the approved abbreviations ({candidates[^1].Length} characters). " +
                "Names are never truncated: choose an approved explicit name.");

        return EnsureValid(name, kind);
    }

    private static string Compose(string prefix, string table, IEnumerable<string> columns)
    {
        var columnPart = string.Concat(columns);
        return columnPart.Length == 0 ? $"{prefix}{table}" : $"{prefix}{table}_{columnPart}";
    }

    private static string[] RequireColumns(string[] columns)
    {
        if (columns.Length == 0 || columns.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("At least one column name is required.", nameof(columns));

        return columns;
    }

    #endregion

    #region Validation

    public static string Prefix(DbObjectKind kind) => kind switch
    {
        DbObjectKind.PrimaryKey => PrimaryKeyPrefix,
        DbObjectKind.UniqueConstraint => UniqueConstraintPrefix,
        DbObjectKind.Index => IndexPrefix,
        DbObjectKind.DefaultConstraint => DefaultConstraintPrefix,
        DbObjectKind.CheckConstraint => CheckConstraintPrefix,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    /// <summary>
    /// True when <paramref name="name"/> has the approved form for <paramref name="kind"/>
    /// and is within SQL Server's identifier limit.
    /// </summary>
    public static bool IsValid(string? name, DbObjectKind kind)
    {
        if (name is not { Length: > 0 and <= MaxLength }) return false;

        var match = kind == DbObjectKind.PrimaryKey
            ? PrimaryKeyRegex().Match(name)
            : TableColumnRegex().Match(name);

        return match.Success && match.Groups["prefix"].Value == Prefix(kind);
    }

    /// <summary>
    /// Throws when <paramref name="name"/> is not a valid name for <paramref name="kind"/>.
    /// </summary>
    public static string EnsureValid(string name, DbObjectKind kind)
    {
        if (!IsValid(name, kind))
        {
            var form = kind == DbObjectKind.PrimaryKey ? "PK{Table}" : $"{Prefix(kind)}{{Table}}_{{Column(s)}}";
            throw new InvalidOperationException(
                $"Database object name '{name}' must have the form {form} with PascalCase letters/digits " +
                $"(and at most SQL Server's {MaxLength} characters).");
        }

        return name;
    }

    // PK + PascalCase table part, no underscore.
    [GeneratedRegex(@"^(?<prefix>PK)[A-Z][A-Za-z0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex PrimaryKeyRegex();

    // Prefix + PascalCase table part + "_" + PascalCase column part.
    [GeneratedRegex(@"^(?<prefix>UQ|IX|DF|CK)[A-Z][A-Za-z0-9]*_[A-Z][A-Za-z0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex TableColumnRegex();

    #endregion
}
