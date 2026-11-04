using System.Diagnostics;
using FarmFMS.API.Common.Errors;
using FarmFMS.API.Common.Identity;
using FarmFMS.Application.Abstractions.Identity;
using Microsoft.AspNetCore.HttpLogging;

/*
 * Description  : Registers the API-layer services (current user, ProblemDetails, exception handling, request logging).
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.API;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        // Current user (audit fields)
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Errors: RFC 7807 ProblemDetails for every error response, with a traceId that matches the logs.
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var httpContext = context.HttpContext;

                context.ProblemDetails.Instance ??= $"{httpContext.Request.Method} {httpContext.Request.Path}";
                context.ProblemDetails.Extensions.TryAdd(
                    "traceId",
                    Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier);
            };
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Request logging: method, path (no query string), status and duration only.
        // Headers and bodies are never logged (they can carry passwords and tokens).
        services.AddHttpLogging(options =>
        {
            options.LoggingFields =
                HttpLoggingFields.RequestMethod |
                HttpLoggingFields.RequestPath |
                HttpLoggingFields.ResponseStatusCode |
                HttpLoggingFields.Duration;
            options.CombineLogs = true;
        });

        return services;
    }
}
