using FarmFMS.Application.Abstractions.Identity;
using FarmFMS.Application.Common.Identity;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Application.Module01.Authorization;
using FarmFMS.Application.Module01.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/*
 * Description  : Registers the Application-layer services.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers Application-layer services (use cases, validators, mappers).
    /// Module services are registered here, grouped by module, as modules are implemented.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Time: services use TimeProvider (UTC) instead of DateTime.Now, so time can be controlled in tests.
        services.TryAddSingleton(TimeProvider.System);

        // Audit: CreatedBy / ModifiedBy = authenticated user, or the System user inside a system operation.
        services.AddScoped<IAuditActorService, AuditActorService>();

        // Module 01 - Step 0 authentication foundation (services are internal; repositories and security are Infrastructure).
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ISessionValidationService, SessionValidationService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<ICurrentUserProfileService, CurrentUserProfileService>();
        services.AddScoped<IAdministratorBootstrapService, AdministratorBootstrapService>();

        return services;
    }
}
