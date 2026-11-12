using FarmFMS.Application.Common.Results;

/*
 * Description  : Central catalogue of the error numbers Stored Procedures raise with THROW (AD-09).
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Infrastructure.Data;

/// <summary>
/// Stored Procedure error contract (Architecture Decision AD-09, documented in
/// <c>Documentation/06_Database/Database_Conventions.md</c>):
/// <list type="table">
/// <item><term>50000</term><description>Generic / unexpected database error -> HTTP 500, generic message.</description></item>
/// <item><term>50001-50999</term><description>Known business errors. Each number used by a procedure MUST be registered
/// in <see cref="Catalog"/> with a safe application message; an unregistered number -> HTTP 500, generic message.</description></item>
/// <item><term>other</term><description>Unknown -> HTTP 500, generic message.</description></item>
/// </list>
/// The message text written in <c>THROW</c> is never sent to API clients; clients only ever see the catalogue message.
/// </summary>
public static class StoredProcedureErrors
{
    /// <summary>
    /// Generic / unexpected error raised by a procedure.
    /// </summary>
    public const int GenericError = 50000;

    /// <summary>
    /// First known business error number.
    /// </summary>
    public const int FirstBusinessError = 50001;

    /// <summary>
    /// Last known business error number.
    /// </summary>
    public const int LastBusinessError = 50999;

    /// <summary>
    /// Error types a business error may use. Failure / Unavailable are reserved for generic and technical errors.
    /// </summary>
    public static readonly IReadOnlySet<ErrorType> AllowedTypes = new HashSet<ErrorType>
    {
        ErrorType.Validation,
        ErrorType.NotFound,
        ErrorType.Conflict,
        ErrorType.Unauthorized,
        ErrorType.Forbidden
    };

    /// <summary>
    /// Registered business errors (error number -> safe application error).
    /// <para>
    /// Add entries here, grouped by module, only when the raising Stored Procedure is approved. Each entry names its
    /// module range (<see cref="StoredProcedureErrorRange"/>), e.g.
    /// <c>new(StoredProcedureErrorRange.Module01, 50001, Error.Conflict("User.UsernameTaken", "..."))</c>.
    /// Module 01 Step 0 (authentication foundation) uses the reserved block 50001-50019
    /// (Step00_Authentication_Foundation_Spec section 5.4, D-28).
    /// </para>
    /// </summary>
    public static IReadOnlyDictionary<int, Error> Catalog { get; } = Build(
    [
        // Module 01 – Security & Access Management (50001-50099); Step 0 block 50001-50019
        new(StoredProcedureErrorRange.Module01, 50001,
            Error.Unauthorized("Auth.SessionInvalid", "Your session is no longer valid. Please sign in again.")),
        new(StoredProcedureErrorRange.Module01, 50002,
            Error.Unauthorized("Auth.UserNotActive", "Invalid username or password.")),
        new(StoredProcedureErrorRange.Module01, 50003,
            Error.Validation("LoginHistory.EventTypeInvalid", "The login event type is not valid.")),
        new(StoredProcedureErrorRange.Module01, 50004,
            Error.NotFound("User.NotFound", "The user was not found.")),
        new(StoredProcedureErrorRange.Module01, 50005,
            Error.Conflict("User.UsernameTaken", "The username is already in use.")),
        new(StoredProcedureErrorRange.Module01, 50006,
            Error.Conflict("UserAccount.AlreadyExists", "The user already has an account."))
    ]);

    /// <summary>
    /// True when <paramref name="errorNumber"/> is in the known business error range (50001-50999).
    /// </summary>
    public static bool IsBusinessErrorNumber(int errorNumber) =>
        errorNumber is >= FirstBusinessError and <= LastBusinessError;

    /// <summary>
    /// Builds and validates a catalogue:
    /// <list type="bullet">
    /// <item>each entry names an approved module range (AD-09) and its number falls inside that range;</item>
    /// <item>numbers and codes are unique, only allowed types are used, and a message is present.</item>
    /// </list>
    /// Throws at type initialisation, so an invalid catalogue stops the API at startup.
    /// </summary>
    internal static IReadOnlyDictionary<int, Error> Build(IEnumerable<StoredProcedureErrorEntry> entries)
    {
        var catalog = new Dictionary<int, Error>();
        var codes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            var (range, number, error) = entry;
            ArgumentNullException.ThrowIfNull(range);
            ArgumentNullException.ThrowIfNull(error);

            if (!StoredProcedureErrorRange.All.Contains(range))
                throw new InvalidOperationException(
                    $"Business error {number} is registered under '{range}', which is not an approved range (AD-09).");

            if (!range.Contains(number))
                throw new InvalidOperationException(
                    $"Business error {number} is outside the range allocated to {range} (AD-09).");

            if (!AllowedTypes.Contains(error.Type))
                throw new InvalidOperationException(
                    $"Business error {number} uses type {error.Type}; allowed: {string.Join(", ", AllowedTypes)}.");

            if (string.IsNullOrWhiteSpace(error.Code) || string.IsNullOrWhiteSpace(error.Message))
                throw new InvalidOperationException($"Business error {number} needs a code and a safe message.");

            if (!catalog.TryAdd(number, error))
                throw new InvalidOperationException($"Business error {number} is registered more than once.");

            if (!codes.Add(error.Code))
                throw new InvalidOperationException($"Business error code '{error.Code}' is registered more than once.");
        }

        return catalog;
    }
}
