using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using FarmFMS.API.Controllers.Module01;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Application.Module01.Authorization;
using FarmFMS.Application.Module01.Users;
using FarmFMS.Domain.Module01.Authentication;
using FarmFMS.Infrastructure.Security;
using FarmFMS.UnitTests.Module01.Fakes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FarmFMS.UnitTests.Module01.Api;

/// <summary>API host tests of Step 0 (spec 10.1): authentication, secure by default, rate limiting, logging redaction.</summary>
public class AuthApiTests
{
    private const string Password = "Sup3r-Secret!";
    private const int UserId = 42;
    private const int SessionId = 900;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    #region Helpers

    private static StringContent JsonBody(object body) =>
        new(JsonSerializer.Serialize(body, Json), System.Text.Encoding.UTF8, "application/json");

    private static string AccessToken(AuthApiFactory factory, int userId = UserId, int sessionId = SessionId) =>
        factory.Services.GetRequiredService<IAccessTokenIssuer>().Issue(userId, sessionId, "jsmith").Token;

    private static HttpRequestMessage Get(string path, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

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

    #endregion

    #region /me and session validation

    [Fact]
    public async Task Me_without_a_token_is_401_with_generic_ProblemDetails()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Bearer", Assert.Single(response.Headers.WwwAuthenticate).ToString()); // no error_description
        var body = await ReadAsync(response);
        Assert.Equal(401, body.GetProperty("status").GetInt32());
        Assert.Equal("Auth.Unauthenticated", body.GetProperty("code").GetString());
        Assert.True(body.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Me_with_a_valid_token_and_session_returns_the_profile_and_permissions_without_password_fields()
    {
        using var factory = new AuthApiFactory();
        factory.Users.User = new AppUserProfileReadModel
        {
            Id = UserId, Username = "jsmith", FirstName = "Jane", LastName = "Smith", Email = "jane@example.test", IsActive = true
        };
        factory.Permissions.Rows.Add(new EffectivePermissionReadModel { MenuCode = "Farm", PermissionCode = "View" });
        using var client = factory.CreateClient();

        using var response = await client.SendAsync(Get("/api/auth/me", AccessToken(factory)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
        var body = await ReadAsync(response);
        Assert.Equal(UserId, body.GetProperty("id").GetInt32());
        Assert.Equal("jsmith", body.GetProperty("username").GetString());
        Assert.Equal("Farm", body.GetProperty("permissions")[0].GetProperty("menuCode").GetString());
        Assert.Equal("View", body.GetProperty("permissions")[0].GetProperty("permissionCode").GetString());
        Assert.DoesNotContain(body.EnumerateObject().Select(p => p.Name), n => n.Contains("assword", StringComparison.OrdinalIgnoreCase) && n != "passwordChangeRequired");

        // The session was validated with the claims of the token.
        var call = Assert.Single(factory.Sessions.Validations);
        Assert.Equal((SessionId, UserId, 30, 60, (byte)1, 7), (call.SessionId, call.UserId, call.IdleTimeoutMinutes, call.TouchIntervalSeconds, call.Platform, call.ModifiedBy));
    }

    [Theory]
    [InlineData(SessionValidationStatus.NotFoundOrUserMismatch)]
    [InlineData(SessionValidationStatus.Revoked)]
    [InlineData(SessionValidationStatus.Expired)]
    [InlineData(SessionValidationStatus.Idle)]
    [InlineData(SessionValidationStatus.UserInactive)]
    [InlineData(SessionValidationStatus.UserLocked)]
    [InlineData(SessionValidationStatus.PlatformDenied)]
    public async Task A_valid_token_with_a_rejected_session_is_401_without_revealing_the_reason(SessionValidationStatus status)
    {
        using var factory = new AuthApiFactory();
        factory.Sessions.Status = status;
        using var client = factory.CreateClient();

        using var response = await client.SendAsync(Get("/api/auth/me", AccessToken(factory)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        var body = JsonDocument.Parse(text).RootElement;
        Assert.Equal("Auth.SessionInvalid", body.GetProperty("code").GetString());
        Assert.Equal("Your session is no longer valid. Please sign in again.", body.GetProperty("detail").GetString());
        Assert.DoesNotContain(status.ToString(), text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Bearer", Assert.Single(response.Headers.WwwAuthenticate).ToString());
    }

    [Fact]
    public async Task When_the_System_user_cannot_be_resolved_a_token_request_fails_closed_with_401_and_an_Error_log()
    {
        using var factory = new AuthApiFactory();
        factory.SystemUser.Id = null;
        using var client = factory.CreateClient();

        using var response = await client.SendAsync(Get("/api/auth/me", AccessToken(factory)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(factory.Logs.Entries, e => e.Level == LogLevel.Error);
        Assert.Empty(factory.Sessions.Validations);
    }

    [Fact]
    public async Task Garbage_and_foreign_tokens_are_401()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        var foreignOptions = new JwtOptions { SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) };
        var foreign = new JwtAccessTokenIssuer(Options.Create(foreignOptions), TimeProvider.System).Issue(UserId, SessionId, "x").Token;

        foreach (var token in new[] { "garbage", "a.b.c", foreign })
        {
            using var response = await client.SendAsync(Get("/api/auth/me", token));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        Assert.Empty(factory.Sessions.Validations);
    }

    [Fact]
    public async Task An_expired_token_is_401()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        var options = factory.Services.GetRequiredService<IOptions<JwtOptions>>();
        var expired = new JwtAccessTokenIssuer(options, new TestTimeProvider(DateTimeOffset.UtcNow.AddHours(-1)))
            .Issue(UserId, SessionId, "jsmith").Token;

        using var response = await client.SendAsync(Get("/api/auth/me", expired));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(factory.Sessions.Validations);
    }

    [Theory]
    [InlineData("no-sid")]
    [InlineData("bad-sid")]
    [InlineData("no-sub")]
    public async Task A_correctly_signed_token_without_valid_subject_or_session_claims_is_401(string variant)
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        var options = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;

        var claims = new Dictionary<string, object> { ["sub"] = "42", ["sid"] = "900", ["name"] = "jsmith" };
        if (variant == "no-sid") claims.Remove("sid");
        if (variant == "bad-sid") claims["sid"] = "abc";
        if (variant == "no-sub") claims.Remove("sub");

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = claims,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(options.GetSigningKeyBytes()), SecurityAlgorithms.HmacSha256)
        });

        using var response = await client.SendAsync(Get("/api/auth/me", token));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(factory.Sessions.Validations);
    }

    #endregion

    #region Secure by default and permissions

    [Fact]
    public async Task A_controller_without_AllowAnonymous_requires_authentication()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        using var anonymous = await client.GetAsync("/api/test-probe/secure");
        using var authenticated = await client.SendAsync(Get("/api/test-probe/secure", AccessToken(factory)));

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.OK, authenticated.StatusCode);
    }

    [Fact]
    public async Task A_permission_policy_allows_with_the_permission_and_denies_without_it()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        var token = AccessToken(factory);

        using var denied = await client.SendAsync(Get("/api/test-probe/farm-view", token));

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        var body = await ReadAsync(denied);
        Assert.Equal("Auth.Forbidden", body.GetProperty("code").GetString());
        Assert.Contains(factory.Logs.Entries, e => e.Level == LogLevel.Warning && e.Text.Contains("AuthorizationDenied") &&
                                                   e.Text.Contains("Perm.Farm.View") && e.Text.Contains(UserId.ToString()));

        factory.Permissions.Rows.Add(new EffectivePermissionReadModel { MenuCode = "Farm", PermissionCode = "View" });
        using var allowed = await client.SendAsync(Get("/api/test-probe/farm-view", token));

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task A_policy_request_without_a_token_is_401_not_403()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/test-probe/farm-view");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void AllowAnonymous_appears_only_on_login_refresh_and_logout()
    {
        var anonymous = typeof(AuthController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetCustomAttribute<AllowAnonymousAttribute>() is not null
                ? [$"{t.Name} (class)"]
                : t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => m.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
                    .Select(m => $"{t.Name}.{m.Name}"))
            .Order()
            .ToArray();

        Assert.Equal(["AuthController.Login", "AuthController.Logout", "AuthController.Refresh"], anonymous);
    }

    [Fact]
    public async Task Health_endpoints_stay_anonymous()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Anonymous endpoints

    [Fact]
    public async Task Login_refresh_and_logout_are_reachable_anonymously_with_the_specified_errors()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        using var login = await client.PostAsync("/api/auth/login", JsonBody(new { username = "ghost", password = "whatever1!A" }));
        using var refresh = await client.PostAsync("/api/auth/refresh", JsonBody(new { refreshToken = ValidRefreshToken() }));
        using var logout = await client.PostAsync("/api/auth/logout", JsonBody(new { refreshToken = ValidRefreshToken() }));

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal("Auth.InvalidCredentials", (await ReadAsync(login)).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        Assert.Equal("Auth.InvalidRefreshToken", (await ReadAsync(refresh)).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
    }

    [Fact]
    public async Task Malformed_requests_are_400_ValidationProblemDetails()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        using var login = await client.PostAsync("/api/auth/login", JsonBody(new { username = "", password = "" }));
        using var refresh = await client.PostAsync("/api/auth/refresh", JsonBody(new { refreshToken = "short" }));
        using var logout = await client.PostAsync("/api/auth/logout", JsonBody(new { }));
        using var tooLongPassword = await client.PostAsync("/api/auth/login", JsonBody(new { username = "u", password = new string('x', 129) }));

        foreach (var response in new[] { login, refresh, logout, tooLongPassword })
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.True((await ReadAsync(response)).TryGetProperty("errors", out _));
        }

        Assert.Equal(0, factory.Login.CredentialLookups);
    }

    [Fact]
    public async Task Successful_login_returns_the_token_contract_with_no_store_and_the_access_token_works()
    {
        using var factory = new AuthApiFactory();
        factory.Login.Credentials = CredentialsFor(factory);
        factory.Users.User = new AppUserProfileReadModel { Id = UserId, Username = "jsmith", FirstName = "Jane", IsActive = true };
        factory.Sessions.Created = new CreatedSessionReadModel(SessionId, DateTime.UtcNow.AddHours(12));
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/auth/login", JsonBody(new { username = " jsmith ", password = Password }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
        Assert.Contains("no-cache", response.Headers.Pragma.ToString());
        var body = await ReadAsync(response);
        foreach (var name in new[] { "accessToken", "accessTokenExpiresUtc", "refreshToken", "sessionExpiresUtc", "idleTimeoutMinutes", "tokenType", "passwordChangeRequired", "user" })
            Assert.True(body.TryGetProperty(name, out _), name);
        Assert.Equal("Bearer", body.GetProperty("tokenType").GetString());
        Assert.Equal(30, body.GetProperty("idleTimeoutMinutes").GetInt32());
        Assert.Equal(43, body.GetProperty("refreshToken").GetString()!.Length);
        Assert.Equal(UserId, body.GetProperty("user").GetProperty("id").GetInt32());
        Assert.Equal("jsmith", factory.Login.LastUsername);

        // The access token from the response is accepted by a protected endpoint.
        using var me = await client.SendAsync(Get("/api/auth/me", body.GetProperty("accessToken").GetString()));
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        Assert.Equal(LoginEventType.Success, Assert.Single(factory.History.Entries).EventType);
    }

    [Fact]
    public async Task Logout_and_refresh_responses_are_not_cacheable()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        using var refresh = await client.PostAsync("/api/auth/refresh", JsonBody(new { refreshToken = ValidRefreshToken() }));
        using var logout = await client.PostAsync("/api/auth/logout", JsonBody(new { refreshToken = ValidRefreshToken() }));

        Assert.Contains("no-store", refresh.Headers.CacheControl?.ToString());
        Assert.Contains("no-store", logout.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task A_spoofed_forwarded_header_is_ignored_when_no_proxy_is_configured()
    {
        using var factory = new AuthApiFactory();
        factory.Login.Credentials = null;
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonBody(new { username = "ghost", password = "whatever1!A" })
        };
        request.Headers.Add("X-Forwarded-For", "203.0.113.9");
        request.Headers.UserAgent.ParseAdd("TestAgent/2.0");

        using var response = await client.SendAsync(request);

        var entry = Assert.Single(factory.History.Entries);
        Assert.NotEqual("203.0.113.9", entry.IpAddress);
        Assert.Equal("TestAgent/2.0", entry.DeviceInfo);
    }

    #endregion

    #region Rate limiting

    [Fact]
    public async Task Login_is_rate_limited_per_client_with_429_ProblemDetails_and_Retry_After()
    {
        using var factory = new AuthApiFactory(new Dictionary<string, string?> { ["RateLimiting:Login:PermitLimit"] = "3" });
        using var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            using var allowed = await client.PostAsync("/api/auth/login", JsonBody(new { username = "ghost", password = "whatever1!A" }));
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        using var limited = await client.PostAsync("/api/auth/login", JsonBody(new { username = "ghost", password = "whatever1!A" }));

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.NotNull(limited.Headers.RetryAfter?.Delta);
        Assert.InRange(limited.Headers.RetryAfter!.Delta!.Value.TotalSeconds, 1, 60);
        var body = await ReadAsync(limited);
        Assert.Equal(429, body.GetProperty("status").GetInt32());
        Assert.Equal("RateLimit.Exceeded", body.GetProperty("code").GetString());
        Assert.Equal(3, factory.Login.CredentialLookups); // the rejected request never reached the service
    }

    [Fact]
    public async Task Refresh_and_logout_share_the_refresh_limit_and_login_is_unaffected()
    {
        using var factory = new AuthApiFactory(new Dictionary<string, string?> { ["RateLimiting:Refresh:PermitLimit"] = "2" });
        using var client = factory.CreateClient();

        using var first = await client.PostAsync("/api/auth/refresh", JsonBody(new { refreshToken = ValidRefreshToken() }));
        using var second = await client.PostAsync("/api/auth/logout", JsonBody(new { refreshToken = ValidRefreshToken() }));
        using var third = await client.PostAsync("/api/auth/refresh", JsonBody(new { refreshToken = ValidRefreshToken() }));
        using var login = await client.PostAsync("/api/auth/login", JsonBody(new { username = "ghost", password = "whatever1!A" }));

        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    #endregion

    #region OpenAPI

    [Fact]
    public async Task The_OpenAPI_document_declares_the_Bearer_scheme_and_the_auth_endpoints()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await ReadAsync(response);
        var scheme = document.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());
        var paths = document.GetProperty("paths");
        foreach (var path in new[] { "/api/auth/login", "/api/auth/refresh", "/api/auth/logout", "/api/auth/me" })
            Assert.True(paths.TryGetProperty(path, out _), path);
    }

    #endregion

    #region Log redaction (AC-16)

    [Fact]
    public async Task No_password_token_hash_or_key_appears_in_logs_or_responses_of_any_auth_flow()
    {
        using var factory = new AuthApiFactory();
        factory.Login.Credentials = CredentialsFor(factory);
        factory.Users.User = new AppUserProfileReadModel { Id = UserId, Username = "jsmith", FirstName = "Jane", IsActive = true };
        factory.Sessions.Created = new CreatedSessionReadModel(SessionId, DateTime.UtcNow.AddHours(12));
        using var client = factory.CreateClient();

        var bodies = new List<string>();
        async Task<HttpResponseMessage> Send(HttpRequestMessage request)
        {
            var response = await client.SendAsync(request);
            bodies.Add(await response.Content.ReadAsStringAsync());
            return response;
        }

        HttpRequestMessage Post(string path, object body, string? token = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonBody(body) };
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        // 1. wrong password, 2. unknown user, 3. successful login
        const string wrongPassword = "Wr0ng-Passw0rd!";
        await Send(Post("/api/auth/login", new { username = "jsmith", password = wrongPassword }));
        var unknownBody = new { username = "ghost.user.name", password = "Unkn0wn-Pass!" };
        factory.Login.Credentials = null;
        await Send(Post("/api/auth/login", unknownBody));
        factory.Login.Credentials = CredentialsFor(factory);
        using var login = await Send(Post("/api/auth/login", new { username = "jsmith", password = Password }));
        var tokens = JsonDocument.Parse(bodies[^1]).RootElement;
        var accessToken = tokens.GetProperty("accessToken").GetString()!;
        var refreshToken = tokens.GetProperty("refreshToken").GetString()!;

        // 4. /me with the access token (the Authorization header must not be logged)
        await Send(Get("/api/auth/me", accessToken));

        // 5. refresh, 6. reuse of the rotated token, 7. logout
        factory.Sessions.Session = ActiveSession();
        using var refresh = await Send(Post("/api/auth/refresh", new { refreshToken }));
        var rotated = ActiveSession();
        rotated.RevokedDate = DateTime.UtcNow;
        rotated.RevokedReason = SessionRevokedReasons.Rotated;
        factory.Sessions.Session = rotated;
        await Send(Post("/api/auth/refresh", new { refreshToken }));
        factory.Sessions.Session = ActiveSession();
        await Send(Post("/api/auth/logout", new { refreshToken }));

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var rotatedTokens = JsonDocument.Parse(bodies[4]).RootElement; // the refresh response
        var newRefreshToken = rotatedTokens.GetProperty("refreshToken").GetString()!;
        var newAccessToken = rotatedTokens.GetProperty("accessToken").GetString()!;

        Assert.True(new SessionTokenFactory().TryHash(refreshToken, out var hash));
        var secrets = new List<string>
        {
            Password, wrongPassword, "Unkn0wn-Pass!", "ghost.user.name",
            accessToken, newAccessToken, refreshToken, newRefreshToken,
            Convert.ToHexString(hash), Convert.ToHexString(hash).ToLowerInvariant(), Convert.ToBase64String(hash),
            Environment.GetEnvironmentVariable("Jwt__SigningKey")!,
            Environment.GetEnvironmentVariable($"PasswordEncryption__Keys__{TestEnvironment.TestKeyVersion}")!
        };

        var logText = factory.Logs.AllText;
        Assert.Contains("LoginSucceeded", logText); // the flows really ran and were logged
        Assert.Contains("RefreshTokenReuse", logText);
        foreach (var secret in secrets)
        {
            Assert.DoesNotContain(secret, logText);
        }

        // Responses of the failure flows never echo secrets either (the token responses carry the tokens by design).
        var failureBodies = string.Join('\n', bodies.Where(b => !b.Contains("accessToken")));
        foreach (var secret in secrets)
            Assert.DoesNotContain(secret, failureBodies);

        // The payload of the credential (ciphertext / nonce / tag) is not logged either.
        var payload = factory.Login.Credentials!;
        Assert.DoesNotContain(Convert.ToBase64String(payload.EncPassword), logText);
        Assert.DoesNotContain(Convert.ToHexString(payload.EncPassword), logText, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
