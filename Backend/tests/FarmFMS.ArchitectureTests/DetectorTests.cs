using FarmFMS.ArchitectureTests.Support;

/*
 * Description  : Proves that the inline-SQL scanner and the SQL-script rules detect violations.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.ArchitectureTests;

/// <summary>
/// The source and script rules pass on the real code only if they are able to fail: these tests feed
/// known-bad samples to the detectors.
/// </summary>
public class DetectorTests
{
    #region Inline SQL scanner

    public static TheoryData<string> SqlSources => new()
    {
        // Regular literal
        """var s = "SELECT Id FROM AppUser";""",
        // Verbatim literal split across lines
        "var s = @\"SELECT Id,\r\n    Username\r\nFROM AppUser\";",
        // Raw literal split across lines
        "var s = \"\"\"\n    UPDATE AppUser\n    SET IsActive = 0\n    \"\"\";",
        // Concatenation across lines
        "var s = \"DELETE \" +\n    \"FROM AppUser \" +\n    \"WHERE Id = 1\";",
        // Interpolated
        "var s = $\"INSERT INTO AppUser (Username) VALUES ('{name}')\";",
        // Interpolated verbatim split across lines with a hole
        "var s = $@\"SELECT *\n FROM {table}\";",
        // Interpolated raw
        "var s = $\"\"\"\n    EXEC dbo.ScrAppUser {id}\n    \"\"\";",
        // Concatenation with a variable in the middle
        "var s = \"SELECT Name \" + column + \" FROM Role\";",
        // Inside a method call argument
        "connection.Execute(\"TRUNCATE TABLE LoginHistory\");",
        // DDL
        "var s = @\"CREATE OR ALTER\nPROCEDURE dbo.SreAppUserList AS\";"
    };

    [Theory]
    [MemberData(nameof(SqlSources))]
    public void Scanner_detects_SQL(string statement)
    {
        var source = $"class C {{ void M(string name, string table, int id, string column, dynamic connection) {{ {statement} }} }}";

        Assert.Contains(StringLiteralScanner.Scan(source), s => StringLiteralScanner.ContainsSql(s.Text));
    }

    public static TheoryData<string> NonSqlSources => new()
    {
        "var s = \"Select a valid role.\";",
        "var s = \"Please update your profile.\";",
        "var s = \"Executed stored procedure {ProcedureName} in {ElapsedMs} ms\";",
        "var s = \"dbo.SreAppUserList\";",
        "// SELECT Id FROM AppUser (comment)\nvar s = \"ok\";",
        "/* DELETE FROM AppUser */ var s = \"ok\";",
        "var total = 1 + 2;"
    };

    [Theory]
    [MemberData(nameof(NonSqlSources))]
    public void Scanner_ignores_non_SQL_strings_and_comments(string statement)
    {
        var source = $"class C {{ void M() {{ {statement} }} }}";

        Assert.DoesNotContain(StringLiteralScanner.Scan(source), s => StringLiteralScanner.ContainsSql(s.Text));
    }

    [Fact]
    public void Scanner_reports_the_line_where_a_multi_line_string_starts()
    {
        const string source = "class C\n{\n    string S = @\"SELECT Id\n        FROM AppUser\";\n}";

        var value = Assert.Single(StringLiteralScanner.Scan(source));
        Assert.Equal(3, value.Line);
        Assert.Equal("SELECT Id FROM AppUser", value.Text);
    }

    #endregion

    #region SQL script rules

    [Fact]
    public void Script_rules_accept_a_valid_procedure_script()
    {
        const string sql = """
            -- Description : Reads the user list. Not a FOREIGN KEY: comments are ignored.
            CREATE OR ALTER PROCEDURE dbo.SreAppUserList
            AS
            BEGIN
                SET NOCOUNT ON;
            END
            """;

        Assert.Empty(SqlScriptRules.Check(sql));
    }

    [Theory]
    [InlineData("ALTER TABLE dbo.UserRole ADD CONSTRAINT CfkUsrRole FOREIGN KEY (UserId) REFERENCES dbo.AppUser (Id);")]
    [InlineData("CREATE TABLE dbo.X (Id INT, UserId INT REFERENCES dbo.AppUser(Id));")]
    [InlineData("CREATE PROCEDURE SreAppUserList AS SELECT 1;")]                  // not schema-qualified
    [InlineData("CREATE PROCEDURE sales.SreAppUserList AS SELECT 1;")]            // wrong schema
    [InlineData("CREATE PROC dbo.sp_GetUsers AS SELECT 1;")]                      // sp_ prefix
    [InlineData("CREATE OR ALTER PROCEDURE dbo.SreUserList AS SELECT 1;")]           // new name uses "User"
    public void Script_rules_detect_violations(string sql)
    {
        Assert.NotEmpty(SqlScriptRules.Check(sql));
    }

    [Fact]
    public void Script_rules_accept_long_descriptive_names_and_reject_names_over_the_SQL_Server_limit()
    {
        // No custom length limit: a long descriptive name is fine.
        Assert.Empty(SqlScriptRules.Check("CREATE OR ALTER PROCEDURE dbo.SreUsrTerritoryAssignmentHistory AS SELECT 1;"));

        // SQL Server's identifier limit is 128 characters.
        var tooLong = "Sre" + new string('X', 126);
        Assert.NotEmpty(SqlScriptRules.Check($"CREATE OR ALTER PROCEDURE dbo.{tooLong} AS SELECT 1;"));
    }

    #endregion
}
