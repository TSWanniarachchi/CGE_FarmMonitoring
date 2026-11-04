using System.Text.RegularExpressions;
using FarmFMS.Infrastructure.Persistence.Naming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

/*
 * Description  : Checks an EF Core model against the mandatory database rules (no FKs, dbo, approved descriptive names).
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.ArchitectureTests.Support;

/// <summary>
/// Returns the violations of each database rule for an EF Core model. Used on the real
/// FarmFmsDbContext model (must be empty) and on a deliberately invalid model (proves each rule detects violations).
/// <list type="bullet">
/// <item>No foreign keys; no navigations.</item>
/// <item>Every table in schema dbo.</item>
/// <item>Tables and columns: PascalCase letters/digits only (no underscores, so "UserId" not "Customer_Id");
/// no "Lk" link-table prefix (the Module 01 specification overrides the PMS standard).</item>
/// <item>PK{Table}, UQ{Table}_{Columns}, IX{Table}_{Columns}, DF{Table}_{Column}, CK{Table}_{Column}
/// (AD-08, FarmFMS_Architecture_Decisions_v2.md); DEFAULT constraints named explicitly (deterministic names);
/// no duplicate names.</item>
/// <item>No custom length limit: every identifier only has to be within SQL Server's 128-character limit.</item>
/// </list>
/// </summary>
internal static partial class ModelRuleChecker
{
    /// <summary>
    /// SQL Server's identifier limit; the only length rule.
    /// </summary>
    public const int MaxLength = DbObjectNames.MaxLength;

    public static IReadOnlyList<string> ForeignKeys(IModel model) =>
        model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys().Select(fk => $"FK {e.DisplayName()} -> {fk.PrincipalEntityType.DisplayName()}"))
            .ToList();

    public static IReadOnlyList<string> Navigations(IModel model) =>
        model.GetEntityTypes()
            .SelectMany(e => e.GetNavigations().Select(n => $"Navigation {e.DisplayName()}.{n.Name}")
                .Concat(e.GetSkipNavigations().Select(n => $"Navigation {e.DisplayName()}.{n.Name}")))
            .ToList();

    public static IReadOnlyList<string> Schemas(IModel model) =>
        Tables(model)
            .Where(t => !string.Equals(t.Schema, DbObjectNames.DefaultSchema, StringComparison.Ordinal))
            .Select(t => $"Table {t.Name} is in schema '{t.Schema ?? "(none)"}', expected '{DbObjectNames.DefaultSchema}'")
            .Distinct()
            .ToList();

    public static IReadOnlyList<string> TableNames(IModel model) =>
        Tables(model)
            .Select(t => t.Name)
            .Distinct()
            .SelectMany(name => IdentifierViolations("Table", name)
                .Concat(LinkPrefixRegex().IsMatch(name) ? [$"Table {name} uses the Lk prefix (not used by the Module 01 specification)"] : []))
            .ToList();

    public static IReadOnlyList<string> ColumnNames(IModel model) =>
        model.GetEntityTypes()
            .Where(e => e.GetTableName() is not null)
            .SelectMany(e =>
            {
                var table = Table(e);
                return e.GetProperties()
                    .Select(p => p.GetColumnName(table))
                    .OfType<string>()
                    .SelectMany(column => IdentifierViolations($"Column {table.Name}.", column));
            })
            .ToList();

    /// <summary>
    /// Every primary key, unique constraint, index, check constraint and DEFAULT constraint name in the model.
    /// A null name means the object is unnamed (only possible for DEFAULT constraints).
    /// </summary>
    public static IReadOnlyList<(DbObjectKind Kind, string Owner, string? Name)> ObjectNames(IModel model)
    {
        var names = new List<(DbObjectKind, string, string?)>();

        foreach (var entity in model.GetEntityTypes().Where(e => e.GetTableName() is not null))
        {
            var table = Table(entity);

            foreach (var key in entity.GetDeclaredKeys())
            {
                names.Add((key.IsPrimaryKey() ? DbObjectKind.PrimaryKey : DbObjectKind.UniqueConstraint, table.Name, key.GetName(table)));
            }

            foreach (var index in entity.GetDeclaredIndexes())
                names.Add((DbObjectKind.Index, table.Name, index.GetDatabaseName(table)));

            foreach (var check in entity.GetDeclaredCheckConstraints())
                names.Add((DbObjectKind.CheckConstraint, table.Name, check.GetName(table)));

            foreach (var property in entity.GetDeclaredProperties().Where(HasDefault))
                names.Add((DbObjectKind.DefaultConstraint, $"{table.Name}.{property.GetColumnName(table)}", property.GetDefaultConstraintName(table)));
        }

        return names;
    }

    /// <summary>
    /// Approved forms (PK{Table}, UQ/IX/DF/CK{Table}_{Column(s)}) within SQL Server's limit; DEFAULT constraints
    /// named explicitly; sequences within SQL Server's limit.
    /// </summary>
    public static IReadOnlyList<string> KeyAndConstraintNames(IModel model)
    {
        var violations = new List<string>();

        foreach (var (kind, owner, name) in ObjectNames(model))
        {
            if (string.IsNullOrEmpty(name))
            {
                violations.Add(kind == DbObjectKind.DefaultConstraint
                    ? $"DefaultConstraint on {owner} must be named explicitly (SQL Server would generate a non-deterministic DF__ name)"
                    : $"{kind} on {owner} has no name");
            }
            else if (!DbObjectNames.IsValid(name, kind))
            {
                var form = kind == DbObjectKind.PrimaryKey ? "PK{Table}" : $"{DbObjectNames.Prefix(kind)}{{Table}}_{{Column(s)}}";
                violations.Add($"{kind} {name} on {owner} must have the form {form} (and at most {MaxLength} characters)");
            }
        }

        foreach (var sequence in model.GetSequences())
            violations.AddRange(IdentifierViolations("Sequence", sequence.Name));

        return violations;
    }

    /// <summary>
    /// No two database objects may share a name (constraint names are schema-scoped in SQL Server; indexes are
    /// checked model-wide as well, so generated names never collide).
    /// </summary>
    public static IReadOnlyList<string> DuplicateNames(IModel model) =>
        ObjectNames(model)
            .Where(o => !string.IsNullOrEmpty(o.Name))
            .GroupBy(o => o.Name!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => $"Name {g.Key} is used {g.Count()} times: {string.Join(", ", g.Select(o => $"{o.Kind} on {o.Owner}"))}")
            .ToList();

    public static IReadOnlyList<string> All(IModel model) =>
        [.. ForeignKeys(model), .. Navigations(model), .. Schemas(model), .. TableNames(model), .. ColumnNames(model), .. KeyAndConstraintNames(model), .. DuplicateNames(model)];

    #region Helpers

    private static IEnumerable<StoreObjectIdentifier> Tables(IModel model) =>
        model.GetEntityTypes().Where(e => e.GetTableName() is not null).Select(Table);

    private static StoreObjectIdentifier Table(IEntityType entity) =>
        StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());

    private static bool HasDefault(IProperty property) =>
        property.FindAnnotation(Microsoft.EntityFrameworkCore.Metadata.RelationalAnnotationNames.DefaultValue) is not null
        || property.FindAnnotation(Microsoft.EntityFrameworkCore.Metadata.RelationalAnnotationNames.DefaultValueSql) is not null;

    private static IEnumerable<string> IdentifierViolations(string kind, string? name)
    {
        if (string.IsNullOrEmpty(name))
            yield return $"{kind} has no name";
        else if (name.Length > MaxLength)
            yield return $"{kind}{(kind.EndsWith('.') ? "" : " ")}{name} exceeds {MaxLength} characters ({name.Length})";
        else if (!IdentifierRegex().IsMatch(name))
            yield return $"{kind}{(kind.EndsWith('.') ? "" : " ")}{name} is not PascalCase letters/digits";
    }

    [GeneratedRegex(@"^[A-Z][A-Za-z0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();

    [GeneratedRegex(@"^Lk[A-Z]", RegexOptions.CultureInvariant)]
    private static partial Regex LinkPrefixRegex();

    #endregion
}
