using FarmFMS.Application.Common.Exceptions;
using FarmFMS.Application.Common.Results;
using Microsoft.Data.SqlClient;

/*
 * Description  : Translates SQL Server errors into user-safe application errors (AD-09).
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Infrastructure.Data;

/// <summary>
/// Translates a <see cref="SqlException"/> into a <see cref="DataAccessException"/> with a user-safe message.
/// <list type="bullet">
/// <item>50000 -> generic error (HTTP 500).</item>
/// <item>50001-50999 registered in <see cref="StoredProcedureErrors.Catalog"/> -> the catalogue error.</item>
/// <item>50001-50999 not registered -> generic error (HTTP 500).</item>
/// <item>Known SQL Server errors (duplicate key, deadlock, timeout, unavailable) -> specific safe errors.</item>
/// <item>Anything else -> generic error (HTTP 500).</item>
/// </list>
/// SQL text, parameter values and server / THROW messages are never placed in the translated message.
/// </summary>
internal static class SqlErrorTranslator
{
    #region SQL Server error numbers

    private const int Timeout = -2;
    private const int CannotOpenDatabase = 4060;
    private const int LoginFailed = 18456;
    private const int ConstraintViolation = 547;
    private const int Deadlock = 1205;
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    // Network / availability errors (connection lost, server not found, Azure SQL throttling / failover).
    private static readonly HashSet<int> UnavailableErrors = [53, 64, 233, 10053, 10054, 10060, 10928, 10929, 40197, 40501, 40613];

    #endregion

    /// <summary>
    /// The generic error returned for 50000, unregistered business errors and unknown errors.
    /// </summary>
    internal static readonly Error GenericError =
        Error.Failure("Database.Error", "A database error occurred. Please try again later.");

    public static DataAccessException Translate(SqlException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return Translate(exception.Number, exception);
    }

    /// <summary>
    /// Translates by SQL Server error number. <paramref name="businessErrors"/> defaults to
    /// <see cref="StoredProcedureErrors.Catalog"/> (overridable for tests).
    /// </summary>
    internal static DataAccessException Translate(
        int errorNumber,
        Exception? innerException,
        IReadOnlyDictionary<int, Error>? businessErrors = null)
    {
        businessErrors ??= StoredProcedureErrors.Catalog;

        if (StoredProcedureErrors.IsBusinessErrorNumber(errorNumber))
        {
            return businessErrors.TryGetValue(errorNumber, out var businessError)
                ? Create(businessError, errorNumber, isTransient: false, innerException)
                : Create(GenericError, errorNumber, isTransient: false, innerException);
        }

        return errorNumber switch
        {
            UniqueIndexViolation or UniqueConstraintViolation => Create(
                Error.Conflict("Database.Duplicate", "A record with the same unique value already exists."),
                errorNumber, isTransient: false, innerException),

            ConstraintViolation => Create(
                Error.Conflict("Database.ConstraintViolation", "The operation conflicts with a database rule."),
                errorNumber, isTransient: false, innerException),

            Deadlock => Create(
                Error.Unavailable("Database.Deadlock", "The operation could not be completed because of a temporary database conflict. Please try again."),
                errorNumber, isTransient: true, innerException),

            Timeout => Create(
                Error.Unavailable("Database.Timeout", "The database did not respond in time. Please try again."),
                errorNumber, isTransient: true, innerException),

            CannotOpenDatabase or LoginFailed => Create(
                Error.Unavailable("Database.Unavailable", "The database is not available. Please try again later."),
                errorNumber, isTransient: false, innerException),

            _ when UnavailableErrors.Contains(errorNumber) => Create(
                Error.Unavailable("Database.Unavailable", "The database is not available. Please try again later."),
                errorNumber, isTransient: true, innerException),

            // 50000 (generic procedure error), numbers above the business range and every unknown error.
            _ => Create(GenericError, errorNumber, isTransient: false, innerException)
        };
    }

    /// <summary>
    /// True when the number is a business error registered in the catalogue (an expected outcome, not a fault).
    /// </summary>
    internal static bool IsRegisteredBusinessError(int errorNumber) =>
        StoredProcedureErrors.Catalog.ContainsKey(errorNumber);

    private static DataAccessException Create(Error error, int errorNumber, bool isTransient, Exception? innerException) =>
        new(error, errorNumber, isTransient, innerException);
}
