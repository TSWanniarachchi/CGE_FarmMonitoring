using FarmFMS.ArchitectureTests.Support;

/*
 * Description  : Verifies the approved OpenAPI / Swagger UI package rule (AD-18 and its approved exception).
 * Author       : T.S.Wanniarachchi
 * Created Date : 05-11-2026
 */
namespace FarmFMS.ArchitectureTests;

/// <summary>
/// AD-18: the built-in ASP.NET Core generator (<c>Microsoft.AspNetCore.OpenApi</c>) is the only OpenAPI document
/// generator. Approved exception: <c>Swashbuckle.AspNetCore.SwaggerUI</c> provides Swagger UI only, in the API
/// project only. Any other Swashbuckle package (generator, annotations, meta-package) is not allowed.
/// </summary>
public class OpenApiPackageTests
{
    private const string BuiltInGenerator = "Microsoft.AspNetCore.OpenApi";
    private const string ApprovedSwaggerUi = "Swashbuckle.AspNetCore.SwaggerUI";

    private static bool IsSwashbuckle(string name) =>
        name.StartsWith("Swashbuckle", StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Api_uses_the_built_in_OpenAPI_generator()
    {
        Assert.Contains(BuiltInGenerator, SolutionFiles.PackageReferences("FarmFMS.API"));
    }

    [Fact]
    public void Api_references_only_the_approved_Swashbuckle_UI_package()
    {
        var packages = SolutionFiles.PackageReferences("FarmFMS.API").Where(IsSwashbuckle).ToList();

        var assemblies = SolutionFiles.LoadLayer("FarmFMS.API")
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(IsSwashbuckle)
            .ToList();

        Assert.Equal([ApprovedSwaggerUi], packages);
        Assert.All(assemblies, name => Assert.Equal(ApprovedSwaggerUi, name));
    }

    [Theory]
    [InlineData("FarmFMS.Domain")]
    [InlineData("FarmFMS.Application")]
    [InlineData("FarmFMS.Infrastructure")]
    public void Inner_layers_reference_no_Swashbuckle_package(string project)
    {
        Assert.DoesNotContain(SolutionFiles.PackageReferences(project), IsSwashbuckle);
    }
}
