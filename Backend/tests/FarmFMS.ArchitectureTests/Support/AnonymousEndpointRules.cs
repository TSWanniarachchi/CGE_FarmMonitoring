using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/*
 * Description  : Finds anonymous endpoints (controllers by reflection, route mapping by source scan) for the secure-by-default rule.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.ArchitectureTests.Support;

/// <summary>
/// AD-13 / Step 0 specification 8.1 and 8.2: every controller endpoint requires authentication unless it is on the allowlist
/// (login, refresh, logout). The convention is <c>MapControllers().RequireAuthorization()</c> in Program.cs.
/// </summary>
internal static partial class AnonymousEndpointRules
{
    /// <summary>
    /// The only controller actions that may be anonymous (spec 8.1, AD-13).
    /// </summary>
    public static readonly IReadOnlyList<string> AllowedAnonymousActions =
        ["AuthController.Login", "AuthController.Logout", "AuthController.Refresh"];

    public static IEnumerable<Type> Controllers(Assembly assembly) =>
        assembly.GetTypes().Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

    /// <summary>
    /// Public instance actions declared by a controller or its base classes in the same assembly.
    /// </summary>
    public static IEnumerable<MethodInfo> Actions(Type controller) =>
        controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.DeclaringType!.Assembly == controller.Assembly && !m.IsSpecialName);

    /// <summary>
    /// "Controller (class)" for a controller marked [AllowAnonymous], "Controller.Action" for each anonymous action.
    /// </summary>
    public static IReadOnlyList<string> AnonymousEntries(IEnumerable<Type> controllers) =>
        controllers
            .SelectMany(t => t.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) is not null
                ? [$"{t.Name} (class)"]
                : Actions(t)
                    .Where(m => m.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) is not null)
                    .Select(m => $"{t.Name}.{m.Name}"))
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Violations: anonymous entries that are not on the allowlist.
    /// </summary>
    public static IReadOnlyList<string> UnexpectedAnonymous(IEnumerable<Type> controllers) =>
        AnonymousEntries(controllers).Except(AllowedAnonymousActions, StringComparer.Ordinal).ToList();

    /// <summary>
    /// Violations in the text of Program.cs: every <c>MapControllers()</c> call must be followed by
    /// <c>.RequireAuthorization()</c>, and at least one call must exist.
    /// </summary>
    public static IReadOnlyList<string> CheckControllerMapping(string programSource)
    {
        var code = LineCommentRegex().Replace(BlockCommentRegex().Replace(programSource, " "), " ");
        var violations = new List<string>();

        var calls = MapControllersRegex().Matches(code);
        if (calls.Count == 0)
            violations.Add("MapControllers() is not called.");

        foreach (Match call in calls)
        {
            if (!call.Groups["secured"].Success)
                violations.Add("MapControllers() is not followed by .RequireAuthorization().");
        }

        return violations;
    }

    /// <summary>
    /// Violations in other API source: an [AllowAnonymous] attribute or .AllowAnonymous() call outside the allowlisted files
    /// (the controller allowlist is checked by reflection; the health endpoints are anonymous by design).
    /// </summary>
    public static IReadOnlyList<string> CheckAnonymousUsage(string relativePath, string source, IReadOnlyCollection<string> allowedFiles)
    {
        if (allowedFiles.Contains(relativePath, StringComparer.OrdinalIgnoreCase)) return [];

        var code = LineCommentRegex().Replace(BlockCommentRegex().Replace(source, " "), " ");

        return AnonymousUsageRegex().IsMatch(code)
            ? [$"{relativePath} uses AllowAnonymous but is not on the anonymous allowlist."]
            : [];
    }

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex BlockCommentRegex();

    [GeneratedRegex(@"//[^\r\n]*", RegexOptions.CultureInvariant)]
    private static partial Regex LineCommentRegex();

    [GeneratedRegex(@"\.MapControllers\s*\(\s*\)(?<secured>\s*\.RequireAuthorization\s*\()?", RegexOptions.CultureInvariant)]
    private static partial Regex MapControllersRegex();

    [GeneratedRegex(@"\bAllowAnonymous(Attribute)?\b", RegexOptions.CultureInvariant)]
    private static partial Regex AnonymousUsageRegex();
}
