using FarmFMS.Application.Common.Results;

/*
 * Description  : Defines the exception raised when a database operation fails (already translated from the provider error).
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Application.Common.Exceptions;

/// <summary>
/// Thrown by the Infrastructure layer when a Stored Procedure call fails.
/// The provider exception (e.g. SqlException) is translated into an <see cref="Results.Error"/>
/// with a user-safe message, so the Application and API layers never depend on the database provider.
/// </summary>
public sealed class DataAccessException : Exception
{
    public DataAccessException(
        Error error,
        int? databaseErrorNumber,
        bool isTransient,
        Exception? innerException = null)
        : base(error?.Message, innerException)
    {
        ArgumentNullException.ThrowIfNull(error);

        Error = error;
        DatabaseErrorNumber = databaseErrorNumber;
        IsTransient = isTransient;
    }

    /// <summary>
    /// The translated, user-safe error.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// The provider error number (SQL Server error number), when known. For logging only.
    /// </summary>
    public int? DatabaseErrorNumber { get; }

    /// <summary>
    /// True for temporary failures (timeout, deadlock, connection loss). Writes are never retried automatically.
    /// </summary>
    public bool IsTransient { get; }
}
