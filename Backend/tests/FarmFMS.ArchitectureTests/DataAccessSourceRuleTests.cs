using System.Reflection;
using System.Text.RegularExpressions;
using FarmFMS.ArchitectureTests.Support;
using FarmFMS.Infrastructure.Data;

/*
 * Description  : Scans the Backend source for forbidden data-access patterns (inline SQL, EF data access, foreign keys).
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.ArchitectureTests;

/// <summary>
/// Source-level checks for the mandatory database rules, plus the SQL scripts in Database/.
/// Generated migrations are excluded only from the EF-API check (generated code); they are checked for
/// inline SQL, raw SQL / procedure deployment and foreign keys like all other code.
/// </summary>
public partial class DataAccessSourceRuleTests
{
    private const string PersistenceFolder = "FarmFMS.Infrastructure/Persistence";
    private const string InfrastructureDiFile = "FarmFMS.Infrastructure/DependencyInjection.cs";
    private const string ConnectionFactoryFile = "FarmFMS.Infrastructure/Data/SqlConnectionFactory.cs";

    [Fact]
    public void Source_files_are_found()
    {
        // Guards against the scan silently checking nothing.
        Assert.Contains(SolutionFiles.SourceFiles, f => f.RelativePath == "FarmFMS.Infrastructure/Data/StoredProcedureExecutor.cs");
    }

    [Fact]
    public void No_inline_SQL()
    {
        // Roslyn-based: finds SQL in regular, verbatim, raw, interpolated and '+'-concatenated strings, across lines.
        // Applies to every source file, migrations included.
        var offenders = SolutionFiles.SourceFiles
            .SelectMany(file => StringLiteralScanner.Scan(file.Text)
                .Where(s => StringLiteralScanner.ContainsSql(s.Text))
                .Select(s => $"{file.RelativePath}:{s.Line}"))
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Inline SQL is not allowed; use a Stored Procedure: {string.Join("; ", offenders)}");
    }

    [Fact]
    public void Migrations_do_not_run_raw_SQL_or_deploy_Stored_Procedures()
    {
        // AD-04: migrations manage schema only; Stored Procedures are separate scripts in Database/StoredProcedures.
        var offenders = Find(MigrationRawSqlRegex(), f => f.IsMigration);

        Assert.True(offenders.Count == 0,
            $"Migrations must not use migrationBuilder.Sql or deploy procedures: {string.Join("; ", offenders)}");
    }

    [Fact]
    public void Database_scripts_have_no_foreign_keys_and_valid_procedure_names()
    {
        var offenders = SolutionFiles.SqlScripts
            .SelectMany(script => SqlScriptRules.Check(script.Text).Select(v => $"{script.RelativePath}: {v}"))
            .ToList();

        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void Database_folder_exists()
    {
        Assert.True(Directory.Exists(SolutionFiles.DatabaseRoot), $"Missing {SolutionFiles.DatabaseRoot}");
    }

    [Fact]
    public void No_ad_hoc_commands_or_borrowed_EF_connections()
    {
        var offenders = Find(AdHocCommandRegex(), _ => true);

        Assert.True(offenders.Count == 0,
            $"Only the Stored Procedure executor may run commands: {string.Join("; ", offenders)}");
    }

    [Fact]
    public void No_EF_Core_data_access_APIs()
    {
        var offenders = Find(EfDataAccessRegex(), f => !f.IsMigration);

        Assert.True(offenders.Count == 0,
            $"EF Core is for schema and migrations only: {string.Join("; ", offenders)}");
    }

    [Fact]
    public void DbSet_is_used_only_in_persistence()
    {
        var offenders = Find(DbSetRegex(), f => !f.IsUnder(PersistenceFolder));

        Assert.True(offenders.Count == 0,
            $"DbSet is only allowed in {PersistenceFolder}: {string.Join("; ", offenders)}");
    }

    [Fact]
    public void FarmFmsDbContext_is_referenced_only_by_persistence_and_registration()
    {
        var offenders = Find(DbContextNameRegex(),
            f => !f.IsUnder(PersistenceFolder) && f.RelativePath != InfrastructureDiFile);

        Assert.True(offenders.Count == 0,
            $"FarmFmsDbContext must not be used for application data access: {string.Join("; ", offenders)}");
    }

    [Fact]
    public void No_foreign_key_constructs()
    {
        var offenders = Find(ForeignKeyRegex(), _ => true);

        Assert.True(offenders.Count == 0,
            $"Physical foreign keys and EF relationships are not allowed: {string.Join("; ", offenders)}");
    }

    [Fact]
    public void Dapper_is_used_only_in_Infrastructure()
    {
        var offenders = Find(DapperUsingRegex(), f => !f.IsUnder("FarmFMS.Infrastructure"));

        Assert.True(offenders.Count == 0,
            $"Dapper belongs to the Infrastructure layer: {string.Join("; ", offenders)}");
    }

    [Fact]
    public void Connections_are_created_only_by_the_connection_factory()
    {
        var offenders = Find(NewConnectionRegex(), f => f.RelativePath != ConnectionFactoryFile);

        Assert.True(offenders.Count == 0,
            $"Obtain connections through IDbConnectionFactory: {string.Join("; ", offenders)}");
    }

    [Fact]
    public void Stored_procedure_name_constants_follow_the_PMS_standard()
    {
        // Convention: procedure names are const strings in static classes named "<Module>Procedures".
        var offenders = typeof(StoredProcedureName).Assembly
            .GetTypes()
            .Where(t => t.Name.EndsWith("Procedures", StringComparison.Ordinal))
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (Field: $"{t.Name}.{f.Name}", Value: (string?)f.GetRawConstantValue())))
            .Where(c => !StoredProcedureName.TryValidate(c.Value, out _) || ProcedureNamingPolicy.Check(c.Value).Count > 0)
            .Select(c => $"{c.Field} = \"{c.Value}\"")
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Invalid Stored Procedure names: {string.Join("; ", offenders)}");
    }

    #region Scanning

    private static List<string> Find(Regex pattern, Func<SolutionFiles.SourceFile, bool> include)
    {
        var offenders = new List<string>();

        foreach (var file in SolutionFiles.SourceFiles.Where(include))
        {
            var lines = file.Text.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (IsComment(line)) continue;

                if (pattern.IsMatch(line))
                    offenders.Add($"{file.RelativePath}:{i + 1}");
            }
        }

        return offenders;
    }

    private static bool IsComment(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("//", StringComparison.Ordinal)
            || trimmed.StartsWith("/*", StringComparison.Ordinal)
            || trimmed.StartsWith('*');
    }

    #endregion

    #region Patterns

    [GeneratedRegex(@"\.Sql\s*\(|\bPROC(EDURE)?\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MigrationRawSqlRegex();

    [GeneratedRegex(
        @"CommandType\.Text|CommandType\.TableDirect|new\s+SqlCommand\b|\.CreateCommand\(|GetDbConnection\(|\.Database\.(BeginTransaction|ExecuteSql|SqlQuery)",
        RegexOptions.CultureInvariant)]
    private static partial Regex AdHocCommandRegex();

    [GeneratedRegex(
        @"\b(SaveChanges|SaveChangesAsync|FromSql|FromSqlRaw|FromSqlInterpolated|ExecuteSql|ExecuteSqlRaw|ExecuteSqlInterpolated|ExecuteSqlRawAsync|ExecuteSqlInterpolatedAsync|SqlQuery|SqlQueryRaw|ExecuteUpdate|ExecuteUpdateAsync|ExecuteDelete|ExecuteDeleteAsync|AsNoTracking|AsTracking)\s*\(|\.Set<",
        RegexOptions.CultureInvariant)]
    private static partial Regex EfDataAccessRegex();

    [GeneratedRegex(@"\bDbSet<", RegexOptions.CultureInvariant)]
    private static partial Regex DbSetRegex();

    [GeneratedRegex(@"\bFarmFmsDbContext\b", RegexOptions.CultureInvariant)]
    private static partial Regex DbContextNameRegex();

    [GeneratedRegex(
        @"\b(HasForeignKey|AddForeignKey|HasOne|HasMany|WithOne|WithMany|HasPrincipalKey|OnDelete)\b|\[ForeignKey\b|\.ForeignKey\(|\bFOREIGN\s+KEY\b|\bREFERENCES\s+\[?\w",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ForeignKeyRegex();

    [GeneratedRegex(@"^\s*using\s+Dapper\s*;", RegexOptions.CultureInvariant)]
    private static partial Regex DapperUsingRegex();

    [GeneratedRegex(@"new\s+SqlConnection\s*\(", RegexOptions.CultureInvariant)]
    private static partial Regex NewConnectionRegex();

    #endregion
}
