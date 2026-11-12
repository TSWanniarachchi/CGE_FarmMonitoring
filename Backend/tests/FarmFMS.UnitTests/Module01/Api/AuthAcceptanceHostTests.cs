using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Application.Module01.Users;
using FarmFMS.Domain.Module01.Authentication;
using FarmFMS.Infrastructure.Security;
using FarmFMS.UnitTests.Module01.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FarmFMS.UnitTests.Module01.Api;

/// <summary>
/// QA additions for Step 0 acceptance criteria (spec 11) that had only service-level proof: AC-02 (identical 401 body),
/// AC-04 (403 over HTTP), AC-07 / AC-09 (rotation, reuse over HTTP), AC-08 (logout history), AC-10 (refresh does not touch
/// activity), AC-11 (every protected request), AC-15 (invalid key material). The repositories are fakes: procedure
/// behaviour in SQL Server is not exercised here (Blocked, AD-23).
/// </summary>
public class AuthAcceptanceHostTests
{
    private const string Password = "Sup3r-Secret!";
    private const int UserId = 42;
    private const int SessionId = 900;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    #region Helpers

    private static StringContent JsonBody(object body) =>
        new(JsonSerializer.Serialize(body, Json), System.Text.Encoding.UTF8, "application/json");

    private static LoginCredentialReadModel CredentialsFor(AuthApiFactory factory, string password = Password)
    {
        var payload = factory.Services.GetRequiredService<IPasswordProtector>().Protect(password, UserId);
        return new LoginCredentialReadModel
        {
            UserId = UserId,
            Username = "jsmith",
            FirstName = "Jane",
            LastName = "Smith",
            EncPassword = payload.Ciphertext,
            PasswordNonce = payload.Nonce,
            PasswordTag = payload.Tag,
            KeyVersion = payload.KeyVersion,
            IsActive = true,
            UserAccountId = 5,
            AccountIsActive = true,
            CanAccessWeb = true,
            CanAccessMobile = false
        };
    }

    private static SessionReadModel ActiveSession() => new()
    {
        SessionId = 400,
        UserId = UserId,
        LastActDate = DateTime.UtcNow,
        ExpiresDate = DateTime.UtcNow.AddHours(1),
        Username = "jsmith",
        FirstName = "Jane",
        IsActive = true,
        AccountIsActive = true,
        CanAccessWeb = true
    };

    private static string ValidRefreshToken() => new SessionTokenFactory().Create().RawToken;

    private static string AccessToken(AuthApiFactory factory) =>
        factory.Services.GetRequiredService<IAccessTokenIssuer>().Issue(UserId, SessionId, "jsmith").Token;

    private static HttpRequestMessage Get(string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    /// <summary>Status plus every ProblemDetails member except the per-request trace id.</summary>
    private static async Task<string> ProblemSignatureAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var members = document.RootElement.EnumerateObject()
            .Where(p => p.Name != "traceId")
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => $"{p.Name}={p.Value.GetRawText()}");

        return $"{(int)response.StatusCode} {response.Content.Headers.ContentType?.MediaType} {string.Join(';', members)}";
    }

    #endregion

    #region AC-02, AC-04

    [Fact]
    public async Task AC02_unknown_inactive_wrong_password_and_locked_return_the_same_401_body()
    {
        var signatures = new List<string>();
        var historyReasons = new List<string?>();

        foreach (var scenario in new[] { "unknown", "inactive", "locked", "wrongPassword" })
        {
            using var factory = new AuthApiFactory();
            var credentials = scenario == "wrongPassword" ? CredentialsFor(factory, "A-different-Passw0rd!") : CredentialsFor(factory);
            if (scenario == "inactive") credentials.IsActive = false;
            if (scenario == "locked") credentials.IsLocked = true;
            factory.Login.Credentials = scenario == "unknown" ? null : credentials;
            using var client = factory.CreateClient();

            using var response = await client.PostAsync("/api/auth/login", JsonBody(new { username = "jsmith", password = Password }));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            signatures.Add(await ProblemSignatureAsync(response));
            historyReasons.Add(Assert.Single(factory.History.Entries).FailureReason);
            Assert.DoesNotContain("N1", factory.Log); // no session in any failure case
        }

        // One body for every failure: no oracle (AC-02, D-12).
        Assert.Single(signatures.Distinct());
        Assert.Contains("Auth.InvalidCredentials", signatures[0]);
        Assert.Contains("Invalid username or password.", signatures[0]);

        // ... while the history keeps the distinct reason codes (D-17).
        Assert.Equal(
            [LoginFailureReasons.UnknownUser, LoginFailureReasons.Inactive, LoginFailureReasons.Locked, LoginFailureReasons.InvalidPassword],
            historyReasons);
    }

    [Theory]
    [InlineData("noAccount")]
    [InlineData("accountInactive")]
    [InlineData("noWebAccess")]
    public async Task AC04_correct_password_without_web_access_is_403_and_not_counted(string variant)
    {
        using var factory = new AuthApiFactory();
        var credentials = CredentialsFor(factory);
        switch (variant)
        {
            case "noAccount": credentials.UserAccountId = null; credentials.AccountIsActive = null; credentials.CanAccessWeb = null; break;
            case "accountInactive": credentials.AccountIsActive = false; break;
            default: credentials.CanAccessWeb = false; break;
        }

        factory.Login.Credentials = credentials;
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/auth/login", JsonBody(new { username = "jsmith", password = Password }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Auth.PlatformAccessDenied", body.RootElement.GetProperty("code").GetString());
        Assert.DoesNotContain("N3", factory.Log);   // not counted
        Assert.DoesNotContain("N1", factory.Log);   // no session
        var entry = Assert.Single(factory.History.Entries);
        Assert.Equal(LoginEventType.Failed, entry.EventType);
        Assert.Equal(LoginFailureReasons.PlatformDenied, entry.FailureReason);
    }

    #endregion

    #region AC-07, AC-08, AC-09, AC-10

    [Fact]
    public async Task AC07_refresh_over_HTTP_rotates_and_returns_new_tokens_without_login_history()
    {
        using var factory = new AuthApiFactory();
        factory.Sessions.Session = ActiveSession();
        using var client = factory.CreateClient();
        var presented = ValidRefreshToken();

        using var response = await client.PostAsync("/api/auth/refresh", JsonBody(new { refreshToken = presented }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var newRefresh = body.RootElement.GetProperty("refreshToken").GetString()!;
        Assert.NotEqual(presented, newRefresh);
        Assert.Equal(43, newRefresh.Length);
        var rotation = Assert.Single(factory.Sessions.Rotations);
        Assert.Equal(400, rotation.CurrentSessionId);
        Assert.Equal(30, rotation.IdleTimeoutMinutes);
        Assert.Empty(factory.History.Entries);       // the spec event list has no refresh event
        Assert.Empty(factory.Sessions.Revocations);
    }

    [Fact]
    public async Task AC10_refresh_never_touches_session_activity_so_background_refresh_cannot_extend_the_idle_window()
    {
        using var factory = new AuthApiFactory();
        factory.Sessions.Session = ActiveSession();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/auth/refresh", JsonBody(new { refreshToken = ValidRefreshToken() }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // N6 (the only procedure that advances LastActDate) is not called by refresh; N5 inherits LastActDate (script check in
        // the architecture tests / integration tests Blocked).
        Assert.Empty(factory.Sessions.Validations);
        Assert.DoesNotContain("N6", factory.Log);
        Assert.True(factory.Log.HappenedInOrder("N4", "N5"));
    }

    [Fact]
    public async Task AC10_an_idle_session_is_rejected_on_the_next_request_and_on_refresh()
    {
        using var factory = new AuthApiFactory();
        factory.Sessions.Status = SessionValidationStatus.Idle;
        var idle = ActiveSession();
        idle.IsIdle = true;
        factory.Sessions.Session = idle;
        using var client = factory.CreateClient();

        using var request = await client.SendAsync(Get("/api/auth/me", AccessToken(factory)));
        using var refresh = await client.PostAsync("/api/auth/refresh", JsonBody(new { refreshToken = ValidRefreshToken() }));

        Assert.Equal(HttpStatusCode.Unauthorized, request.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        var revoke = Assert.Single(factory.Sessions.Revocations);
        Assert.Equal(SessionRevokedReasons.IdleTimeout, revoke.Reason);
        Assert.Empty(factory.Sessions.Rotations);
    }

    [Fact]
    public async Task AC09_reuse_of_a_rotated_refresh_token_over_HTTP_revokes_all_sessions_and_logs_a_warning()
    {
        using var factory = new AuthApiFactory();
        var rotated = ActiveSession();
        rotated.RevokedDate = DateTime.UtcNow;
        rotated.RevokedReason = SessionRevokedReasons.Rotated;
        factory.Sessions.Session = rotated;
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/auth/refresh", JsonBody(new { refreshToken = ValidRefreshToken() }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var revoke = Assert.Single(factory.Sessions.Revocations);
        Assert.Equal(UserId, revoke.UserId);
        Assert.Null(revoke.SessionId);                                   // all sessions of the user
        Assert.Equal(SessionRevokedReasons.RefreshTokenReuse, revoke.Reason);
        Assert.Contains(factory.Logs.Entries, e => e.Level == LogLevel.Warning && e.Text.Contains("RefreshTokenReuse"));
        Assert.Empty(factory.Sessions.Rotations);
    }

    [Fact]
    public async Task AC08_logout_over_HTTP_revokes_the_session_and_writes_one_Logout_history_row()
    {
        using var factory = new AuthApiFactory();
        factory.Sessions.Session = ActiveSession();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/auth/logout", JsonBody(new { refreshToken = ValidRefreshToken() }));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var revoke = Assert.Single(factory.Sessions.Revocations);
        Assert.Equal((UserId, (int?)400, SessionRevokedReasons.Logout), (revoke.UserId, revoke.SessionId, revoke.Reason));
        var entry = Assert.Single(factory.History.Entries);
        Assert.Equal(LoginEventType.Logout, entry.EventType);
        Assert.Equal(UserId, entry.UserId);
        Assert.True(factory.Log.HappenedInOrder("N4", "BEGIN", "Revoke", "History", "COMMIT"));
    }

    #endregion

    #region AC-11

    [Theory]
    [InlineData(SessionValidationStatus.NotFoundOrUserMismatch)]
    [InlineData(SessionValidationStatus.Revoked)]
    [InlineData(SessionValidationStatus.Expired)]
    [InlineData(SessionValidationStatus.Idle)]
    [InlineData(SessionValidationStatus.UserInactive)]
    [InlineData(SessionValidationStatus.UserLocked)]
    [InlineData(SessionValidationStatus.PlatformDenied)]
    public async Task AC11_a_rejected_session_is_refused_on_any_protected_endpoint_not_only_me(SessionValidationStatus status)
    {
        using var factory = new AuthApiFactory();
        factory.Sessions.Status = status;
        using var client = factory.CreateClient();
        var token = AccessToken(factory);

        using var secure = await client.SendAsync(Get("/api/test-probe/secure", token));
        using var policy = await client.SendAsync(Get("/api/test-probe/farm-view", token));

        Assert.Equal(HttpStatusCode.Unauthorized, secure.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, policy.StatusCode);   // 401, never reaching the permission check
        Assert.Equal(2, factory.Sessions.Validations.Count);            // validated on every request
        Assert.Equal(0, factory.Permissions.Calls);
    }

    [Fact]
    public async Task AC11_the_session_is_validated_on_every_request_with_the_token_claims()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        var token = AccessToken(factory);

        for (var i = 0; i < 3; i++)
        {
            using var response = await client.SendAsync(Get("/api/test-probe/secure", token));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        Assert.Equal(3, factory.Sessions.Validations.Count);
        Assert.All(factory.Sessions.Validations, call => Assert.Equal((SessionId, UserId), (call.SessionId, call.UserId)));
    }

    #endregion

    #region AC-15

    private static WebApplicationFactory<Program> WithSetting(string key, string value) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })));

    [Theory]
    [InlineData("Jwt:SigningKey", "***not-base-64***")]
    [InlineData("Jwt:SigningKey", "   ")]
    [InlineData($"PasswordEncryption:Keys:{TestEnvironment.TestKeyVersion}", "")]
    [InlineData($"PasswordEncryption:Keys:{TestEnvironment.TestKeyVersion}", "***not-base-64***")]
    public void AC15_invalid_or_empty_key_material_stops_the_API_at_startup(string key, string value)
    {
        using var factory = WithSetting(key, value);

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        // The failure message never carries the offending value or any real key.
        Assert.DoesNotContain(Environment.GetEnvironmentVariable("Jwt__SigningKey")!, exception.ToString());
        Assert.DoesNotContain(Environment.GetEnvironmentVariable($"PasswordEncryption__Keys__{TestEnvironment.TestKeyVersion}")!, exception.ToString());
    }

    [Fact]
    public void AC15_missing_signing_key_and_missing_password_key_are_options_validation_failures()
    {
        using var noJwtKey = WithSetting("Jwt:SigningKey", "");
        using var noPasswordKeys = WithSetting("PasswordEncryption:ActiveKeyVersion", "");

        Assert.Throws<OptionsValidationException>(() => noJwtKey.CreateClient());
        Assert.Throws<OptionsValidationException>(() => noPasswordKeys.CreateClient());
    }

    #endregion
}
