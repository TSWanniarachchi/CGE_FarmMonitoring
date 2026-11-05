using System.Globalization;
using System.Text;

/*
 * Description  : Null-safe normalisation helpers for user input and log-safe masking.
 * Author       : T.S.Wanniarachchi
 * Created Date : 05-11-2026
 */
namespace FarmFMS.Application.Common.Extensions;

/// <summary>
/// Input normalisation used by Application services before values reach a repository. Every helper accepts
/// <c>null</c>. Helpers that return <c>string?</c> return <c>null</c> for blank input, so "not provided" is
/// stored as <c>NULL</c> rather than an empty string.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Trimmed value, or <c>null</c> when null, empty or whitespace.
    /// </summary>
    public static string? TrimToNull(this string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>
    /// Trimmed value, or an empty string when null or whitespace.
    /// </summary>
    public static string TrimOrEmpty(this string? value) => value?.Trim() ?? string.Empty;

    /// <summary>
    /// Trimmed value with every run of whitespace (spaces, tabs, line breaks) replaced by one space;
    /// <c>null</c> when blank.
    /// </summary>
    public static string? CollapseWhitespace(this string? value)
    {
        var trimmed = value.TrimToNull();
        if (trimmed is null) return null;

        var builder = new StringBuilder(trimmed.Length);
        var previousWasWhitespace = false;
        foreach (var c in trimmed)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!previousWasWhitespace) builder.Append(' ');
                previousWasWhitespace = true;
            }
            else
            {
                builder.Append(c);
                previousWasWhitespace = false;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Code / short identifier: trimmed, whitespace removed, upper case (invariant); <c>null</c> when blank.
    /// </summary>
    public static string? ToCode(this string? value)
    {
        var trimmed = value.TrimToNull();
        if (trimmed is null) return null;

        var builder = new StringBuilder(trimmed.Length);
        foreach (var c in trimmed)
        {
            if (!char.IsWhiteSpace(c)) builder.Append(char.ToUpperInvariant(c));
        }

        return builder.ToString();
    }

    /// <summary>
    /// E-mail address: trimmed, lower case (invariant); <c>null</c> when blank. Format is not validated here.
    /// </summary>
    public static string? ToEmail(this string? value) => value.TrimToNull()?.ToLowerInvariant();

    /// <summary>
    /// Digits only (e.g. a phone number without spaces, dashes or '+'); <c>null</c> when there are no digits.
    /// Only ASCII digits 0-9 are kept.
    /// </summary>
    public static string? ToDigitsOnly(this string? value)
    {
        if (value is null) return null;

        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (char.IsAsciiDigit(c)) builder.Append(c);
        }

        return builder.Length == 0 ? null : builder.ToString();
    }

    /// <summary>
    /// Search text for a Stored Procedure filter parameter: whitespace collapsed; <c>null</c> (no filter) when blank.
    /// </summary>
    public static string? ToSearchTerm(this string? value) => value.CollapseWhitespace();

    /// <summary>
    /// Log-safe form: only the last <paramref name="visibleCharacters"/> characters are shown, the rest become '*'.
    /// Values not longer than twice the visible part are masked completely. Returns <c>null</c> for null.
    /// </summary>
    public static string? Mask(this string? value, int visibleCharacters = 4)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(visibleCharacters);
        if (value is null) return null;
        if (value.Length <= visibleCharacters * 2) return new string('*', value.Length);

        return string.Create(CultureInfo.InvariantCulture,
            $"{new string('*', value.Length - visibleCharacters)}{value[^visibleCharacters..]}");
    }
}
