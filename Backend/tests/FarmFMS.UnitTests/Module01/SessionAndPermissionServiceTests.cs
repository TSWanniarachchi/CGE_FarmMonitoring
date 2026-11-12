using FarmFMS.Application.Common.Identity;
using FarmFMS.Application.Common.Results;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Application.Module01.Authorization;
using FarmFMS.Application.Module01.Users;
using FarmFMS.UnitTests.Module01.Fakes;
using Microsoft.Extensions.Logging;

namespace FarmFMS.UnitTests.Module01;

public class SessionValidationServiceTests
{
    private sealed class Harness
    {
        public CallLog Log { get; } = new();
        public FakeUserSessionRepository Sessions { get; }
        public FakeSessionSettings Settings { get; } = new() { ActivityTouchIntervalSeconds = 45 };
        public FakeAuditActor Actor { get; } = new();
        public ListLogger<SessionValidationService> Logger { get; } = new();
        public SessionValidationService Service { get; }

        public Harness()
        {
            Sessions = new FakeUserSessionRepository(Log);
            Service = new SessionValidationService(Sessions, Settings, Actor, Logger);
        }
    }

    [Fact]
    public async Task Valid_session_succeeds_and_passes_policy_values_and_the_system_actor_to_N6()
    {
        var h = new Harness();

        var result = await h.Service.ValidateAsync(10, 42);

        Assert.True(result.IsSuccess);
        var call = Assert.Single(h.Sessions.Validations);
        Assert.Equal(new ValidateCall(10, 42, 30, 45, 1, 7), call);
        Assert.Equal(["SessionExpiry"], h.Actor.Operations);
        Assert.DoesNotContain(h.Logger.Entries, e => e.Message.Contains("SessionRejected"));
    }

    [Theory]
    [InlineData(SessionValidationStatus.NotFoundOrUserMismatch, LogLevel.Warning)]
    [InlineData(SessionValidationStatus.Revoked, LogLevel.Warning)]
    [InlineData(SessionValidationStatus.Expired, LogLevel.Information)]
    [InlineData(SessionValidationStatus.Idle, LogLevel.Information)]
    [InlineData(SessionValidationStatus.UserInactive, LogLevel.Warning)]
    [InlineData(SessionValidationStatus.UserLocked, LogLevel.Warning)]
    [InlineData(SessionValidationStatus.PlatformDenied, LogLevel.Warning)]
    public async Task Every_non_valid_status_is_Unauthorized_and_logged_at_the_specified_level(SessionValidationStatus status, LogLevel level)
    {
        var h = new Harness();
        h.Sessions.Status = status;

        var result = await h.Service.ValidateAsync(10, 42);

        Assert.Same(AuthErrors.SessionInvalid, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        var entry = Assert.Single(h.Logger.Entries, e => e.Message.Contains("SessionRejected"));
        Assert.Equal(level, entry.Level);
        Assert.Contains(status.ToString(), entry.Message);
    }

    [Fact]
    public void The_status_enum_mirrors_the_N6_codes()
    {
        Assert.Equal(
            [0, 1, 2, 3, 4, 5, 6, 7],
            Enum.GetValues<SessionValidationStatus>().Select(v => (int)v).Order());
    }

    [Fact]
    public async Task Without_a_System_user_the_request_fails_closed_before_N6()
    {
        var h = new Harness();
        h.Actor.SystemUserId = null;

        var result = await h.Service.ValidateAsync(10, 42);

        Assert.Same(AuditErrors.SystemUserNotConfigured, result.Error);
        Assert.Empty(h.Sessions.Validations);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, -1)]
    public async Task Invalid_ids_are_rejected_without_a_database_call(int sessionId, int userId)
    {
        var h = new Harness();

        var result = await h.Service.ValidateAsync(sessionId, userId);

        Assert.Same(AuthErrors.SessionInvalid, result.Error);
        Assert.Empty(h.Sessions.Validations);
    }
}

public class PermissionServiceTests
{
    private static FakeUserPermissionRepository Repository(params (string Menu, string Permission)[] rows)
    {
        var repository = new FakeUserPermissionRepository();
        repository.Rows.AddRange(rows.Select(r => new EffectivePermissionReadModel { MenuCode = r.Menu, PermissionCode = r.Permission }));
        return repository;
    }

    [Fact]
    public async Task Permissions_are_loaded_once_per_scope_and_user()
    {
        var repository = Repository(("Farm", "View"), ("Farm", "Edit"));
        var service = new PermissionService(repository);

        var first = await service.GetPermissionsAsync(42);
        var second = await service.GetPermissionsAsync(42);
        await service.HasPermissionAsync(42, "Farm", "View");

        Assert.Equal(1, repository.Calls);
        Assert.Same(first, second);
        Assert.Equal(2, first.Count);
        Assert.Equal(("Farm", "View"), (first[0].MenuCode, first[0].PermissionCode));
    }

    [Fact]
    public async Task Web_platform_is_requested()
    {
        var repository = Repository();
        var service = new PermissionService(repository);

        await service.GetPermissionsAsync(42);

        Assert.Equal((42, (byte)1), Assert.Single(repository.Requests));
    }

    [Fact]
    public async Task Different_users_are_looked_up_separately()
    {
        var repository = Repository(("Farm", "View"));
        var service = new PermissionService(repository);

        await service.GetPermissionsAsync(1);
        await service.GetPermissionsAsync(2);

        Assert.Equal(2, repository.Calls);
    }

    [Theory]
    [InlineData("Farm", "View", true)]
    [InlineData("Farm", "Edit", false)]
    [InlineData("Herd", "View", false)]
    [InlineData("farm", "View", false)] // ordinal, case-sensitive: fail closed
    [InlineData("Farm", "view", false)]
    [InlineData("", "View", false)]
    [InlineData("Farm", "", false)]
    public async Task HasPermission_compares_both_codes_ordinally(string menu, string permission, bool expected)
    {
        var service = new PermissionService(Repository(("Farm", "View")));

        Assert.Equal(expected, await service.HasPermissionAsync(42, menu, permission));
    }

    [Fact]
    public async Task A_user_without_permissions_gets_an_empty_set_and_is_denied()
    {
        var service = new PermissionService(Repository());

        Assert.Empty(await service.GetPermissionsAsync(42));
        Assert.False(await service.HasPermissionAsync(42, "Farm", "View"));
    }

    [Fact]
    public async Task Invalid_user_ids_never_hit_the_database()
    {
        var repository = Repository(("Farm", "View"));
        var service = new PermissionService(repository);

        Assert.Empty(await service.GetPermissionsAsync(0));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task A_failed_lookup_is_not_memoized()
    {
        var repository = Repository(("Farm", "View"));
        repository.ExceptionToThrow = new InvalidOperationException("db down");
        var service = new PermissionService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetPermissionsAsync(42));
        repository.ExceptionToThrow = null;

        Assert.Single(await service.GetPermissionsAsync(42));
        Assert.Equal(2, repository.Calls);
    }
}

public class CurrentUserProfileServiceTests
{
    private static AppUserProfileReadModel User(bool active = true, bool locked = false) => new()
    {
        Id = 42,
        Username = "jsmith",
        FirstName = "Jane",
        LastName = "Smith",
        Email = "jane@example.test",
        IsActive = active,
        IsLocked = locked,
        PwdChangeReq = true
    };

    [Fact]
    public async Task Profile_contains_the_user_and_the_effective_permissions()
    {
        var users = new FakeAppUserReadRepository { User = User() };
        var permissions = new FakeUserPermissionRepository();
        permissions.Rows.Add(new EffectivePermissionReadModel { MenuCode = "Farm", PermissionCode = "View" });
        var service = new CurrentUserProfileService(users, new PermissionService(permissions));

        var result = await service.GetAsync(42);

        Assert.True(result.IsSuccess);
        var me = result.Value;
        Assert.Equal(42, me.Id);
        Assert.Equal("jsmith", me.Username);
        Assert.Equal("Jane", me.FirstName);
        Assert.Equal("Smith", me.LastName);
        Assert.Equal("jane@example.test", me.Email);
        Assert.True(me.PasswordChangeRequired);
        Assert.Equal(("Farm", "View"), (me.Permissions[0].MenuCode, me.Permissions[0].PermissionCode));
    }

    [Fact]
    public async Task Missing_inactive_or_locked_users_are_Unauthorized()
    {
        var permissions = new PermissionService(new FakeUserPermissionRepository());

        foreach (var user in new AppUserProfileReadModel?[] { null, User(active: false), User(locked: true) })
        {
            var service = new CurrentUserProfileService(new FakeAppUserReadRepository { User = user }, permissions);

            var result = await service.GetAsync(42);

            Assert.Same(AuthErrors.SessionInvalid, result.Error);
        }
    }

    [Fact]
    public void The_response_type_has_no_password_members()
    {
        var names = typeof(CurrentUserResponse).GetProperties().Select(p => p.Name);

        Assert.DoesNotContain(names, n => n.Contains("Password", StringComparison.OrdinalIgnoreCase) && n != "PasswordChangeRequired");
        Assert.DoesNotContain(typeof(AppUserProfileReadModel).GetProperties(), p => p.Name is "EncPassword" or "PasswordNonce" or "PasswordTag" or "KeyVersion");
    }
}

public class AuthenticationContractTests
{
    [Fact]
    public void Policy_constants_follow_the_specification()
    {
        Assert.Equal(TimeSpan.FromMinutes(30), SessionPolicy.IdleTimeout);
        Assert.Equal(30, SessionPolicy.IdleTimeoutMinutes);
        Assert.Equal(5, SessionPolicy.MaxFailedLoginAttempts);
        Assert.Equal((byte)1, SessionPolicy.PlatformWeb);
        Assert.Equal(43, SessionPolicy.RefreshTokenLength);
    }

    [Fact]
    public void Service_errors_match_the_specification()
    {
        Assert.Equal(("Auth.InvalidCredentials", ErrorType.Unauthorized, "Invalid username or password."),
            (AuthErrors.InvalidCredentials.Code, AuthErrors.InvalidCredentials.Type, AuthErrors.InvalidCredentials.Message));
        Assert.Equal(("Auth.PlatformAccessDenied", ErrorType.Forbidden, "Your account does not have access to this application."),
            (AuthErrors.PlatformAccessDenied.Code, AuthErrors.PlatformAccessDenied.Type, AuthErrors.PlatformAccessDenied.Message));
        Assert.Equal(("Auth.InvalidRefreshToken", ErrorType.Unauthorized, "Your session is no longer valid. Please sign in again."),
            (AuthErrors.InvalidRefreshToken.Code, AuthErrors.InvalidRefreshToken.Type, AuthErrors.InvalidRefreshToken.Message));
    }

    [Fact]
    public void SessionInvalid_equals_the_catalogue_entry_50001()
    {
        var catalogue = FarmFMS.Infrastructure.Data.StoredProcedureErrors.Catalog[50001];

        Assert.Equal(catalogue.Code, AuthErrors.SessionInvalid.Code);
        Assert.Equal(catalogue.Message, AuthErrors.SessionInvalid.Message);
        Assert.Equal(catalogue.Type, AuthErrors.SessionInvalid.Type);
    }

    [Fact]
    public void ClientContext_truncates_trims_and_nulls_blank_values()
    {
        var context = new ClientContext("  " + new string('1', 60) + "  ", new string('x', 600));

        Assert.Equal(45, context.IPAddress!.Length);
        Assert.Equal(500, context.DeviceInfo!.Length);
        Assert.Null(new ClientContext(" ", null).IPAddress);
        Assert.Null(new ClientContext(" ", "").DeviceInfo);
        Assert.Equal("1.2.3.4", new ClientContext(" 1.2.3.4 ", null).IPAddress);
    }

    [Fact]
    public void Request_DTOs_carry_the_specified_validation()
    {
        static T Attr<T>(Type type, string property) where T : Attribute =>
            (T)Attribute.GetCustomAttribute(type.GetProperty(property)!, typeof(T))!;

        Assert.Equal(100, Attr<System.ComponentModel.DataAnnotations.StringLengthAttribute>(typeof(LoginRequest), "Username").MaximumLength);
        Assert.Equal(128, Attr<System.ComponentModel.DataAnnotations.StringLengthAttribute>(typeof(LoginRequest), "Password").MaximumLength);
        foreach (var type in new[] { typeof(RefreshRequest), typeof(LogoutRequest) })
        {
            var length = Attr<System.ComponentModel.DataAnnotations.StringLengthAttribute>(type, "RefreshToken");
            Assert.Equal((43, 43), (length.MinimumLength, length.MaximumLength));
            Assert.NotNull(Attr<System.ComponentModel.DataAnnotations.RequiredAttribute>(type, "RefreshToken"));
        }
    }

    [Theory]
    [InlineData("Abcdef1!", 0)]
    [InlineData("abcdef1!", 1)] // no uppercase
    [InlineData("ABCDEF1!", 1)] // no lowercase
    [InlineData("Abcdefg!", 1)] // no digit
    [InlineData("Abcdefg1", 1)] // no special
    [InlineData("Ab1!", 1)] // too short
    [InlineData("", 5)] // short + upper + lower + digit + special
    public void Password_policy_requires_length_upper_lower_digit_and_special(string password, int expectedProblems)
    {
        Assert.Equal(expectedProblems, PasswordPolicy.Validate(password).Count);
    }

    [Fact]
    public void Password_policy_enforces_the_maximum_length_and_never_echoes_the_password()
    {
        var tooLong = "Aa1!" + new string('x', 125);

        var problems = PasswordPolicy.Validate(tooLong);

        Assert.Single(problems);
        Assert.DoesNotContain(tooLong, string.Join(' ', problems));
        Assert.Empty(PasswordPolicy.Validate("Aa1!" + new string('x', 124))); // exactly 128
    }

    [Fact]
    public void Password_policy_counts_the_NFC_form()
    {
        // 8 characters composed, 9 decomposed: both satisfy the minimum after normalization.
        Assert.Empty(PasswordPolicy.Validate("Café-1xY"));
    }
}
