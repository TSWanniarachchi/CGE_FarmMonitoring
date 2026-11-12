using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

/*
 * Description  : Rate limiting of login / refresh / logout (Step 0 spec 8.3, D-26).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.API.Common.Security;

/// <summary>Limit of one fixed window per client IP.</summary>
public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; } = 60;
}

/// <summary>
/// <c>RateLimiting</c> section: <c>Login</c> (default 10 requests / 60 s per client IP) and <c>Refresh</c>
/// (default 30 / 60 s; also used by logout). Validated at startup: values must be greater than 0.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public const string LoginPolicy = "login";
    public const string RefreshPolicy = "refresh";

    public RateLimitPolicyOptions Login { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };

    public RateLimitPolicyOptions Refresh { get; set; } = new() { PermitLimit = 30, WindowSeconds = 60 };
}

public sealed class RateLimitingOptionsValidator : IValidateOptions<RateLimitingOptions>
{
    public ValidateOptionsResult Validate(string? name, RateLimitingOptions options)
    {
        var failures = new List<string>();

        Check("Login", options.Login, failures);
        Check("Refresh", options.Refresh, failures);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void Check(string name, RateLimitPolicyOptions? policy, List<string> failures)
    {
        if (policy is null)
        {
            failures.Add($"RateLimiting:{name} is required.");
            return;
        }

        if (policy.PermitLimit <= 0)
            failures.Add($"RateLimiting:{name}:PermitLimit must be greater than 0.");
        if (policy.WindowSeconds <= 0)
            failures.Add($"RateLimiting:{name}:WindowSeconds must be greater than 0.");
    }
}

public static class RateLimitingExtensions
{
    /// <summary>
    /// Fixed-window policies <c>login</c> and <c>refresh</c> partitioned by the client IP (after forwarded headers).
    /// Rejections are 429 ProblemDetails with <c>Retry-After</c>.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitingOptions>()
            .BindConfiguration(RateLimitingOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<RateLimitingOptions>, RateLimitingOptionsValidator>();

        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RateLimitingOptions>>((limiter, settings) =>
            {
                limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                limiter.AddPolicy(RateLimitingOptions.LoginPolicy, context => Partition(context, settings.Value.Login));
                limiter.AddPolicy(RateLimitingOptions.RefreshPolicy, context => Partition(context, settings.Value.Refresh));
                limiter.OnRejected = async (rejected, _) =>
                {
                    var context = rejected.HttpContext;
                    var retryAfter = rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var delay)
                        ? (int)Math.Ceiling(delay.TotalSeconds)
                        : WindowSecondsFor(context, settings.Value);

                    context.Response.Headers.RetryAfter = Math.Max(retryAfter, 1).ToString(CultureInfo.InvariantCulture);

                    await ProblemResponses.WriteAsync(
                        context,
                        StatusCodes.Status429TooManyRequests,
                        "Too Many Requests",
                        "Too many requests. Please try again later.",
                        ProblemResponses.TooManyRequests);
                };
            });

        return services;
    }

    // L-9: the fallback delay is the window of the policy that rejected the request (refresh, not login).
    internal static int WindowSecondsFor(HttpContext context, RateLimitingOptions options) =>
        context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName == RateLimitingOptions.RefreshPolicy
            ? options.Refresh.WindowSeconds
            : options.Login.WindowSeconds;

    private static RateLimitPartition<string> Partition(HttpContext context, RateLimitPolicyOptions policy) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = policy.PermitLimit,
                Window = TimeSpan.FromSeconds(policy.WindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
}
