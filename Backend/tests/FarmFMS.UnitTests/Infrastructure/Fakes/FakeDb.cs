using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using FarmFMS.Application.Abstractions.Data;

namespace FarmFMS.UnitTests.Infrastructure.Fakes;

/// <summary>
/// Connection factory that hands out <see cref="FakeDbConnection"/>s and records them.
/// </summary>
internal sealed class FakeConnectionFactory : IDbConnectionFactory
{
    public List<FakeDbConnection> Created { get; } = [];

    public Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new FakeDbConnection();
        Created.Add(connection);
        return Task.FromResult<DbConnection>(connection);
    }
}

/// <summary>
/// In-memory connection that supports transactions only (no commands).
/// </summary>
internal sealed class FakeDbConnection : DbConnection
{
    private ConnectionState _state = ConnectionState.Open;

    public FakeDbTransaction? LastTransaction { get; private set; }

    public bool IsDisposed { get; private set; }

    [AllowNull]
    public override string ConnectionString { get; set; } = string.Empty;

    public override string Database => "Fake";

    public override string DataSource => "Fake";

    public override string ServerVersion => "0";

    public override ConnectionState State => _state;

    public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();

    public override void Open() => _state = ConnectionState.Open;

    public override void Close() => _state = ConnectionState.Closed;

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        LastTransaction = new FakeDbTransaction(this, isolationLevel);
        return LastTransaction;
    }

    protected override DbCommand CreateDbCommand() => throw new NotSupportedException("Commands are not supported by the fake.");

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        _state = ConnectionState.Closed;
        base.Dispose(disposing);
    }
}

internal sealed class FakeDbTransaction(FakeDbConnection connection, IsolationLevel isolationLevel) : DbTransaction
{
    public bool IsCommitted { get; private set; }

    public bool IsRolledBack { get; private set; }

    public bool IsDisposed { get; private set; }

    public override IsolationLevel IsolationLevel { get; } = isolationLevel;

    protected override DbConnection DbConnection { get; } = connection;

    public override void Commit() => IsCommitted = true;

    public override void Rollback() => IsRolledBack = true;

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        base.Dispose(disposing);
    }
}
