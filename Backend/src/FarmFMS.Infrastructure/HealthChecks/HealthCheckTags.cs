/*
 * Description  : Defines the tags that split health checks into liveness and readiness endpoints.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Infrastructure.HealthChecks;

public static class HealthCheckTags
{
    /// <summary>
    /// Checks that depend on external resources (SQL Server). Served by <c>/health/ready</c> only;
    /// the liveness endpoint <c>/health</c> never runs them.
    /// </summary>
    public const string Ready = "ready";
}
