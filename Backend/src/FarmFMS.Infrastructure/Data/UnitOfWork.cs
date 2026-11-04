using System.Data;
using System.Data.Common;
using FarmFMS.Application.Abstractions.Data;
using FarmFMS.Application.Common.Results;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

/*
 * Description  : Manages one connection and transaction per request scope for multi-procedure operations.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Infrastructure.Data;

/// <summary>
/// Scoped unit of work. Opens a connection through <see cref="IDbConnectionFactory"/> only when a
/// transaction is started, and releases it on commit, rollback or scope disposal.
/// An uncommitted transaction is rolled back when the request scope ends.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork, ITransactionAccessor, IAsyncDisposable, IDisposable
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<UnitOfWork> _logger;

    private DbConnection? _connection;
    private DbTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(IDbConnectionFactory connectionFactory, ILogger<UnitOfWork> logger)
    {
        _connectionFactory = connectionFactory ??
            throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ??
            throw new ArgumentNullException(nameof(logger));
    }

    #region Properties

    public bool HasActiveTransaction => _transaction is not null;

    DbTransaction? ITransactionAccessor.CurrentTransaction => _transaction;

    #endregion

    #region Transaction

    /// <summary>
    /// Opens a connection and begins a transaction.
    /// </summary>
    public async Task BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is not null)
            throw new InvalidOperationException("A transaction is already active. Nested transactions are not supported.");

        try
        {
            _connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            _transaction = await _connection.BeginTransactionAsync(isolationLevel, cancellationToken);
        }
        catch (SqlException ex)
        {
            await ReleaseAsync();
            _logger.LogError(ex, "Failed to begin a database transaction (SQL error {SqlErrorNumber})", ex.Number);
            throw SqlErrorTranslator.Translate(ex);
        }
        catch
        {
            await ReleaseAsync();
            throw;
        }

        _logger.LogDebug("Database transaction started ({IsolationLevel})", isolationLevel);
    }

    /// <summary>
    /// Commits the active transaction and releases the connection.
    /// </summary>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is null)
            throw new InvalidOperationException("There is no active transaction to commit.");

        try
        {
            await _transaction.CommitAsync(cancellationToken);
            _logger.LogDebug("Database transaction committed");
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to commit the database transaction (SQL error {SqlErrorNumber})", ex.Number);
            throw SqlErrorTranslator.Translate(ex);
        }
        finally
        {
            await ReleaseAsync();
        }
    }

    /// <summary>
    /// Rolls back the active transaction (no-op when there is none) and releases the connection.
    /// The rollback itself is not cancellable, so the database is never left with an open transaction.
    /// </summary>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;

        try
        {
            await _transaction.RollbackAsync(CancellationToken.None);
            _logger.LogDebug("Database transaction rolled back");
        }
        catch (Exception ex)
        {
            // The connection is discarded below; SQL Server rolls back when the connection closes.
            _logger.LogError(ex, "Failed to roll back the database transaction");
        }
        finally
        {
            await ReleaseAsync();
        }
    }

    /// <summary>
    /// Runs the operation in a transaction: commits on a successful result, rolls back on a failed result or an exception.
    /// </summary>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
        where TResult : Result
    {
        ArgumentNullException.ThrowIfNull(operation);

        await BeginTransactionAsync(isolationLevel, cancellationToken);

        try
        {
            var result = await operation(cancellationToken);

            if (result.IsSuccess)
                await CommitAsync(cancellationToken);
            else
                await RollbackAsync(cancellationToken);

            return result;
        }
        catch
        {
            await RollbackAsync(cancellationToken);
            throw;
        }
    }

    #endregion

    #region Disposal

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_transaction is not null)
        {
            _logger.LogWarning("A database transaction was not committed before the request ended; it has been rolled back");
            await RollbackAsync();
        }

        await ReleaseAsync();
        _disposed = true;
    }

    /// <summary>
    /// Synchronous disposal for scopes that are not disposed asynchronously.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        if (_transaction is not null)
        {
            _logger.LogWarning("A database transaction was not committed before the request ended; it has been rolled back");

            try { _transaction.Rollback(); }
            catch (Exception ex) { _logger.LogError(ex, "Failed to roll back the database transaction"); }
        }

        _transaction?.Dispose();
        _transaction = null;
        _connection?.Dispose();
        _connection = null;
        _disposed = true;
    }

    private async ValueTask ReleaseAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    #endregion
}
