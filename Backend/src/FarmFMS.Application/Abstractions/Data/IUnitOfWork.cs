using System.Data;
using FarmFMS.Application.Common.Results;

/*
 * Description  : Defines the unit of work that groups several Stored Procedure calls into one transaction.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Application.Abstractions.Data;

/// <summary>
/// Groups several Stored Procedure calls of one request into a single database transaction.
/// <para>
/// Most business operations should be atomic inside ONE Stored Procedure and need no unit of work.
/// Use it only when a use case must call several procedures that succeed or fail together.
/// While a transaction is active, every Stored Procedure call in the same request scope enlists in it.
/// Nested transactions are not supported.
/// </para>
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// True while a transaction is open.
    /// </summary>
    bool HasActiveTransaction { get; }

    /// <summary>
    /// Opens a connection and begins a transaction. Throws when a transaction is already active.
    /// </summary>
    Task BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the active transaction and releases the connection.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the active transaction (if any) and releases the connection.
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> in a transaction: commits when it returns a successful result,
    /// rolls back when it returns a failed result or throws.
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
        where TResult : Result;
}
