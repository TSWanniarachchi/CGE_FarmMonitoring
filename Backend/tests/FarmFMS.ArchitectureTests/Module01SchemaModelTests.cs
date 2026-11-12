using System.Reflection;
using System.Text.RegularExpressions;
using FarmFMS.ArchitectureTests.Support;
using FarmFMS.Domain.Module01;
using FarmFMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

/*
 * Description  : Step 0 (spec 4, 10.2): the EF model and the migration contain exactly the 9 Step 0 tables as in the approved workbook.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.ArchitectureTests;

/// <summary>
/// Step 0 specification 4.1 (nine tables, D-27), 4.2 (object names from Database_Conventions.md 3.3), 4.3 (no navigations),
/// AD-07 (no foreign keys), AD-24 (dbo). Column names, SQL types, lengths, nullability and defaults are compared with the
/// approved Module 01 database specification v0.2 workbook (<see cref="SpecificationWorkbook"/>), never with a copied list.
/// The EF design-time model and the checked-in migration are both compared; nothing touches a database.
/// </summary>
public class Module01SchemaModelTests
{
    private const string NoConnection = "Server=architecture-tests;Database=ArchitectureTests;Trusted_Connection=True";

    private static readonly Lazy<IModel> Model = new(() =>
    {
        using var context = new FarmFmsDbContext(
            new DbContextOptionsBuilder<FarmFmsDbContext>().UseSqlServer(NoConnection).Options);

        return context.GetService<IDesignTimeModel>().Model;
    });

    public static readonly string[] Tables =
        ["AppUser", "UserAccount", "UserSession", "LoginHistory", "Role", "Permission", "Menu", "UserRole", "RoleMenuAccess"];

    public static TheoryData<string> TableNames => new(Tables);

    /// <summary>Spec 4.2, written out literally (independent of DbObjectNames).</summary>
    private sealed record ExpectedObjects(
        string PrimaryKey,
        Dictionary<string, string[]> Unique,
        Dictionary<string, string[]> Indexes,
        Dictionary<string, (string Column, int Value)> Defaults,
        string[] Checks);

    private static readonly Dictionary<string, ExpectedObjects> Expected = new()
    {
        ["AppUser"] = new("PKAppUser",
            new() { ["UQAppUser_Username"] = ["Username"] },
            [],
            new()
            {
                ["DFAppUser_IsActive"] = ("IsActive", 1),
                ["DFAppUser_FailedLogCount"] = ("FailedLogCount", 0),
                ["DFAppUser_IsLocked"] = ("IsLocked", 0),
                ["DFAppUser_PwdChangeReq"] = ("PwdChangeReq", 0)
            },
            []),
        ["UserAccount"] = new("PKUserAccount",
            new() { ["UQUserAccount_UserId"] = ["UserId"] },
            [],
            new()
            {
                ["DFUserAccount_CanAccessWeb"] = ("CanAccessWeb", 0),
                ["DFUserAccount_CanAccessMobile"] = ("CanAccessMobile", 0),
                ["DFUserAccount_IsActive"] = ("IsActive", 1)
            },
            []),
        ["UserSession"] = new("PKUserSession",
            new() { ["UQUserSession_TokenHash"] = ["TokenHash"] },
            [], [], []),
        ["LoginHistory"] = new("PKLoginHistory",
            [],
            new() { ["IXLoginHistory_UserIdEventDate"] = ["UserId", "EventDate"] },
            [],
            ["CKLoginHistory_EventType"]),
        ["Role"] = new("PKRole",
            new() { ["UQRole_Name"] = ["Name"] },
            [],
            new() { ["DFRole_IsSystemRole"] = ("IsSystemRole", 0), ["DFRole_IsActive"] = ("IsActive", 1) },
            []),
        ["Permission"] = new("PKPermission",
            new() { ["UQPermission_Name"] = ["Name"], ["UQPermission_Code"] = ["Code"] },
            [],
            new() { ["DFPermission_IsActive"] = ("IsActive", 1) },
            []),
        ["Menu"] = new("PKMenu",
            new() { ["UQMenu_Code"] = ["Code"] },
            new() { ["IXMenu_ParentMenuIdDisplayOrder"] = ["ParentMenuId", "DisplayOrder"] },
            new() { ["DFMenu_DisplayOrder"] = ("DisplayOrder", 0), ["DFMenu_IsActive"] = ("IsActive", 1) },
            ["CKMenu_Platform"]),
        ["UserRole"] = new("PKUserRole",
            new() { ["UQUserRole_UserIdRoleId"] = ["UserId", "RoleId"] },
            [], [], []),
        ["RoleMenuAccess"] = new("PKRoleMenuAccess",
            new() { ["UQRoleMenuAccess_RoleIdMenuIdPermissionId"] = ["RoleId", "MenuId", "PermissionId"] },
            new() { ["IXRoleMenuAccess_RoleIdMenuId"] = ["RoleId", "MenuId"] },
            [], [])
    };

    private sealed record SpecColumn(string Name, string SqlType, bool IsIdentity, bool IsNullable, int? DefaultValue);

    private static IReadOnlyList<SpecColumn> SpecColumns(string table) =>
        SpecificationWorkbook.Rows(table)
            .Select(row =>
            {
                var type = row[1].Trim().ToLowerInvariant();
                var identity = type.Contains("identity", StringComparison.Ordinal);
                type = Regex.Replace(type, @"\s+identity\(.*?\)", "");
                var rule = row[3];
                var defaultMatch = Regex.Match(rule, @"DEFAULT\s*\((\d+)\)", RegexOptions.IgnoreCase);

                return new SpecColumn(
                    row[0].Trim(),
                    type,
                    identity,
                    string.Equals(row[2].Trim(), "Yes", StringComparison.OrdinalIgnoreCase),
                    defaultMatch.Success ? int.Parse(defaultMatch.Groups[1].Value) : null);
            })
            .ToList();

    private static IEntityType Entity(string table) =>
        Model.Value.GetEntityTypes().Single(e => e.GetTableName() == table);

    private static StoreObjectIdentifier Store(IEntityType entity) =>
        StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());

    #region Model: tables

    [Fact]
    public void The_workbook_defines_the_nine_Step_0_tables()
    {
        Assert.All(Tables, table => Assert.NotEmpty(SpecColumns(table)));
        Assert.Equal(9, Tables.Length);
    }

    [Fact]
    public void The_model_has_exactly_the_nine_Step_0_tables_in_dbo()
    {
        var entities = Model.Value.GetEntityTypes().ToList();

        Assert.Equal(Tables.Order(StringComparer.Ordinal), entities.Select(e => e.GetTableName()!).Order(StringComparer.Ordinal));
        Assert.Equal(9, entities.Count);                       // no shadow / join entity types
        Assert.All(entities, e => Assert.Equal("dbo", e.GetSchema()));
    }

    [Fact]
    public void The_deferred_tables_UserTerritory_and_UserHierarchy_are_not_in_the_model()
    {
        // Spec 4.1 / D-27: Steps 11-12.
        Assert.DoesNotContain(Model.Value.GetEntityTypes(), e => e.GetTableName() is "UserTerritory" or "UserHierarchy");
    }

    [Fact]
    public void No_entity_has_foreign_keys_or_navigations()
    {
        Assert.All(Model.Value.GetEntityTypes(), entity =>
        {
            Assert.Empty(entity.GetForeignKeys());
            Assert.Empty(entity.GetNavigations());
            Assert.Empty(entity.GetSkipNavigations());
        });
    }

    #endregion

    #region Model: columns

    [Theory]
    [MemberData(nameof(TableNames))]
    public void Columns_match_the_workbook_exactly(string table)
    {
        var entity = Entity(table);
        var store = Store(entity);
        var spec = SpecColumns(table);

        var properties = entity.GetProperties().ToDictionary(p => p.GetColumnName(store)!, StringComparer.Ordinal);

        // Same set of columns (no extra, none missing).
        Assert.Equal(spec.Select(c => c.Name).Order(StringComparer.Ordinal), properties.Keys.Order(StringComparer.Ordinal));

        foreach (var column in spec)
        {
            var property = properties[column.Name];
            var where = $"{table}.{column.Name}";

            Assert.True(NormalizeType(property.GetColumnType(store)) == column.SqlType,
                $"{where}: model type '{property.GetColumnType(store)}', workbook '{column.SqlType}'");
            Assert.True(property.IsColumnNullable(store) == column.IsNullable,
                $"{where}: model nullable {property.IsColumnNullable(store)}, workbook {column.IsNullable}");

            var identity = property.GetValueGenerationStrategy(store) == SqlServerValueGenerationStrategy.IdentityColumn;
            Assert.True(identity == column.IsIdentity, $"{where}: identity {identity}, workbook {column.IsIdentity}");
            if (column.IsIdentity)
            {
                Assert.Equal(1L, property.GetIdentitySeed(store));
                Assert.Equal(1, property.GetIdentityIncrement(store));
            }

            var defaultValue = HasDefault(property) ? property.GetDefaultValue(store) : null;
            if (column.DefaultValue is { } expectedDefault)
            {
                Assert.True(defaultValue is not null && Convert.ToInt32(defaultValue) == expectedDefault,
                    $"{where}: default '{defaultValue}', workbook {expectedDefault}");
            }
            else
            {
                Assert.True(!HasDefault(property),
                    $"{where}: the workbook defines no DEFAULT but the model has one"); // e.g. CreatedDate: spec silent (Conventions 3.3 note 4)
            }
        }
    }

    [Fact]
    public void Primary_key_is_the_Id_column_of_every_table()
    {
        Assert.All(Tables, table =>
        {
            var key = Entity(table).FindPrimaryKey()!;
            Assert.Equal(["Id"], key.Properties.Select(p => p.Name));
        });
    }

    [Fact]
    public void Module01FieldLengths_equal_the_workbook_lengths_and_are_all_used()
    {
        var map = new (string Table, string Column, string Constant)[]
        {
            ("AppUser", "Username", nameof(Module01FieldLengths.Username)),
            ("AppUser", "EncPassword", nameof(Module01FieldLengths.EncPassword)),
            ("AppUser", "PasswordNonce", nameof(Module01FieldLengths.PasswordNonce)),
            ("AppUser", "PasswordTag", nameof(Module01FieldLengths.PasswordTag)),
            ("AppUser", "KeyVersion", nameof(Module01FieldLengths.KeyVersion)),
            ("AppUser", "FirstName", nameof(Module01FieldLengths.FirstName)),
            ("AppUser", "LastName", nameof(Module01FieldLengths.LastName)),
            ("AppUser", "Email", nameof(Module01FieldLengths.Email)),
            ("AppUser", "MobileNo", nameof(Module01FieldLengths.MobileNo)),
            ("Role", "Name", nameof(Module01FieldLengths.RoleName)),
            ("Role", "Description", nameof(Module01FieldLengths.RoleDescription)),
            ("Permission", "Name", nameof(Module01FieldLengths.PermissionName)),
            ("Permission", "Code", nameof(Module01FieldLengths.PermissionCode)),
            ("Permission", "Description", nameof(Module01FieldLengths.PermissionDescription)),
            ("Menu", "Name", nameof(Module01FieldLengths.MenuName)),
            ("Menu", "Code", nameof(Module01FieldLengths.MenuCode)),
            ("Menu", "Icon", nameof(Module01FieldLengths.MenuIcon)),
            ("Menu", "Route", nameof(Module01FieldLengths.MenuRoute)),
            ("UserSession", "TokenHash", nameof(Module01FieldLengths.TokenHash)),
            ("UserSession", "IPAddress", nameof(Module01FieldLengths.IPAddress)),
            ("UserSession", "DeviceInfo", nameof(Module01FieldLengths.DeviceInfo)),
            ("UserSession", "RevokedReason", nameof(Module01FieldLengths.RevokedReason)),
            ("LoginHistory", "Username", nameof(Module01FieldLengths.Username)),
            ("LoginHistory", "IPAddress", nameof(Module01FieldLengths.IPAddress)),
            ("LoginHistory", "DeviceInfo", nameof(Module01FieldLengths.DeviceInfo)),
            ("LoginHistory", "FailureReason", nameof(Module01FieldLengths.FailureReason))
        };

        foreach (var (table, column, constant) in map)
        {
            var type = SpecColumns(table).Single(c => c.Name == column).SqlType;
            var length = int.Parse(Regex.Match(type, @"\((\d+)\)").Groups[1].Value);
            var value = (int)typeof(Module01FieldLengths).GetField(constant)!.GetRawConstantValue()!;

            Assert.True(length == value, $"Module01FieldLengths.{constant} = {value}, workbook {table}.{column} = {type}");
        }

        // Every constant is checked against the workbook by the map above.
        var allConstants = typeof(Module01FieldLengths)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => f.Name)
            .Order(StringComparer.Ordinal);
        Assert.Equal(allConstants, map.Select(m => m.Constant).Distinct().Order(StringComparer.Ordinal));
    }

    #endregion

    #region Model: object names (spec 4.2)

    [Fact]
    public void Expected_objects_cover_the_nine_tables_and_the_Module01ObjectNameTests_names()
    {
        Assert.Equal(Tables.Order(StringComparer.Ordinal), Expected.Keys.Order(StringComparer.Ordinal));

        // Spec 4.2 / Database_Conventions.md 3.3 without the deferred tables: 9 PK, 9 UQ, 3 IX, 12 DF, 2 CK.
        Assert.Equal(9, Expected.Values.Count(e => e.PrimaryKey.Length > 0));
        Assert.Equal(9, Expected.Values.Sum(e => e.Unique.Count));
        Assert.Equal(3, Expected.Values.Sum(e => e.Indexes.Count));
        Assert.Equal(12, Expected.Values.Sum(e => e.Defaults.Count));
        Assert.Equal(2, Expected.Values.Sum(e => e.Checks.Length));
    }

    [Theory]
    [MemberData(nameof(TableNames))]
    public void Keys_constraints_and_indexes_have_exactly_the_spec_4_2_names(string table)
    {
        var entity = Entity(table);
        var store = Store(entity);
        var expected = Expected[table];

        Assert.Equal(expected.PrimaryKey, entity.FindPrimaryKey()!.GetName(store));

        // Unique constraints: alternate keys and unique indexes, with their exact columns.
        var unique = entity.GetKeys().Where(k => !k.IsPrimaryKey()).Select(k => (Name: k.GetName(store)!, Columns: k.Properties.Select(p => p.Name).ToArray()))
            .Concat(entity.GetIndexes().Where(i => i.IsUnique).Select(i => (Name: i.GetDatabaseName(store)!, Columns: i.Properties.Select(p => p.Name).ToArray())))
            .ToList();
        Assert.Equal(expected.Unique.Keys.Order(StringComparer.Ordinal), unique.Select(u => u.Name).Order(StringComparer.Ordinal));
        foreach (var (name, columns) in unique)
            Assert.Equal(expected.Unique[name], columns);

        // Non-unique indexes.
        var indexes = entity.GetIndexes().Where(i => !i.IsUnique).ToList();
        Assert.Equal(expected.Indexes.Keys.Order(StringComparer.Ordinal), indexes.Select(i => i.GetDatabaseName(store)!).Order(StringComparer.Ordinal));
        foreach (var index in indexes)
            Assert.Equal(expected.Indexes[index.GetDatabaseName(store)!], index.Properties.Select(p => p.Name));

        // DEFAULT constraints: name, column and value.
        var defaults = entity.GetProperties().Where(HasDefault).ToList();
        Assert.Equal(expected.Defaults.Keys.Order(StringComparer.Ordinal), defaults.Select(p => p.GetDefaultConstraintName(store)!).Order(StringComparer.Ordinal));
        foreach (var property in defaults)
        {
            var (column, value) = expected.Defaults[property.GetDefaultConstraintName(store)!];
            Assert.Equal(column, property.Name);
            Assert.Equal(value, Convert.ToInt32(property.GetDefaultValue(store)));
        }

        // CHECK constraints (D-16).
        Assert.Equal(expected.Checks.Order(StringComparer.Ordinal), entity.GetCheckConstraints().Select(c => c.GetName(store)!).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void The_check_constraints_allow_exactly_the_proposed_code_lists()
    {
        // D-16: LoginHistory.EventType 1 Success, 2 Failed, 3 Logout; Menu.Platform 1 Web, 2 Mobile, 3 Both.
        foreach (var (table, column) in new[] { ("LoginHistory", "EventType"), ("Menu", "Platform") })
        {
            var check = Assert.Single(Entity(table).GetCheckConstraints());
            Assert.Contains($"[{column}]", check.Sql);
            Assert.Equal(["1", "2", "3"], Regex.Matches(check.Sql, @"\d+").Select(m => m.Value));
        }
    }

    [Fact]
    public void The_model_has_no_database_objects_beyond_the_spec_4_2_list()
    {
        var all = Expected.Values.SelectMany(e => new[] { e.PrimaryKey }
            .Concat(e.Unique.Keys).Concat(e.Indexes.Keys).Concat(e.Defaults.Keys).Concat(e.Checks)).ToHashSet(StringComparer.Ordinal);

        var inModel = ModelRuleChecker.ObjectNames(Model.Value).Select(o => o.Name!).ToList();

        Assert.Equal(all.Order(StringComparer.Ordinal), inModel.Order(StringComparer.Ordinal));
        Assert.Empty(Model.Value.GetSequences());
    }

    #endregion

    #region Migration

    private static string MigrationText()
    {
        var file = SolutionFiles.SourceFiles.Single(f =>
            f.IsMigration && f.RelativePath.EndsWith("_Module01AuthenticationFoundation.cs", StringComparison.Ordinal));
        return file.Text;
    }

    [Fact]
    public void The_migration_creates_exactly_the_nine_tables_in_dbo_and_nothing_else()
    {
        var text = MigrationText();
        var created = Regex.Matches(text, @"migrationBuilder\.CreateTable\(\s*name:\s*""(?<n>\w+)"",\s*schema:\s*""(?<s>\w+)""");

        Assert.Equal(Tables.Order(StringComparer.Ordinal), created.Select(m => m.Groups["n"].Value).Order(StringComparer.Ordinal));
        Assert.All(created, m => Assert.Equal("dbo", m.Groups["s"].Value));

        // Schema only: no foreign keys, raw SQL, data seeding or procedures (AD-04, AD-07).
        Assert.DoesNotContain("ForeignKey", text);
        Assert.DoesNotContain("migrationBuilder.Sql", text);
        Assert.DoesNotContain("InsertData", text);
        Assert.DoesNotContain("Procedure", text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(TableNames))]
    public void The_migration_columns_match_the_workbook(string table)
    {
        var text = MigrationText();
        var start = text.IndexOf($"CreateTable(\r\n                name: \"{table}\"", StringComparison.Ordinal);
        if (start < 0) start = text.IndexOf($"CreateTable(\n                name: \"{table}\"", StringComparison.Ordinal);
        Assert.True(start >= 0, $"CreateTable for {table} not found.");

        var end = text.IndexOf("migrationBuilder.", start + 10, StringComparison.Ordinal);
        var block = end < 0 ? text[start..] : text[start..end];

        var columns = Regex.Matches(block, @"(?<n>\w+) = table\.Column<[^>]+>\(type: ""(?<t>[^""]+)""[^)]*?nullable: (?<null>true|false)")
            .ToDictionary(m => m.Groups["n"].Value, m => (Type: NormalizeType(m.Groups["t"].Value), Nullable: m.Groups["null"].Value == "true"));
        var spec = SpecColumns(table);

        Assert.Equal(spec.Select(c => c.Name).Order(StringComparer.Ordinal), columns.Keys.Order(StringComparer.Ordinal));
        foreach (var column in spec)
        {
            Assert.True(columns[column.Name].Type == column.SqlType, $"{table}.{column.Name}: migration '{columns[column.Name].Type}', workbook '{column.SqlType}'");
            Assert.True(columns[column.Name].Nullable == column.IsNullable, $"{table}.{column.Name}: nullable differs from the workbook");
        }
    }

    [Fact]
    public void Every_spec_4_2_name_is_present_in_the_migration()
    {
        var text = MigrationText();
        var names = Expected.Values.SelectMany(e => new[] { e.PrimaryKey }
            .Concat(e.Unique.Keys).Concat(e.Indexes.Keys).Concat(e.Defaults.Keys).Concat(e.Checks));

        var missing = names.Where(n => !text.Contains($"\"{n}\"", StringComparison.Ordinal)).ToList();

        Assert.True(missing.Count == 0, $"Names missing from the migration: {string.Join(", ", missing)}");
    }

    #endregion

    #region Rules detect violations

    [Theory]
    [InlineData("INT", "int")]
    [InlineData("nvarchar(100)", "nvarchar(100)")]
    [InlineData("DATETIME2", "datetime2")]
    [InlineData("datetime2(7)", "datetime2")]
    [InlineData("VARBINARY(32)", "varbinary(32)")]
    public void Type_normalization_equates_workbook_and_EF_spellings(string raw, string expected)
    {
        Assert.Equal(expected, NormalizeType(raw));
    }

    [Fact]
    public void Type_comparison_distinguishes_lengths_and_types()
    {
        Assert.NotEqual(NormalizeType("nvarchar(100)"), NormalizeType("nvarchar(101)"));
        Assert.NotEqual(NormalizeType("varchar(100)"), NormalizeType("nvarchar(100)"));
        Assert.NotEqual(NormalizeType("int"), NormalizeType("bigint"));
    }

    [Fact]
    public void The_workbook_reader_returns_all_columns_and_empty_cells()
    {
        var rows = SpecificationWorkbook.Rows("AppUser");

        Assert.Equal(20, rows.Count);                     // 20 columns in the workbook sheet
        Assert.Equal("Id", rows[0][0]);
        Assert.Equal("INT IDENTITY(1,1)", rows[0][1]);
        Assert.Equal("Yes", rows.Single(r => r[0] == "LastName")[2]);
        Assert.Equal("", rows.Single(r => r[0] == "EncPassword")[3]);   // an empty rule cell is kept as an empty string
        Assert.Equal("DEFAULT (1)", rows.Single(r => r[0] == "IsActive")[3]);
    }

    #endregion

    private static bool HasDefault(IProperty property) =>
        property.FindAnnotation(RelationalAnnotationNames.DefaultValue) is not null
        || property.FindAnnotation(RelationalAnnotationNames.DefaultValueSql) is not null;

    private static string NormalizeType(string? type) =>
        Regex.Replace((type ?? "").Trim().ToLowerInvariant(), @"^datetime2\(\d+\)$", "datetime2");
}
