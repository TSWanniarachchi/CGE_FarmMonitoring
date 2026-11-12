using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using FarmFMS.API.Common.Security;
using FarmFMS.Application.Common.Exceptions;
using FarmFMS.Application.Common.Results;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Application.Module01.Authorization;
using FarmFMS.Infrastructure.Persistence;
using FarmFMS.UnitTests.Module01.Fakes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace FarmFMS.UnitTests.Module01.Api;

/// <summary>Regression tests for the G6 review findings M-1, M-3, L-4, L-8, L-9.</summary>
public class ReviewFixTests
{
    private static string Token(AuthApiFactory factory) =>
        factory.Services.GetRequiredService<IAccessTokenIssuer>().Issue(42, 900, "jsmith").Token;

    private static StringContent Body(string json) => new(json, System.Text.Encoding.UTF8, "application/json");

    private static string RefreshJson() => $"{{\"refreshToken\":\"{new FarmFMS.Infrastructure.Security.SessionTokenFactory().Create().RawToken}\"}}";

    #region M-1

    [Theory]
    [InlineData("/api/auth/refresh", HttpStatusCode.Unauthorized)]
    [InlineData("/api/auth/logout", HttpStatusCode.NoContent)]
    public async Task Anonymous_endpoints_never_validate_or_touch_the_session_of_an_attached_bearer_token(string path, HttpStatusCode expected)
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = Body(RefreshJson()) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token(factory)); // valid token, valid session

        using var response = await client.SendAsync(request);

        Assert.Equal(expected, response.StatusCode);
        Assert.Empty(factory.Sessions.Validations); // no N6 call, so LastActDate is not advanced
        Assert.DoesNotContain("N6", factory.Log);
    }

    [Fact]
    public async Task Login_and_health_with_an_attached_bearer_token_do_not_validate_the_session()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = Body("{\"username\":\"ghost\",\"password\":\"whatever1!A\"}")
        };
        login.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token(factory));
        using var health = new HttpRequestMessage(HttpMethod.Get, "/health");
        health.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token(factory));

        using var loginResponse = await client.SendAsync(login);
        using var healthResponse = await client.SendAsync(health);

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode); // unknown user, not a token problem
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        Assert.Empty(factory.Sessions.Validations);
    }

    [Fact]
    public async Task A_protected_endpoint_still_validates_the_session()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/test-probe/secure");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token(factory));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(factory.Sessions.Validations);
    }

    #endregion

    #region L-4

    [Fact]
    public async Task A_transient_database_failure_during_session_validation_is_503_not_401()
    {
        using var factory = new AuthApiFactory();
        factory.Sessions.ValidateException = new DataAccessException(
            Error.Unavailable("Database.Unavailable", "The service is temporarily unavailable. Please try again."),
            databaseErrorNumber: 1205,
            isTransient: true);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token(factory));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var text = await response.Content.ReadAsStringAsync();
        Assert.Equal(503, JsonDocument.Parse(text).RootElement.GetProperty("status").GetInt32());
        Assert.DoesNotContain("1205", text);
        Assert.DoesNotContain("Exception", text);
    }

    #endregion

    #region L-8, L-9

    [Fact]
    public async Task The_permission_handler_passes_the_request_cancellation_token()
    {
        var repository = new FakeUserPermissionRepository();
        repository.Rows.Add(new EffectivePermissionReadModel { MenuCode = "Farm", PermissionCode = "View" });
        var handler = new PermissionAuthorizationHandler(
            new PermissionService(repository), NullLogger<PermissionAuthorizationHandler>.Instance);
        using var cts = new CancellationTokenSource();
        var http = new DefaultHttpContext { RequestAborted = cts.Token };
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "42")], "Test"));
        var context = new AuthorizationHandlerContext([new PermissionRequirement("Farm", "View")], user, http);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.Equal(cts.Token, repository.LastToken);
    }

    [Theory]
    [InlineData("login", 11, 22, 11)]
    [InlineData("refresh", 11, 22, 22)]
    public void The_Retry_After_fallback_uses_the_window_of_the_rejecting_policy(string policy, int loginWindow, int refreshWindow, int expected)
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(new EnableRateLimitingAttribute(policy)), "test"));
        var options = new RateLimitingOptions
        {
            Login = new RateLimitPolicyOptions { PermitLimit = 1, WindowSeconds = loginWindow },
            Refresh = new RateLimitPolicyOptions { PermitLimit = 1, WindowSeconds = refreshWindow }
        };

        Assert.Equal(expected, RateLimitingExtensions.WindowSecondsFor(context, options));
    }

    [Fact]
    public async Task The_refresh_limit_rejects_with_a_Retry_After_within_the_refresh_window()
    {
        using var factory = new AuthApiFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:Refresh:PermitLimit"] = "1",
            ["RateLimiting:Refresh:WindowSeconds"] = "7"
        });
        using var client = factory.CreateClient();

        using var first = await client.PostAsync("/api/auth/refresh", Body(RefreshJson()));
        using var limited = await client.PostAsync("/api/auth/refresh", Body(RefreshJson()));

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.InRange(limited.Headers.RetryAfter!.Delta!.Value.TotalSeconds, 1, 7);
    }

    #endregion

    #region M-3 design-time factory

    [Fact]
    public void Without_a_connection_string_the_design_time_context_falls_back_to_the_provider_without_one()
    {
        using var context = DesignTimeDbContextFactory.Create(new ConfigurationBuilder().Build());

        Assert.NotNull(context.Model);
        Assert.True(string.IsNullOrEmpty(context.Database.GetConnectionString()));
    }

    [Fact]
    public void A_configured_connection_string_is_used_without_being_opened()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [DesignTimeDbContextFactory.ConnectionStringKey] = TestEnvironment.PlaceholderConnectionString
            })
            .Build();

        using var context = DesignTimeDbContextFactory.Create(configuration);

        Assert.Contains("farmfms-tests.invalid", context.Database.GetConnectionString()); // EF normalizes the keywords
    }

    [Fact]
    public void The_design_time_user_secrets_id_is_the_one_of_the_API_project()
    {
        var apiId = typeof(Program).Assembly.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId;

        Assert.Equal(DesignTimeDbContextFactory.ApiUserSecretsId, apiId);
    }

    #endregion
}
