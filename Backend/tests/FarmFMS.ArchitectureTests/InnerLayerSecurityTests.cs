using FarmFMS.ArchitectureTests.Support;

/*
 * Description  : Step 0 (spec 10.2): Domain and Application reference no JWT / token libraries and no crypto implementation.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.ArchitectureTests;

/// <summary>
/// Module 01 Step 0 specification section 10.2, AD-02 (Application owns interfaces only). Checked three ways: project
/// package references, compiled assembly references, and a source scan for crypto types. Each rule has a known-bad
/// detector test at the bottom.
/// </summary>
public class InnerLayerSecurityTests
{
    private static readonly string[] InnerLayers = ["FarmFMS.Domain", "FarmFMS.Application"];

    public static TheoryData<string> Layers => new(InnerLayers);

    [Fact]
    public void Inner_layer_sources_are_found()
    {
        // Guards against a vacuous pass of the source scans below.
        foreach (var layer in InnerLayers)
            Assert.Contains(SolutionFiles.SourceFiles, f => f.IsUnder(layer));

        Assert.Contains(SolutionFiles.SourceFiles, f => f.IsUnder("FarmFMS.Application") && f.RelativePath.Contains("/Module01/"));
    }

    [Theory]
    [MemberData(nameof(Layers))]
    public void Package_references_exclude_JWT_identity_token_and_crypto_libraries(string project)
    {
        var forbidden = SolutionFiles.PackageReferences(project).Where(InnerLayerSecurityRules.IsForbiddenReference).ToList();

        Assert.True(forbidden.Count == 0, $"{project} must not reference: {string.Join(", ", forbidden)}");
    }

    [Theory]
    [MemberData(nameof(Layers))]
    public void Assembly_references_exclude_JWT_identity_token_and_crypto_libraries(string project)
    {
        var forbidden = SolutionFiles.LoadLayer(project)
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(InnerLayerSecurityRules.IsForbiddenReference)
            .ToList();

        Assert.True(forbidden.Count == 0, $"{project} assembly must not reference: {string.Join(", ", forbidden)}");
    }

    [Theory]
    [MemberData(nameof(Layers))]
    public void Source_uses_no_crypto_implementation_types(string project)
    {
        var offenders = SolutionFiles.SourceFiles
            .Where(f => f.IsUnder(project))
            .SelectMany(f => InnerLayerSecurityRules.CheckSource(f.Text).Select(v => $"{f.RelativePath}: {v}"))
            .ToList();

        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void Infrastructure_is_where_the_crypto_and_token_implementations_live()
    {
        // Positive control: the scan finds the implementations where they belong, so a silent scanner failure is noticed.
        var infrastructure = SolutionFiles.SourceFiles.Where(f => f.IsUnder("FarmFMS.Infrastructure")).ToList();

        Assert.Contains(infrastructure, f => InnerLayerSecurityRules.CheckSource(f.Text).Contains("uses crypto type AesGcm"));
        Assert.Contains(infrastructure, f => InnerLayerSecurityRules.CheckSource(f.Text).Contains("uses crypto type SHA256"));
        Assert.Contains(
            SolutionFiles.PackageReferences("FarmFMS.Infrastructure"),
            p => p.StartsWith("Microsoft.IdentityModel", StringComparison.OrdinalIgnoreCase));
    }

    #region Rules detect violations

    [Theory]
    [InlineData("Microsoft.IdentityModel.JsonWebTokens")]
    [InlineData("Microsoft.IdentityModel.Tokens")]
    [InlineData("System.IdentityModel.Tokens.Jwt")]
    [InlineData("Microsoft.AspNetCore.Authentication.JwtBearer")]
    [InlineData("System.Security.Cryptography")]
    [InlineData("System.Security.Cryptography.Pkcs")]
    public void Reference_rule_detects_forbidden_libraries(string name)
    {
        Assert.True(InnerLayerSecurityRules.IsForbiddenReference(name));
    }

    [Theory]
    [InlineData("FarmFMS.Domain")]
    [InlineData("Microsoft.Extensions.Logging.Abstractions")]
    [InlineData("System.Runtime")]
    public void Reference_rule_accepts_ordinary_references(string name)
    {
        Assert.False(InnerLayerSecurityRules.IsForbiddenReference(name));
    }

    [Theory]
    [InlineData("using System.Security.Cryptography;\nclass A { }", "System.Security.Cryptography namespace")]
    [InlineData("class A { void M() { using var a = new AesGcm(key, 16); } }", "AesGcm")]
    [InlineData("class A { byte[] M() => RandomNumberGenerator.GetBytes(32); }", "RandomNumberGenerator")]
    [InlineData("class A { byte[] M(byte[] d) => SHA256.HashData(d); }", "SHA256")]
    [InlineData("class A { byte[] M(byte[] d) => System.Security.Cryptography.SHA256.HashData(d); }", "SHA256")]
    public void Source_rule_detects_crypto_usage(string source, string expectedFragment)
    {
        Assert.Contains(InnerLayerSecurityRules.CheckSource(source), v => v.Contains(expectedFragment));
    }

    [Fact]
    public void Source_rule_ignores_comments_string_literals_and_similar_names()
    {
        const string source = """
            // Uses AES-GCM through IPasswordProtector; SHA256 and AesGcm are Infrastructure details.
            /* RandomNumberGenerator is only mentioned here: System.Security.Cryptography */
            class A
            {
                const string Algorithm = "SHA256 AesGcm RandomNumberGenerator";
                string SHA256Hash = "x";          // identifier that merely starts with SHA256
                void SHA2561() { }
            }
            """;

        Assert.Empty(InnerLayerSecurityRules.CheckSource(source));
    }

    #endregion
}
