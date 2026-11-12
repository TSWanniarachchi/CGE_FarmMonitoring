using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

/*
 * Description  : Trusted forwarded headers for the client IP (Step 0 spec 8.4, D-26).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.API.Common.Security;

/// <summary>
/// <c>ForwardedHeaders</c> section. <see cref="KnownProxies"/> lists the IP addresses of the proxies (the Blazor server)
/// whose <c>X-Forwarded-For</c> is trusted. Default empty = trust nobody: the header is ignored and the client IP is
/// the direct peer.
/// </summary>
public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    public string[] KnownProxies { get; set; } = [];
}

public sealed class ForwardedHeadersSettingsValidator : IValidateOptions<ForwardedHeadersSettings>
{
    // "10.0.0" parses as 10.0.0.0 in .NET; only complete dotted IPv4 addresses and IPv6 addresses are accepted.
    private static bool IsValidAddress(string? value) =>
        IPAddress.TryParse(value, out var address) &&
        (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 || value!.Count(c => c == '.') == 3);

    public ValidateOptionsResult Validate(string? name, ForwardedHeadersSettings options)
    {
        var invalid = (options.KnownProxies ?? []).Count(proxy => !IsValidAddress(proxy));

        return invalid == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail($"ForwardedHeaders:KnownProxies contains {invalid} value(s) that are not valid IP addresses.");
    }
}

public static class ForwardedHeadersExtensions
{
    /// <summary>
    /// Only <c>X-Forwarded-For</c> is processed, and only from the configured proxies. The framework defaults
    /// (loopback proxies / networks) are cleared on purpose. Important: the middleware trusts EVERY sender when both
    /// known lists are empty, so with no configured proxy forwarding is switched off completely
    /// (<see cref="ForwardedHeaders.None"/>) instead of relying on empty lists.
    /// </summary>
    public static IServiceCollection AddApiForwardedHeaders(this IServiceCollection services)
    {
        services.AddOptions<ForwardedHeadersSettings>()
            .BindConfiguration(ForwardedHeadersSettings.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ForwardedHeadersSettings>, ForwardedHeadersSettingsValidator>();

        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IOptions<ForwardedHeadersSettings>>((forwarded, settings) =>
            {
                forwarded.ForwardLimit = 1;
                forwarded.KnownProxies.Clear();
                forwarded.KnownIPNetworks.Clear();

                // Empty known lists would mean "trust everybody": no configured proxy = process nothing.
                forwarded.ForwardedHeaders = settings.Value.KnownProxies.Length == 0
                    ? ForwardedHeaders.None
                    : ForwardedHeaders.XForwardedFor;

                foreach (var proxy in settings.Value.KnownProxies)
                    forwarded.KnownProxies.Add(IPAddress.Parse(proxy));
            });

        return services;
    }
}
