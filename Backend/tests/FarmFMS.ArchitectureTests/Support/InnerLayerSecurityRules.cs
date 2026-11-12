using System.Text.RegularExpressions;

/*
 * Description  : Rules that keep JWT / identity-token libraries and cryptographic implementations out of Domain and Application.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.ArchitectureTests.Support;

/// <summary>
/// Module 01 Step 0 specification section 10.2 / AD-02: the Application layer owns the interfaces
/// (<c>IPasswordProtector</c>, <c>IAccessTokenIssuer</c>, <c>ISessionTokenFactory</c>); JWT, token-validation and
/// cryptographic implementations belong to Infrastructure (and JwtBearer wiring to the API).
/// </summary>
internal static partial class InnerLayerSecurityRules
{
    /// <summary>
    /// Package / assembly name prefixes that must not be referenced by Domain or Application.
    /// <c>System.Security.Cryptography</c> covers the BCL crypto implementations (AesGcm, SHA256, RandomNumberGenerator ...).
    /// </summary>
    public static readonly string[] ForbiddenReferencePrefixes =
    [
        "Microsoft.IdentityModel",
        "System.IdentityModel.Tokens.Jwt",
        "Microsoft.AspNetCore.Authentication.JwtBearer",
        "System.Security.Cryptography"
    ];

    /// <summary>
    /// Crypto implementation types that must not be used in Domain / Application source.
    /// </summary>
    public static readonly string[] ForbiddenCryptoTypes = ["AesGcm", "RandomNumberGenerator", "SHA256"];

    public static bool IsForbiddenReference(string name) =>
        ForbiddenReferencePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Violations in C# source text: use of a forbidden crypto type or of the <c>System.Security.Cryptography</c>
    /// namespace. Comments and string literals are ignored.
    /// </summary>
    public static IReadOnlyList<string> CheckSource(string source)
    {
        var code = StripLiteralsAndComments(source);
        var violations = new List<string>();

        foreach (var type in ForbiddenCryptoTypes)
        {
            if (Regex.IsMatch(code, $@"\b{type}\b", RegexOptions.CultureInvariant))
                violations.Add($"uses crypto type {type}");
        }

        if (CryptographyNamespaceRegex().IsMatch(code))
            violations.Add("uses the System.Security.Cryptography namespace");

        return violations;
    }

    private static string StripLiteralsAndComments(string source) =>
        StringLiteralRegex().Replace(LineCommentRegex().Replace(BlockCommentRegex().Replace(source, " "), " "), "\"\"");

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex BlockCommentRegex();

    [GeneratedRegex(@"//[^\r\n]*", RegexOptions.CultureInvariant)]
    private static partial Regex LineCommentRegex();

    [GeneratedRegex("\"(?:[^\"\\\r\n]|\\.)*\"", RegexOptions.CultureInvariant)]
    private static partial Regex StringLiteralRegex();

    [GeneratedRegex(@"System\s*\.\s*Security\s*\.\s*Cryptography", RegexOptions.CultureInvariant)]
    private static partial Regex CryptographyNamespaceRegex();
}
