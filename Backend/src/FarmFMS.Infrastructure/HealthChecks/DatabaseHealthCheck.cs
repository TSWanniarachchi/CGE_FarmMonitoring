using FarmFMS.Application.Abstractions.Data;
using FarmFMS.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

/*
 * Description  : Readiness health check that verifies SQL Server connectivity by opening a connection.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Infrastructure.HealthChecks;

/// <summary>
/// Opens (and immediately disposes) a connection through <see cref="IDbConnectionFactory"/>.
/// No command or Stored Procedure is executed and no database object is touched.
/// The response never carries connection details or provider messages; failures are logged with the
/// translated error code and SQL Server error number only.
/// </summary>
internal sealed class DatabaseHealthCheck(
    IDbConnectionFactory connectionFactory,
    ILogger<DatabaseHealthCheck> logger) : IHealthCheck
{
    public const string Name = "database";

    private const string FailureDescription = "The database is not reachable.";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (SqlException exception)
        {
            var error = SqlErrorTranslator.Translate(exception).Error;
            logger.LogWarning(
                "Database readiness check failed: {ErrorCode} (SQL Server error {ErrorNumber}).",
                error.Code,
                exception.Number);

            return new HealthCheckResult(context.Registration.FailureStatus, FailureDescription);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // e.g. a malformed connection string. The message may echo configuration, so only the type is logged.
            logger.LogWarning("Database readiness check failed: {ExceptionType}.", exception.GetType().Name);

            return new HealthCheckResult(context.Registration.FailureStatus, FailureDescription);
        }
    }
}
