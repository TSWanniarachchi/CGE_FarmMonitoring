using FarmFMS.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/*
 * Description  : Maps the liveness and readiness health endpoints with OpenAPI metadata.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.API.Common.Health;

/// <summary>
/// Health endpoints for infrastructure monitoring. They run the registered health checks through
/// <see cref="HealthCheckService"/> (the same registrations <c>MapHealthChecks</c> would use) but are mapped as
/// regular endpoints so they appear in the OpenAPI document. Anonymous; the body is only the overall status.
/// </summary>
internal static class HealthEndpoints
{
    private const string TextPlain = "text/plain";

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var health = endpoints.MapGroup("/health")
            .WithTags("Health")
            .AllowAnonymous();

        health.MapGet("", (HealthCheckService healthChecks, HttpContext context) =>
                CheckAsync(healthChecks, context, check => !check.Tags.Contains(HealthCheckTags.Ready)))
            .WithName("GetLiveness")
            .WithSummary("Liveness: the API process is running.")
            .WithDescription(
                "Returns `Healthy` when the API is up. Does not depend on the database or any other external resource, " +
                "so it can be used as a liveness probe.")
            .Produces<string>(StatusCodes.Status200OK, TextPlain)
            .Produces<string>(StatusCodes.Status503ServiceUnavailable, TextPlain);

        health.MapGet("/ready", (HealthCheckService healthChecks, HttpContext context) =>
                CheckAsync(healthChecks, context, check => check.Tags.Contains(HealthCheckTags.Ready)))
            .WithName("GetReadiness")
            .WithSummary("Readiness: the API can serve requests (SQL Server reachable).")
            .WithDescription(
                "Opens and closes a SQL Server connection; no query or Stored Procedure is executed. " +
                "Returns `Healthy` (200) when the database is reachable and `Unhealthy` (503) when it is not. " +
                "The response never contains connection or error details.")
            .Produces<string>(StatusCodes.Status200OK, TextPlain)
            .Produces<string>(StatusCodes.Status503ServiceUnavailable, TextPlain);

        return endpoints;
    }

    private static async Task<IResult> CheckAsync(
        HealthCheckService healthChecks,
        HttpContext context,
        Func<HealthCheckRegistration, bool> predicate)
    {
        var report = await healthChecks.CheckHealthAsync(predicate, context.RequestAborted);

        // Same status mapping and no-cache headers as the health check middleware.
        context.Response.Headers.CacheControl = "no-store, no-cache";
        context.Response.Headers.Pragma = "no-cache";

        var statusCode = report.Status == HealthStatus.Unhealthy
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status200OK;

        return Results.Text(report.Status.ToString(), TextPlain, statusCode: statusCode);
    }
}
