/*
 * Description  : Defines an application error (code, user-safe message, category and field errors).
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Application.Common.Results;

/// <summary>
/// An expected application error returned inside a <see cref="Result"/>.
/// <para>
/// <see cref="Message"/> is shown to the user, so it must never contain internal details
/// (SQL text, stack traces, passwords, keys or other sensitive values).
/// </para>
/// </summary>
public sealed class Error
{
    private static readonly IReadOnlyDictionary<string, string[]> NoValidationErrors =
        new Dictionary<string, string[]>();

    /// <summary>
    /// The "no error" value carried by a successful result.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public Error(
        string code,
        string message,
        ErrorType type,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(message);

        Code = code;
        Message = message;
        Type = type;
        ValidationErrors = validationErrors ?? NoValidationErrors;
    }

    #region Properties

    /// <summary>
    /// Stable, machine-readable code (e.g. "AppUser.NotFound"). Returned to clients as the ProblemDetails "code" extension.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// User-safe message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Error category; decides the HTTP status code.
    /// </summary>
    public ErrorType Type { get; }

    /// <summary>
    /// Field validation errors (field name -> messages). Empty unless <see cref="Type"/> is <see cref="ErrorType.Validation"/>.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

    #endregion

    #region Factories

    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);

    public static Error Validation(string code, string message, IReadOnlyDictionary<string, string[]>? validationErrors = null) =>
        new(code, message, ErrorType.Validation, validationErrors);

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static Error Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden);

    public static Error Unavailable(string code, string message) =>
        new(code, message, ErrorType.Unavailable);

    #endregion

    public override string ToString() => $"{Type}: {Code}";
}
