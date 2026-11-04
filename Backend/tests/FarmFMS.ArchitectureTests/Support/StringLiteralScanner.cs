using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/*
 * Description  : Extracts C# string values (incl. multi-line and concatenated literals) with Roslyn, and detects SQL in them.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.ArchitectureTests.Support;

/// <summary>
/// Parses C# source with Roslyn and returns every string value, so SQL is found no matter how it is written:
/// regular, verbatim (@"..."), raw ("""..."""), interpolated ($"...", $@"...", $"""..."""), UTF-8 ("..."u8),
/// and concatenations of these with '+' across lines. Comments are ignored by construction.
/// Interpolation holes are replaced by "{0}". Whitespace (incl. new lines) is collapsed to single spaces.
/// <para>
/// Not detected: SQL assembled at run time from separate statements (e.g. StringBuilder.Append calls)
/// or read from files / resources.
/// </para>
/// </summary>
internal static partial class StringLiteralScanner
{
    public sealed record StringValue(int Line, string Text);

    public static IReadOnlyList<StringValue> Scan(string source)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var values = new List<StringValue>();

        foreach (var node in root.DescendantNodes())
        {
            if (!IsStringExpression(node) || IsPartOfLargerStringExpression(node)) continue;

            var line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            values.Add(new StringValue(line, WhitespaceRegex().Replace(Fold(node), " ").Trim()));
        }

        return values;
    }

    /// <summary>
    /// True when <paramref name="text"/> looks like a SQL statement.
    /// </summary>
    public static bool ContainsSql(string text) => SqlRegex().IsMatch(text);

    #region Syntax

    private static bool IsStringExpression(SyntaxNode node) => node switch
    {
        LiteralExpressionSyntax literal =>
            literal.IsKind(SyntaxKind.StringLiteralExpression) || literal.IsKind(SyntaxKind.Utf8StringLiteralExpression),
        InterpolatedStringExpressionSyntax => true,
        BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AddExpression) =>
            IsStringExpression(binary.Left) || IsStringExpression(binary.Right),
        ParenthesizedExpressionSyntax parenthesized => IsStringExpression(parenthesized.Expression),
        _ => false
    };

    private static bool IsPartOfLargerStringExpression(SyntaxNode node) =>
        node.Parent is ParenthesizedExpressionSyntax or BinaryExpressionSyntax
        && IsStringExpression(node.Parent);

    private static string Fold(SyntaxNode node) => node switch
    {
        LiteralExpressionSyntax literal => literal.Token.ValueText,
        InterpolatedStringExpressionSyntax interpolated => string.Concat(
            interpolated.Contents.Select(c => c is InterpolatedStringTextSyntax text ? text.TextToken.ValueText : "{0}")),
        BinaryExpressionSyntax binary => Fold(binary.Left) + Fold(binary.Right),
        ParenthesizedExpressionSyntax parenthesized => Fold(parenthesized.Expression),
        _ => "{0}"
    };

    #endregion

    #region Regex

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(
        @"\bSELECT\b.*\bFROM\b" +
        @"|\bSELECT\s+(\*|\d|@|TOP\b|DISTINCT\b)" +
        @"|\bINSERT\s+(INTO\s+)?\[?\w+\]?(\.\[?\w+\]?)?\s*(\(|VALUES\b|SELECT\b)" +
        @"|\bUPDATE\s+\S+\s+SET\b" +
        @"|\bDELETE\s+(FROM\b|\[?\w+\]?\s+WHERE\b)" +
        @"|\bMERGE\s+(INTO\s+)?\S+\s+(AS\s+\w+\s+)?USING\b" +
        @"|\bTRUNCATE\s+TABLE\b" +
        @"|\b(CREATE|ALTER|DROP)\s+(OR\s+ALTER\s+)?(TABLE|PROCEDURE|PROC|VIEW|FUNCTION|INDEX|TRIGGER|SCHEMA)\b" +
        @"|\bEXEC(UTE)?\s+\S",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SqlRegex();

    #endregion
}
