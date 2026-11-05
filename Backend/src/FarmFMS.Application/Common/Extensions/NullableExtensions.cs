using System.Runtime.CompilerServices;
using FarmFMS.Application.Common.Results;

/*
 * Description  : Turns missing required values into validation results instead of exceptions.
 * Author       : T.S.Wanniarachchi
 * Created Date : 05-11-2026
 */
namespace FarmFMS.Application.Common.Extensions;

/// <summary>
/// For user input, a missing required value is an expected outcome, so it becomes a <see cref="ErrorType.Validation"/>
/// result (HTTP 400 with the field name) rather than an exception. The field name is taken from the argument
/// expression: <c>request.FarmId.Required()</c> reports the field <c>FarmId</c>.
/// </summary>
public static class NullableExtensions
{
    public const string RequiredErrorCode = "Validation.Required";

    /// <summary>
    /// Success with the value, or a validation error when it is <c>null</c>.
    /// </summary>
    public static Result<T> Required<T>(
        this T? value,
        [CallerArgumentExpression(nameof(value))] string? fieldName = null)
        where T : struct
    {
        return value.HasValue ? value.Value : RequiredError(fieldName);
    }

    /// <summary>
    /// Success with the value, or a validation error when it is <c>null</c> (or, for strings, blank).
    /// </summary>
    public static Result<T> Required<T>(
        this T? value,
        [CallerArgumentExpression(nameof(value))] string? fieldName = null)
        where T : class
    {
        return value is null || (value is string text && string.IsNullOrWhiteSpace(text))
            ? RequiredError(fieldName)
            : value;
    }

    private static Error RequiredError(string? fieldExpression)
    {
        var field = FieldName(fieldExpression);
        var message = $"{field} is required.";

        return Error.Validation(
            RequiredErrorCode,
            message,
            new Dictionary<string, string[]> { [field] = [message] });
    }

    /// <summary>
    /// "request.FarmId" -> "FarmId"; "value" -> "value".
    /// </summary>
    private static string FieldName(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return "Value";

        var lastDot = expression.LastIndexOf('.');
        return lastDot >= 0 ? expression[(lastDot + 1)..].Trim() : expression.Trim();
    }
}
