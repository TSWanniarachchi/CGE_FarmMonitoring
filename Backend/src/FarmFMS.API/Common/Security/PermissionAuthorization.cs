using System.Globalization;
using FarmFMS.Application.Module01.Authorization;
using FarmFMS.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;

/*
 * Description  : Permission-based authorization mechanism (Step 0 spec 8.2, AD-13, D-11).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.API.Common.Security;

/// <summary>Requires the effective permission <see cref="PermissionCode"/> on the menu <see cref="MenuCode"/>.</summary>
public sealed class PermissionRequirement(string menuCode, string permissionCode) : IAuthorizationRequirement
{
    public string MenuCode { get; } = menuCode ?? throw new ArgumentNullException(nameof(menuCode));

    public string PermissionCode { get; } = permissionCode ?? throw new ArgumentNullException(nameof(permissionCode));

    public string PolicyName => Policies.Permission(MenuCode, PermissionCode);
}

/// <summary>
/// Policy names. Step 0 registers no business policy: Menu / Permission codes are not defined yet (DBG-014); later
/// steps add policies with <see cref="AddPermissionPolicy"/> (naming <c>Perm.{MenuCode}.{PermissionCode}</c>, D-11).
/// </summary>
public static class Policies
{
    public const string PermissionPrefix = "Perm.";

    /// <summary>The policy name <c>Perm.{MenuCode}.{PermissionCode}</c>.</summary>
    public static string Permission(string menuCode, string permissionCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(menuCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        return $"{PermissionPrefix}{menuCode}.{permissionCode}";
    }

    /// <summary>Registers the policy <c>Perm.{menu}.{permission}</c> (authenticated + the permission requirement).</summary>
    public static AuthorizationBuilder AddPermissionPolicy(this AuthorizationBuilder builder, string menuCode, string permissionCode)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddPolicy(
            Permission(menuCode, permissionCode),
            policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissionRequirement(menuCode, permissionCode)));
    }
}

/// <summary>
/// Succeeds when <see cref="IPermissionService"/> (memoized per request, <c>SvaUsrPerm</c>) grants the permission to the
/// authenticated user. Denials are logged as <c>AuthorizationDenied</c> (Warning: user Id and policy only).
/// </summary>
internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissions;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(IPermissionService permissions, ILogger<PermissionAuthorizationHandler> logger)
    {
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var subject = context.User.FindFirst(JwtValidation.SubjectClaim)?.Value;

        if (context.User.Identity?.IsAuthenticated == true &&
            int.TryParse(subject, NumberStyles.None, CultureInfo.InvariantCulture, out var userId) && userId > 0 &&
            await _permissions.HasPermissionAsync(
                userId, requirement.MenuCode, requirement.PermissionCode, CancellationTokenOf(context)))
        {
            context.Succeed(requirement);
            return;
        }

        _logger.LogWarning(
            "AuthorizationDenied: user {UserId}, policy {PolicyName}",
            subject ?? "anonymous", requirement.PolicyName);
    }

    // The request's cancellation token (L-8); handlers run outside a request only in unit tests.
    private static CancellationToken CancellationTokenOf(AuthorizationHandlerContext context) =>
        context.Resource is HttpContext http ? http.RequestAborted : CancellationToken.None;
}
