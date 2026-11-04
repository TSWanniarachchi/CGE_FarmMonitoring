using FarmFMS.Infrastructure.Data;
using FarmFMS.UnitTests.Infrastructure.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FarmFMS.UnitTests.Infrastructure;

/// <summary>
/// Executor guard tests. The live Dapper path (real commands, SqlException translation end to end) needs a
/// SQL Server integration test database and is NOT covered yet (see Backend/CLAUDE.md, "Tests").
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

        Assert.Empty(_factory.Created);
    }
}
