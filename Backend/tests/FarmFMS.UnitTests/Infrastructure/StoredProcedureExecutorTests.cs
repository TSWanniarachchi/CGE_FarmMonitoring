using System.Data;
using Dapper;
using FarmFMS.Infrastructure.Data;
using FarmFMS.UnitTests.Infrastructure.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FarmFMS.UnitTests.Infrastructure;

/// <summary>
/// Executor guard and output-parameter contract tests, run against in-memory fake commands. The live Dapper path
/// against SQL Server (real procedures, SqlException translation end to end) needs an integration test database
/// and is NOT covered yet (see Backend/CLAUDE.md, "Tests").
/// </summary>
public class StoredProcedureExecutorTests
{
    private readonly FakeConnectionFactory _factory = new();

    private StoredProcedureExecutor CreateExecutor() => new(
        _factory,
        new UnitOfWork(_factory, NullLogger<UnitOfWork>.Instance),
        Options.Create(new DatabaseOptions()),
        NullLogger<StoredProcedureExecutor>.Instance);

    public static TheoryData<string> InvalidCommands => new()
    {
        "SELECT * FROM AppUser",
        "UPDATE AppUser SET IsActive = 0",
        "dbo.ScrAppUser; DROP TABLE AppUser",
        "EXEC dbo.ScrAppUser",
        "ScrAppUser",            // not dbo-qualified
        "sales.ScrAppUser"       // other schema
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Invalid_commands_are_rejected_before_a_connection_is_opened(string commandText)
    {
        var executor = CreateExecutor();

        await Assert.ThrowsAsync<ArgumentException>(() => executor.QueryAsync<int>(commandText));
        await Assert.ThrowsAsync<ArgumentException>(() => executor.QuerySingleOrDefaultAsync<int>(commandText));
        await Assert.ThrowsAsync<ArgumentException>(() => executor.QueryMultipleAsync<int, int>(commandText));
        await Assert.ThrowsAsync<ArgumentException>(() => executor.ExecuteAsync(commandText));
        await Assert.ThrowsAsync<ArgumentException>(() => executor.ExecuteScalarAsync<int>(commandText));
        await Assert.ThrowsAsync<ArgumentException>(() => executor.ExecuteForNewIdAsync(commandText));
        await Assert.ThrowsAsync<ArgumentException>(() => executor.ExecuteForRowsAffectedAsync(commandText));

        Assert.Empty(_factory.Created);
    }

    #region Output-parameter contract (AD-09)

    private const string InsertProcedure = "dbo.ScrSample";
    private const string UpdateProcedure = "dbo.SupSample";

    private static Func<FakeDbCommand, int> SetsOutput(string name, object? value) => command =>
    {
        command.Parameter(name).Value = value;
        return -1; // SET NOCOUNT ON
    };

    [Fact]
    public async Task ExecuteForNewIdAsync_returns_the_NewId_output_parameter()
    {
        _factory.CommandHandler = SetsOutput(StoredProcedureOutputParameters.NewId, 42);

        var newId = await CreateExecutor().ExecuteForNewIdAsync(InsertProcedure, new { Name = "Sample" });

        Assert.Equal(42, newId);
        var command = Assert.Single(Assert.Single(_factory.Created).ExecutedCommands);
        Assert.Equal(CommandType.StoredProcedure, command.CommandType);
        Assert.Equal(InsertProcedure, command.CommandText);
        Assert.Equal("Sample", command.Parameter("Name").Value);
        var output = command.Parameter(StoredProcedureOutputParameters.NewId);
        Assert.Equal(ParameterDirection.Output, output.Direction);
        Assert.Equal(DbType.Int32, output.DbType);
    }

    [Fact]
    public async Task ExecuteForNewIdAsync_accepts_DynamicParameters()
    {
        _factory.CommandHandler = SetsOutput(StoredProcedureOutputParameters.NewId, 7);
        var parameters = new DynamicParameters();
        parameters.Add("Name", "Sample", DbType.String);

        var newId = await CreateExecutor().ExecuteForNewIdAsync(InsertProcedure, parameters);

        Assert.Equal(7, newId);
        Assert.Equal(7, parameters.Get<int>(StoredProcedureOutputParameters.NewId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ExecuteForNewIdAsync_fails_when_the_procedure_does_not_set_a_positive_NewId(int? value)
    {
        _factory.CommandHandler = SetsOutput(StoredProcedureOutputParameters.NewId, value is null ? DBNull.Value : value);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateExecutor().ExecuteForNewIdAsync(InsertProcedure));

        Assert.Contains("@NewId", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(25)]
    public async Task ExecuteForRowsAffectedAsync_returns_the_RowsAffected_output_parameter(int rows)
    {
        _factory.CommandHandler = SetsOutput(StoredProcedureOutputParameters.RowsAffected, rows);

        var rowsAffected = await CreateExecutor().ExecuteForRowsAffectedAsync(UpdateProcedure, new { Id = 3 });

        Assert.Equal(rows, rowsAffected);
        var command = Assert.Single(Assert.Single(_factory.Created).ExecutedCommands);
        Assert.Equal(UpdateProcedure, command.CommandText);
        Assert.Equal(ParameterDirection.Output, command.Parameter(StoredProcedureOutputParameters.RowsAffected).Direction);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    public async Task ExecuteForRowsAffectedAsync_fails_when_the_procedure_does_not_set_RowsAffected(int? value)
    {
        _factory.CommandHandler = SetsOutput(StoredProcedureOutputParameters.RowsAffected, value is null ? DBNull.Value : value);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateExecutor().ExecuteForRowsAffectedAsync(UpdateProcedure));

        Assert.Contains("@RowsAffected", exception.Message);
    }

    [Fact]
    public async Task Reserved_output_parameter_names_are_rejected_before_a_connection_is_opened()
    {
        var executor = CreateExecutor();
        var dynamicParameters = new DynamicParameters();
        dynamicParameters.Add("rowsaffected", 1);

        await Assert.ThrowsAsync<ArgumentException>(() => executor.ExecuteForNewIdAsync(InsertProcedure, new { NewId = 5 }));
        await Assert.ThrowsAsync<ArgumentException>(() => executor.ExecuteForRowsAffectedAsync(UpdateProcedure, dynamicParameters));

        Assert.Empty(_factory.Created);
    }

    [Fact]
    public async Task Output_contract_calls_run_in_the_active_unit_of_work_transaction()
    {
        _factory.CommandHandler = SetsOutput(StoredProcedureOutputParameters.NewId, 1);
        var unitOfWork = new UnitOfWork(_factory, NullLogger<UnitOfWork>.Instance);
        var executor = new StoredProcedureExecutor(
            _factory, unitOfWork, Options.Create(new DatabaseOptions()), NullLogger<StoredProcedureExecutor>.Instance);

        await unitOfWork.BeginTransactionAsync();
        await executor.ExecuteForNewIdAsync(InsertProcedure);

        var connection = Assert.Single(_factory.Created);
        var command = Assert.Single(connection.ExecutedCommands);
        Assert.Same(connection.LastTransaction, command.Transaction);
        await unitOfWork.RollbackAsync();
    }

    #endregion
}
