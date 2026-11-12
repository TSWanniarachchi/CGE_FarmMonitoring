using System.Security.Claims;
using FarmFMS.API.Common.Security;
using FarmFMS.Application.Module01.Authorization;
using FarmFMS.UnitTests.Module01.Fakes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FarmFMS.UnitTests.Module01.Api;

public class ApiSecuritySetupTests
{
    #region Policies and handler

    [Fact]
    public void Policy_names_follow_Perm_Menu_Permission()
    {
        Assert.Equal("Perm.Farm.View", Policies.Permission("Farm", "View"));
        Assert.Equal("Perm.", Policies.PermissionPrefix);
        Assert.Throws<ArgumentException>(() => Policies.Permission("", "View"));
        Assert.Throws<ArgumentException>(() => Policies.Permission("Farm", " "));
        Assert.Equal("Perm.Farm.View", new PermissionRequirement("Farm", "View").PolicyName);
    }

    private static AuthorizationHandlerContext ContextFor(ClaimsPrincipal user, PermissionRequirement requirement) =>
        new([requirement], user, null);

    private static ClaimsPrincipal Authenticated(string sub) =>
        new(new ClaimsIdentity([new Claim("sub", sub)], "Test"));

    private static (PermissionAuthorizationHandler Handler, ListLogger<PermissionAuthorizationHandler> Logger) CreateHandler(params (string, string)[] rows)
    {
        var repository = new FakeUserPermissionRepository();
        repository.Rows.AddRange(rows.Select(r => new EffectivePermissionReadModel { MenuCode = r.Item1, PermissionCode = r.Item2 }));
        var logger = new ListLogger<PermissionAuthorizationHandler>();
        return (new PermissionAuthorizationHandler(new PermissionService(repository), logger), logger);
    }

    [Fact]
    public async Task Handler_succeeds_when_the_user_has_the_permission()
    {
        var (handler, logger) = CreateHandler(("Farm", "View"));
        var requirement = new PermissionRequirement("Farm", "View");
        var context = ContextFor(Authenticated("42"), requirement);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.Empty(logger.Entries);
    }

    [Theory]
    [InlineData("Farm", "Edit")]
    [InlineData("Herd", "View")]
    public async Task Handler_denies_and_logs_AuthorizationDenied_with_user_and_policy(string menu, string permission)
    {
        var (handler, logger) = CreateHandler(("Farm", "View"));
        var context = ContextFor(Authenticated("42"), new PermissionRequirement(menu, permission));

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("AuthorizationDenied", entry.Message);
        Assert.Contains("42", entry.Message);
        Assert.Contains($"Perm.{menu}.{permission}", entry.Message);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    public async Task Handler_denies_an_invalid_subject(string sub)
    {
        var (handler, _) = CreateHandler(("Farm", "View"));
        var context = ContextFor(Authenticated(sub), new PermissionRequirement("Farm", "View"));

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_denies_an_anonymous_user()
    {
        var (handler, logger) = CreateHandler(("Farm", "View"));
        var context = ContextFor(new ClaimsPrincipal(new ClaimsIdentity()), new PermissionRequirement("Farm", "View"));

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        Assert.Single(logger.Entries);
    }

    [Fact]
    public void No_business_policies_are_registered_in_Step_0()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiSecurity();
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.Null(options.GetPolicy("Perm.Farm.View"));
        Assert.Null(options.FallbackPolicy);
    }

    #endregion

    #region Rate limiting and forwarded headers options

    [Theory]
    [InlineData(0, 60, false)]
    [InlineData(-1, 60, false)]
    [InlineData(10, 0, false)]
    [InlineData(10, 60, true)]
    public void Rate_limit_values_must_be_positive(int permits, int window, bool valid)
    {
        var options = new RateLimitingOptions
        {
            Login = new RateLimitPolicyOptions { PermitLimit = permits, WindowSeconds = window },
            Refresh = new RateLimitPolicyOptions { PermitLimit = 30, WindowSeconds = 60 }
        };

        Assert.Equal(valid, new RateLimitingOptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void Rate_limit_defaults_are_login_10_and_refresh_30_per_minute()
    {
        var options = new RateLimitingOptions();

        Assert.Equal((10, 60, 30, 60), (options.Login.PermitLimit, options.Login.WindowSeconds, options.Refresh.PermitLimit, options.Refresh.WindowSeconds));
    }

    [Theory]
    [InlineData("not-an-ip", false)]
    [InlineData("10.0.0", false)]
    [InlineData("10.1.2.3", true)]
    [InlineData("::1", true)]
    public void Known_proxies_must_be_IP_addresses(string proxy, bool valid)
    {
        var result = new ForwardedHeadersSettingsValidator().Validate(null, new ForwardedHeadersSettings { KnownProxies = [proxy] });

        Assert.Equal(valid, result.Succeeded);
    }

    private static ForwardedHeadersOptions ForwardedOptions(params string[] proxies)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(proxies.Select((p, i) => new KeyValuePair<string, string?>($"ForwardedHeaders:KnownProxies:{i}", p)))
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApiForwardedHeaders();
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
    }

    [Fact]
    public void By_default_forwarding_is_off_because_empty_known_lists_would_trust_everybody()
    {
        var options = ForwardedOptions();

        Assert.Equal(ForwardedHeaders.None, options.ForwardedHeaders);
        Assert.Empty(options.KnownProxies);   // the loopback defaults are cleared
        Assert.Empty(options.KnownIPNetworks);
    }

    [Fact]
    public void Configured_proxies_are_trusted()
    {
        var options = ForwardedOptions("10.1.2.3", "::1");

        Assert.Equal(ForwardedHeaders.XForwardedFor, options.ForwardedHeaders);
        Assert.Equal(["10.1.2.3", "::1"], options.KnownProxies.Select(p => p.ToString()).Order(StringComparer.Ordinal));
    }

    #endregion
}
