using FarmFMS.ArchitectureTests.Support;

/*
 * Description  : Verifies the Simple Clean Architecture dependency rules between the Backend projects.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.ArchitectureTests;

/// <summary>
/// Domain -> nothing; Application -> Domain; Infrastructure -> Application, Domain; API -> Application, Infrastructure.
/// Checked both in the project files and in the compiled assemblies.
/// </summary>
public class LayerDependencyTests
{
    private static readonly string[] DataAccessPackagePrefixes =
        ["Dapper", "Microsoft.EntityFrameworkCore", "Microsoft.Data.SqlClient", "System.Data.SqlClient"];

    public static TheoryData<string, string[]> AllowedProjectReferences => new()
    {
        { "FarmFMS.Domain", [] },
        { "FarmFMS.Application", ["FarmFMS.Domain"] },
        { "FarmFMS.Infrastructure", ["FarmFMS.Application", "FarmFMS.Domain"] },
        { "FarmFMS.API", ["FarmFMS.Application", "FarmFMS.Infrastructure"] }
    };

    [Theory]
    [MemberData(nameof(AllowedProjectReferences))]
    public void Project_references_only_allowed_layers(string project, string[] allowed)
    {
        var references = SolutionFiles.ProjectReferences(project);

        var forbidden = references.Except(allowed, StringComparer.OrdinalIgnoreCase).ToList();

        Assert.True(forbidden.Count == 0,
            $"{project} must not reference: {string.Join(", ", forbidden)}");
    }

    [Theory]
    [MemberData(nameof(AllowedProjectReferences))]
    public void Assembly_references_only_allowed_layers(string project, string[] allowed)
    {
        var referencedLayers = SolutionFiles.LoadLayer(project)
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => name.StartsWith("FarmFMS.", StringComparison.Ordinal));

        var forbidden = referencedLayers.Except(allowed, StringComparer.Ordinal).ToList();

        Assert.True(forbidden.Count == 0,
            $"{project} assembly must not reference: {string.Join(", ", forbidden)}");
    }

    [Fact]
    public void Domain_has_no_package_references()
    {
        Assert.Empty(SolutionFiles.PackageReferences("FarmFMS.Domain"));
    }

    [Theory]
    [InlineData("FarmFMS.Domain")]
    [InlineData("FarmFMS.Application")]
    public void Inner_layers_have_no_data_access_packages(string project)
    {
        var forbidden = SolutionFiles.PackageReferences(project)
            .Where(IsDataAccessPackage)
            .ToList();

        Assert.True(forbidden.Count == 0,
            $"{project} must not reference data-access packages: {string.Join(", ", forbidden)}");
    }

    [Theory]
    [InlineData("FarmFMS.Domain")]
    [InlineData("FarmFMS.Application")]
    public void Inner_layer_assemblies_do_not_use_data_access_libraries(string project)
    {
        var forbidden = SolutionFiles.LoadLayer(project)
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(IsDataAccessPackage)
            .ToList();

        Assert.True(forbidden.Count == 0,
            $"{project} must not use: {string.Join(", ", forbidden)}");
    }

    [Fact]
    public void Api_does_not_reference_Dapper_or_SqlClient_directly()
    {
        var packages = SolutionFiles.PackageReferences("FarmFMS.API")
            .Where(p => p.StartsWith("Dapper", StringComparison.OrdinalIgnoreCase)
                     || p.Contains("SqlClient", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var assemblies = SolutionFiles.LoadLayer("FarmFMS.API")
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n is "Dapper" or "Microsoft.Data.SqlClient")
            .ToList();

        Assert.True(packages.Count == 0 && assemblies.Count == 0,
            $"Data access belongs to Infrastructure. API references: {string.Join(", ", packages.Concat(assemblies))}");
    }

    private static bool IsDataAccessPackage(string name) =>
        DataAccessPackagePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}
