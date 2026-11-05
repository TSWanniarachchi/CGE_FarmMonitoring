using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using Dapper;
using FarmFMS.Application.Abstractions.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
 * Description  : Executes Stored Procedures with Dapper (connections from IDbConnectionFactory only).
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Infrastructure.Data;

/// <summary>
/// Dapper executor for Stored Procedures. See <see cref="IStoredProcedureExecutor"/> for the rules it enforces.
/// <para>
/// Without an active transaction each call opens its own connection and closes it when done
/// (pooled by SqlClient). With an active <see cref="IUnitOfWork"/> transaction the call runs on the
/// transaction's connection. Calls on one transaction must not run in parallel.
/// </para>
/// Parameter values are never logged.
/// </summary>
internal sealed class StoredProcedureExecutor : IStoredProcedureExecutor
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITransactionAccessor _transactionAccessor;
    private readonly ILogger<StoredProcedureExecutor> _logger;
    private readonly int _commandTimeoutSeconds;

    public StoredProcedureExecutor(
        IDbConnectionFactory connectionFactory,
        ITransactionAccessor transactionAccessor,
        IOptions<DatabaseOptions> options,
        ILogger<StoredProcedureExecutor> logger)
    {
        _connectionFactory = connectionFactory ??
            throw new ArgumentNullException(nameof(connectionFactory));
        _transactionAccessor = transactionAccessor ??
            throw new ArgumentNullException(nameof(transactionAccessor));
        _logger = logger ??
            throw new ArgumentNullException(nameof(logger));
        _commandTimeoutSeconds = options?.Value.CommandTimeoutSeconds ??
            throw new ArgumentNullException(nameof(options));
    }

    #region Read

    public Task<IReadOnlyList<T>> QueryAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return RunAsync<IReadOnlyList<T>>(
            procedureName,
            parameters,
            async (connection, command) => (await connection.QueryAsync<T>(command)).AsList(),
            cancellationToken);
    }

    public Task<T?> QuerySingleOrDefaultAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            procedureName,
            parameters,
            (connection, command) => connection.QuerySingleOrDefaultAsync<T>(command),
            cancellationToken);
    }

    public Task<(IReadOnlyList<T1> First, IReadOnlyList<T2> Second)> QueryMultipleAsync<T1, T2>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return QueryMultipleAsync<(IReadOnlyList<T1>, IReadOnlyList<T2>)>(
            procedureName,
            async grid =>
            {
                var first = (await grid.ReadAsync<T1>()).AsList();
                var second = (await grid.ReadAsync<T2>()).AsList();

                return (first, second);
            },
            parameters,
            cancellationToken);
    }

    public Task<TResult> QueryMultipleAsync<TResult>(
        string procedureName,
        Func<SqlMapper.GridReader, Task<TResult>> read,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(read);

        return RunAsync(
            procedureName,
            parameters,
            async (connection, command) =>
            {
                await using var grid = await connection.QueryMultipleAsync(command);
                return await read(grid);
            },
            cancellationToken);
    }

    #endregion

    #region Write

    public Task<int> ExecuteAsync(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            procedureName,
            parameters,
            (connection, command) => connection.ExecuteAsync(command),
            cancellationToken);
    }

    public async Task<int> ExecuteForNewIdAsync(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var dynamicParameters = WithOutputParameter(parameters, StoredProcedureOutputParameters.NewId);

        await ExecuteAsync(procedureName, dynamicParameters, cancellationToken);

        var newId = dynamicParameters.Get<int?>(StoredProcedureOutputParameters.NewId);
        if (newId is not > 0)
            throw new InvalidOperationException(
                $"Stored procedure {procedureName} did not set @{StoredProcedureOutputParameters.NewId} to a positive Id (AD-09).");

        return newId.Value;
    }

    public async Task<int> ExecuteForRowsAffectedAsync(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var dynamicParameters = WithOutputParameter(parameters, StoredProcedureOutputParameters.RowsAffected);

        await ExecuteAsync(procedureName, dynamicParameters, cancellationToken);

        var rowsAffected = dynamicParameters.Get<int?>(StoredProcedureOutputParameters.RowsAffected);
        if (rowsAffected is not >= 0)
            throw new InvalidOperationException(
                $"Stored procedure {procedureName} did not set @{StoredProcedureOutputParameters.RowsAffected} (AD-09).");

        return rowsAffected.Value;
    }

    public Task<T?> ExecuteScalarAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            procedureName,
            parameters,
            (connection, command) => connection.ExecuteScalarAsync<T>(command),
            cancellationToken);
    }

    #endregion

    #region Execution

    /// <summary>
    /// Validates the procedure name, builds the command, resolves the connection, runs the action
    /// and translates SQL Server errors.
    /// </summary>
    private async Task<TResult> RunAsync<TResult>(
        string procedureName,
        object? parameters,
        Func<DbConnection, CommandDefinition, Task<TResult>> action,
        CancellationToken cancellationToken)
    {
        StoredProcedureName.EnsureValid(procedureName);

        var transaction = _transactionAccessor.CurrentTransaction;
        var command = BuildCommand(procedureName, parameters, transaction, cancellationToken);

        DbConnection? ownedConnection = null;
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            var connection = transaction?.Connection
                ?? (ownedConnection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken));

            var result = await action(connection, command);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Executed stored procedure {ProcedureName} in {ElapsedMs:0.0} ms",
                    procedureName,
                    Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            }

            return result;
        }
        catch (SqlException ex)
        {
            var translated = SqlErrorTranslator.Translate(ex);

            if (SqlErrorTranslator.IsRegisteredBusinessError(ex.Number))
            {
                // Expected business outcome: no stack trace, and not the THROW text (it may contain data values).
                _logger.LogWarning(
                    "Stored procedure {ProcedureName} raised business error {SqlErrorNumber} ({ErrorCode})",
                    procedureName,
                    ex.Number,
                    translated.Error.Code);

                throw translated;
            }

            _logger.Log(
                translated.IsTransient ? LogLevel.Warning : LogLevel.Error,
                ex,
                "Stored procedure {ProcedureName} failed with SQL error {SqlErrorNumber} after {ElapsedMs:0.0} ms",
                procedureName,
                ex.Number,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

            throw translated;
        }
        finally
        {
            if (ownedConnection is not null)
                await ownedConnection.DisposeAsync();
        }
    }

    /// <summary>
    /// Returns DynamicParameters carrying the caller's parameters plus the reserved <c>INT OUTPUT</c> parameter.
    /// A DynamicParameters instance is used as-is (so the caller can also read its own output parameters);
    /// anything else (e.g. an anonymous object) is wrapped.
    /// </summary>
    private static DynamicParameters WithOutputParameter(object? parameters, string outputName)
    {
        var dynamicParameters = parameters as DynamicParameters;

        var alreadyUsed = dynamicParameters is not null
            ? dynamicParameters.ParameterNames.Contains(outputName, StringComparer.OrdinalIgnoreCase)
            : parameters?.GetType().GetProperty(outputName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) is not null;
        if (alreadyUsed)
            throw new ArgumentException(
                $"'{outputName}' is a reserved output parameter (AD-09); the executor adds it.", nameof(parameters));

        dynamicParameters ??= new DynamicParameters(parameters);
        dynamicParameters.Add(outputName, dbType: DbType.Int32, direction: ParameterDirection.Output);
        return dynamicParameters;
    }

    private CommandDefinition BuildCommand(
        string procedureName,
        object? parameters,
        DbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        return new CommandDefinition(
            commandText: procedureName,
            parameters: parameters,
            transaction: transaction,
            commandTimeout: _commandTimeoutSeconds,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
    }

    #endregion
}
