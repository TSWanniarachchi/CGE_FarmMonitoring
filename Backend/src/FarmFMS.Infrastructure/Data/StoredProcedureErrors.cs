using FarmFMS.Application.Common.Results;

/*
 * Description  : Central catalogue of the error numbers Stored Procedures raise with THROW (AD-09).
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Infrastructure.Data;

/// <summary>
/// Stored Procedure error contract (Architecture Decision AD-09, documented in
/// <c>Documentation/Database/Database_Conventions.md</c>):
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
    /// Add entries here, grouped by module, only when the raising Stored Procedure is approved.
    /// No business error numbers are registered yet (no Stored Procedures exist).
    /// </para>
    /// </summary>
    public static IReadOnlyDictionary<int, Error> Catalog { get; } = Build(
    [
        // Module 01 – Security & Access Management
        // (none yet)
    ]);

    /// <summary>
    /// True when <paramref name="errorNumber"/> is in the known business error range (50001-50999).
    /// </summary>
    public static bool IsBusinessErrorNumber(int errorNumber) =>
        errorNumber is >= FirstBusinessError and <= LastBusinessError;

    /// <summary>
    /// Builds and validates a catalogue: numbers in range and unique, codes unique, allowed types only, message present.
    /// Throws at type initialisation, so an invalid catalogue stops the API at startup.
    /// </summary>
    internal static IReadOnlyDictionary<int, Error> Build(IEnumerable<KeyValuePair<int, Error>> entries)
    {
        var catalog = new Dictionary<int, Error>();
        var codes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (number, error) in entries)
        {
            ArgumentNullException.ThrowIfNull(error);

            if (!IsBusinessErrorNumber(number))
                throw new InvalidOperationException(
                    $"Business error {number} is outside {FirstBusinessError}-{LastBusinessError}.");

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
