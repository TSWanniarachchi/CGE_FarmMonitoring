using FarmFMS.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

/*
 * Description  : Registers JWT authentication and the permission authorization mechanism (Step 0 spec 8.2).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.API.Common.Security;

public static class ApiSecurityExtensions
{
    /// <summary>
    /// JwtBearer with the shared <see cref="JwtValidation.CreateParameters"/>, configured through
    /// <see cref="IOptions{TOptions}"/> of <see cref="JwtOptions"/> so the startup validation still applies (AC-15).
    /// <c>MapInboundClaims = false</c>: claims keep their JWT names (sub, sid, name).
    /// Authorization: the permission handler and no business policies (DBG-014); controllers require authentication by
    /// default (<c>MapControllers().RequireAuthorization()</c> in Program.cs, AD-13).
    /// </summary>
    public static IServiceCollection AddApiSecurity(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.SaveToken = false;
                bearer.RequireHttpsMetadata = false; // no metadata endpoint: the signing key is configured
                bearer.TokenValidationParameters = JwtValidation.CreateParameters(jwt.Value);
                bearer.Events = new ApiJwtBearerEvents();
            });

        services.AddAuthorizationBuilder();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
