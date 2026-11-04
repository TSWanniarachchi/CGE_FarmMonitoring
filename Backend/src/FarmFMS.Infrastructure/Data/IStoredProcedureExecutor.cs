using Dapper;

/*
 * Description  : Defines the Dapper executor that runs Stored Procedures only.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Infrastructure.Data;

/// <summary>
/// Executes Stored Procedures with Dapper. Every repository uses this executor; nothing else runs SQL.
/// <list type="bullet">
/// <item>The command type is always <c>StoredProcedure</c>, and the name is validated against the PMS standard,
/// so inline SQL cannot be executed.</item>
/// <item>Connections come only from <c>IDbConnectionFactory</c>; calls enlist in the active <c>IUnitOfWork</c> transaction.</item>
/// <item>SQL Server errors are translated into <c>DataAccessException</c> with a user-safe message.</item>
/// </list>
/// Parameters are passed as <see cref="DynamicParameters"/> (preferred, supports output parameters
/// and explicit types) or an anonymous object.
/// </summary>
public interface IStoredProcedureExecutor
{
    /// <summary>
    /// READ: list / search. Returns an empty list when there are no rows.
    /// </summary>
    Task<IReadOnlyList<T>> QueryAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// READ: get by ID / single row. Returns default when there is no row; throws when there is more than one.
    /// </summary>
    Task<T?> QuerySingleOrDefaultAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// READ: two result sets from one procedure (e.g. header + details, user + roles).
    /// </summary>
    Task<(IReadOnlyList<T1> First, IReadOnlyList<T2> Second)> QueryMultipleAsync<T1, T2>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// READ: any number of result sets. <paramref name="read"/> must read every result set before it returns.
    /// </summary>
    Task<TResult> QueryMultipleAsync<TResult>(
        string procedureName,
        Func<SqlMapper.GridReader, Task<TResult>> read,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// WRITE: command. Returns the number of affected rows as reported by SQL Server
    /// (-1 when the procedure uses SET NOCOUNT ON); prefer output parameters for results.
    /// </summary>
    Task<int> ExecuteAsync(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// WRITE / READ: first column of the first row (e.g. a new ID).
    /// </summary>
    Task<T?> ExecuteScalarAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default);
}
