using FarmFMS.API.Controllers.Module01;
using FarmFMS.ArchitectureTests.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/*
 * Description  : Step 0 (spec 10.2, AC-12): [AllowAnonymous] only on the allowlisted auth actions; secure by default.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.ArchitectureTests;

/// <summary>
/// AD-13, Step 0 specification 8.1 / 8.2, AC-12. The behavioural proof (a controller without [AllowAnonymous] returns 401)
/// is in the UnitTests API host tests; these tests fix the structure: the allowlist, the endpoint convention and the
/// health endpoints.
/// </summary>
public class AnonymousEndpointTests
{
    private const string ProgramFile = "FarmFMS.API/Program.cs";

    private static readonly string[] AnonymousAllowedFiles =
    [
        "FarmFMS.API/Controllers/Module01/AuthController.cs", // login, refresh, logout (allowlist checked by reflection)
        "FarmFMS.API/Common/Health/HealthEndpoints.cs"        // /health, /health/ready
    ];

    private static readonly System.Reflection.Assembly Api = typeof(AuthController).Assembly;

    [Fact]
    public void Controllers_are_found()
    {
        var controllers = AnonymousEndpointRules.Controllers(Api).ToList();

        Assert.Contains(typeof(AuthController), controllers);
        Assert.NotEmpty(AnonymousEndpointRules.Actions(typeof(AuthController)));
    }

    [Fact]
    public void AllowAnonymous_appears_only_on_the_allowlisted_actions()
    {
        var unexpected = AnonymousEndpointRules.UnexpectedAnonymous(AnonymousEndpointRules.Controllers(Api));

        Assert.True(unexpected.Count == 0, $"Anonymous endpoints outside the allowlist: {string.Join(", ", unexpected)}");
    }

    [Fact]
    public void Every_allowlisted_action_exists_is_anonymous_and_is_an_HTTP_action()
    {
        // Keeps the allowlist honest: a stale entry (renamed or secured action) is noticed.
        var anonymous = AnonymousEndpointRules.AnonymousEntries(AnonymousEndpointRules.Controllers(Api));
        Assert.Equal(AnonymousEndpointRules.AllowedAnonymousActions.Order(StringComparer.Ordinal), anonymous);

        foreach (var name in new[] { nameof(AuthController.Login), nameof(AuthController.Refresh), nameof(AuthController.Logout) })
        {
            var action = typeof(AuthController).GetMethod(name)!;
            Assert.NotNull(action);
            Assert.NotEmpty(action.GetCustomAttributes(typeof(HttpPostAttribute), inherit: true));
        }
    }

    [Fact]
    public void The_current_user_endpoint_is_not_anonymous()
    {
        var me = typeof(AuthController).GetMethod(nameof(AuthController.Me))!;

        Assert.Empty(me.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
        Assert.Empty(typeof(AuthController).GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
    }

    [Fact]
    public void Controllers_are_mapped_with_RequireAuthorization()
    {
        var program = Assert.Single(SolutionFiles.SourceFiles, f => f.RelativePath == ProgramFile);

        Assert.Empty(AnonymousEndpointRules.CheckControllerMapping(program.Text));
    }

    [Fact]
    public void AllowAnonymous_is_not_used_anywhere_else_in_the_API()
    {
        var apiFiles = SolutionFiles.SourceFiles.Where(f => f.IsUnder("FarmFMS.API")).ToList();
        Assert.Contains(apiFiles, f => f.RelativePath == ProgramFile); // not vacuous

        var offenders = apiFiles
            .SelectMany(f => AnonymousEndpointRules.CheckAnonymousUsage(f.RelativePath, f.Text, AnonymousAllowedFiles))
            .ToList();

        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void The_anonymous_allowlisted_files_really_use_AllowAnonymous()
    {
        // Positive control for the scan above.
        foreach (var path in AnonymousAllowedFiles)
        {
            var file = Assert.Single(SolutionFiles.SourceFiles, f => f.RelativePath == path);
            Assert.Contains("AllowAnonymous", file.Text);
        }
    }

    #region Rules detect violations

    [ApiController]
    private sealed class AnonymousActionController : ControllerBase
    {
        [HttpGet("a")]
        [AllowAnonymous]
        public IActionResult A() => Ok();

        [HttpGet("b")]
        public IActionResult B() => Ok();
    }

    [ApiController]
    [AllowAnonymous]
    private sealed class ClassLevelAnonymousController : ControllerBase
    {
        [HttpGet("a")]
        public IActionResult A() => Ok();
    }

    [ApiController]
    private sealed class SecuredController : ControllerBase
    {
        [HttpGet("a")]
        [Authorize]
        public IActionResult A() => Ok();
    }

    [Fact]
    public void Anonymous_rule_detects_class_level_and_action_level_AllowAnonymous()
    {
        Assert.Equal(
            [$"{nameof(ClassLevelAnonymousController)} (class)"],
            AnonymousEndpointRules.UnexpectedAnonymous([typeof(ClassLevelAnonymousController)]));
        Assert.Equal(
            [$"{nameof(AnonymousActionController)}.A"],
            AnonymousEndpointRules.UnexpectedAnonymous([typeof(AnonymousActionController)]));
        Assert.Empty(AnonymousEndpointRules.UnexpectedAnonymous([typeof(SecuredController)]));
    }

    [Theory]
    [InlineData("app.MapControllers();")]
    [InlineData("app.MapControllers() ;\napp.MapControllers().RequireAuthorization();")]
    [InlineData("// app.MapControllers().RequireAuthorization();")]
    [InlineData("")]
    public void Mapping_rule_detects_unsecured_or_missing_controller_mapping(string source)
    {
        Assert.NotEmpty(AnonymousEndpointRules.CheckControllerMapping(source));
    }

    [Theory]
    [InlineData("app.MapControllers().RequireAuthorization();")]
    [InlineData("app.MapControllers()\n    .RequireAuthorization();")]
    public void Mapping_rule_accepts_secured_controller_mapping(string source)
    {
        Assert.Empty(AnonymousEndpointRules.CheckControllerMapping(source));
    }

    [Theory]
    [InlineData("endpoints.MapGet(\"/x\", () => 1).AllowAnonymous();")]
    [InlineData("[AllowAnonymous] public class X { }")]
    [InlineData("[AllowAnonymousAttribute] public class X { }")]
    public void Usage_rule_detects_AllowAnonymous_outside_the_allowlist(string source)
    {
        Assert.NotEmpty(AnonymousEndpointRules.CheckAnonymousUsage("FarmFMS.API/Other.cs", source, AnonymousAllowedFiles));
        Assert.Empty(AnonymousEndpointRules.CheckAnonymousUsage(AnonymousAllowedFiles[1], source, AnonymousAllowedFiles));
    }

    [Fact]
    public void Usage_rule_ignores_comments()
    {
        Assert.Empty(AnonymousEndpointRules.CheckAnonymousUsage(
            "FarmFMS.API/Other.cs", "// AllowAnonymous is only for login\n/* AllowAnonymous */ class X { }", AnonymousAllowedFiles));
    }

    #endregion
}
