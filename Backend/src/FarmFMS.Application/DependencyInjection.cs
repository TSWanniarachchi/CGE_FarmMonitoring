using FarmFMS.Application.Abstractions.Identity;
using FarmFMS.Application.Common.Identity;
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

        return services;
    }
}
