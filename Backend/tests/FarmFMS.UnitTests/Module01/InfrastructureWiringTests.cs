using Dapper;
using FarmFMS.Application;
using FarmFMS.Application.Abstractions.Data;
using FarmFMS.Application.Abstractions.Identity;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Application.Module01.Authorization;
using FarmFMS.Application.Module01.Users;
using FarmFMS.Domain.Module01.Authentication;
using FarmFMS.Infrastructure;
using FarmFMS.Infrastructure.Data;
using FarmFMS.Infrastructure.Identity;
using FarmFMS.Infrastructure.Repositories.Module01;
using FarmFMS.UnitTests.Module01.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FarmFMS.UnitTests.Module01;

/// <summary>Repository contract (procedure names, parameter names, null handling), System user resolution and DI lifetimes.</summary>
public class InfrastructureWiringTests
{
    private sealed class RecordingExecutor : IStoredProcedureExecutor
    {
        public List<(string Procedure, DynamicParameters? Parameters)> Calls { get; } = [];
        public object? QueryResult { get; set; }
        public int NewId { get; set; } = 11;
        public int Rows { get; set; } = 1;
        public Dictionary<string, object?> Outputs { get; } = [];

        private DynamicParameters Record(string procedure, object? parameters)
        {
            var dynamic = parameters as DynamicParameters ?? throw new InvalidOperationException("DynamicParameters expected");
            Calls.Add((procedure, dynamic));
            foreach (var (name, value) in Outputs)
                dynamic.Add(name, value);
            return dynamic;
        }

        public Task<IReadOnlyList<T>> QueryAsync<T>(string procedureName, object? parameters = null, CancellationToken cancellationToken = default)
        {
            Record(procedureName, parameters);
            return Task.FromResult((IReadOnlyList<T>)(QueryResult as IReadOnlyList<T> ?? []));
        }

        public Task<T?> QuerySingleOrDefaultAsync<T>(string procedureName, object? parameters = null, CancellationToken cancellationToken = default)
        {
            Record(procedureName, parameters);
            return Task.FromResult(QueryResult is T value ? value : default);
        }

        public Task<(IReadOnlyList<T1> First, IReadOnlyList<T2> Second)> QueryMultipleAsync<T1, T2>(string procedureName, object? parameters = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TResult> QueryMultipleAsync<TResult>(string procedureName, Func<SqlMapper.GridReader, Task<TResult>> read, object? parameters = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> ExecuteAsync(string procedureName, object? parameters = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> ExecuteForNewIdAsync(string procedureName, object? parameters = null, CancellationToken cancellationToken = default)
        {
            Record(procedureName, parameters);
            return Task.FromResult(NewId);
        }

        public Task<int> ExecuteForRowsAffectedAsync(string procedureName, object? parameters = null, CancellationToken cancellationToken = default)
        {
            Record(procedureName, parameters);
            return Task.FromResult(Rows);
        }

        public Task<T?> ExecuteScalarAsync<T>(string procedureName, object? parameters = null, CancellationToken cancellationToken = default)
        {
            Record(procedureName, parameters);
            return Task.FromResult(QueryResult is T value ? value : default);
        }
    }

    private static string[] Names(RecordingExecutor executor) =>
        [.. executor.Calls[0].Parameters!.ParameterNames.Order(StringComparer.Ordinal)];

    #region Repositories

    [Fact]
    public async Task Login_repository_uses_the_specified_procedures_and_parameters()
    {
        var executor = new RecordingExecutor { QueryResult = new LoginCredentialReadModel { UserId = 1 } };
        var repository = new LoginRepository(executor);

        Assert.NotNull(await repository.GetCredentialsAsync("jsmith"));
        Assert.Equal("dbo.SvaAppUserLogin", executor.Calls[0].Procedure);
        Assert.Equal(["Username"], Names(executor));

        executor.Calls.Clear();
        await repository.RecordLoginSuccessAsync(5);
        Assert.Equal("dbo.SupAppUsrLoginSuccess", executor.Calls[0].Procedure);
        Assert.Equal(["UserId"], Names(executor));

        executor.Calls.Clear();
        executor.Outputs["IsLocked"] = true;
        var outcome = await repository.RecordLoginFailureAsync(5, 5);
        Assert.Equal("dbo.SupAppUsrLoginFailure", executor.Calls[0].Procedure);
        Assert.Contains("MaxFailedAttempts", Names(executor));
        Assert.Equal(new LoginFailureOutcome(1, true), outcome);
    }

    [Fact]
    public async Task Session_repository_maps_N6_status_rows_and_fails_closed()
    {
        var executor = new RecordingExecutor();
        var repository = new UserSessionRepository(executor);

        // No row must never read as 0 = Valid.
        executor.QueryResult = null;
        Assert.Equal(SessionValidationStatus.NotFoundOrUserMismatch, await repository.ValidateAsync(1, 2, 30, 60, 1, 7));

        executor.QueryResult = (byte?)0;
        Assert.Equal(SessionValidationStatus.Valid, await repository.ValidateAsync(1, 2, 30, 60, 1, 7));

        executor.QueryResult = (byte?)4;
        Assert.Equal(SessionValidationStatus.Idle, await repository.ValidateAsync(1, 2, 30, 60, 1, 7));

        executor.QueryResult = (byte?)99;
        Assert.Equal(SessionValidationStatus.NotFoundOrUserMismatch, await repository.ValidateAsync(1, 2, 30, 60, 1, 7));

        Assert.Equal("dbo.SvaUsrSess", executor.Calls[0].Procedure);
        Assert.Equal(
            ["IdleTimeoutMinutes", "ModifiedBy", "Platform", "SessionId", "TouchIntervalSeconds", "UserId"],
            executor.Calls[0].Parameters!.ParameterNames.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Session_repository_create_returns_the_id_and_a_UTC_expiry()
    {
        var executor = new RecordingExecutor { NewId = 77 };
        executor.Outputs["ExpiresDate"] = new DateTime(2026, 11, 13, 6, 0, 0, DateTimeKind.Unspecified);
        var repository = new UserSessionRepository(executor);

        var created = await repository.CreateAsync(1, new byte[32], 720, "10.0.0.1", "agent", 1);

        Assert.Equal(77, created.SessionId);
        Assert.Equal(DateTimeKind.Utc, created.ExpiresUtc.Kind);
        Assert.Equal("dbo.ScrUsrSess", executor.Calls[0].Procedure);
        Assert.Equal(
            ["AbsoluteLifetimeMinutes", "CreatedBy", "DeviceInfo", "ExpiresDate", "IPAddress", "TokenHash", "UserId"],
            executor.Calls[0].Parameters!.ParameterNames.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Session_repository_rotate_revoke_and_lookup_use_the_specified_procedures()
    {
        var executor = new RecordingExecutor();
        var repository = new UserSessionRepository(executor);

        await repository.GetByTokenHashAsync(new byte[32], 30);
        await repository.RotateAsync(1, new byte[32], 30, 1, null, null, 9);
        await repository.RevokeAsync(9, null, "Logout", 9);

        Assert.Equal(
            ["dbo.SreUsrSessByTokenHash", "dbo.ScrUsrSessRotate", "dbo.SupUserSessRev"],
            executor.Calls.Select(c => c.Procedure));
        Assert.Equal(["IdleTimeoutMinutes", "TokenHash"], executor.Calls[0].Parameters!.ParameterNames.Order(StringComparer.Ordinal));
        Assert.Equal(
            ["CreatedBy", "CurrentSessionId", "DeviceInfo", "IPAddress", "IdleTimeoutMinutes", "NewTokenHash", "Platform"],
            executor.Calls[1].Parameters!.ParameterNames.Order(StringComparer.Ordinal));
        Assert.Equal(["ModifiedBy", "RevokedReason", "SessionId", "UserId"], executor.Calls[2].Parameters!.ParameterNames.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task History_permission_and_profile_repositories_use_the_specified_procedures()
    {
        var executor = new RecordingExecutor();

        await new LoginHistoryRepository(executor).CreateAsync(null, "ghost", LoginEventType.Failed, null, null, "UnknownUser", 7);
        await new UserPermissionRepository(executor).GetEffectivePermissionsAsync(1, 1);
        await new AppUserReadRepository(executor).GetByIdAsync(1);

        Assert.Equal(["dbo.ScrLoginHist", "dbo.SvaUsrPerm", "dbo.SreAppUserById"], executor.Calls.Select(c => c.Procedure));
        Assert.Equal(
            ["CreatedBy", "DeviceInfo", "EventType", "FailureReason", "IPAddress", "UserId", "Username"],
            executor.Calls[0].Parameters!.ParameterNames.Order(StringComparer.Ordinal));
        Assert.Equal(["Platform", "UserId"], executor.Calls[1].Parameters!.ParameterNames.Order(StringComparer.Ordinal));
        Assert.Equal(["Id"], executor.Calls[2].Parameters!.ParameterNames.ToArray());
    }

    [Fact]
    public async Task Bootstrap_repository_uses_the_specified_procedures_and_never_reserves_output_names()
    {
        var executor = new RecordingExecutor();
        var repository = new AdministratorBootstrapRepository(executor);
        var payload = new EncryptedPassword([1], new byte[12], new byte[16], "v1");

        await repository.GetSystemUserIdAsync("SYSTEM");
        await repository.CreateUserAsync(new BootstrapUserData("admin", "First", null, null, payload, false, true, 7));
        await repository.SetPasswordAsync(1, payload, false, 7);
        await repository.CreateUserAccountAsync(1, 3, true, false, 7);

        Assert.Equal(
            ["dbo.SreAppUsrSystemId", "dbo.ScrAppUser", "dbo.SupAppUsrPwd", "dbo.ScrUserAccount"],
            executor.Calls.Select(c => c.Procedure));
        Assert.Equal(
            ["CreatedBy", "Email", "EncPassword", "FirstName", "IsActive", "KeyVersion", "LastName", "MobileNo", "PasswordNonce", "PasswordTag", "PwdChangeReq", "Username"],
            executor.Calls[1].Parameters!.ParameterNames.Order(StringComparer.Ordinal));
        Assert.Equal(
            ["EncPassword", "KeyVersion", "ModifiedBy", "PasswordNonce", "PasswordTag", "PwdChangeReq", "UserId"],
            executor.Calls[2].Parameters!.ParameterNames.Order(StringComparer.Ordinal));
        Assert.Equal(["CanAccessMobile", "CanAccessWeb", "CreatedBy", "ProcessId", "UserId"], executor.Calls[3].Parameters!.ParameterNames.Order(StringComparer.Ordinal));
        Assert.DoesNotContain(executor.Calls.SelectMany(c => c.Parameters!.ParameterNames), n => n is "NewId" or "RowsAffected");
    }

    #endregion

    #region System user provider

    private sealed class CountingBootstrapRepository(CallLog log) : IAdministratorBootstrapRepository
    {
        public int? Id { get; set; }
        public int Lookups { get; private set; }

        public Task<int?> GetSystemUserIdAsync(string username, CancellationToken cancellationToken = default)
        {
            Lookups++;
            log.Add(username);
            return Task.FromResult(Id);
        }

        public Task<int> CreateUserAsync(BootstrapUserData user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> SetPasswordAsync(int userId, EncryptedPassword payload, bool passwordChangeRequired, int modifiedBy, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> CreateUserAccountAsync(int userId, int processId, bool canAccessWeb, bool canAccessMobile, int createdBy, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [Fact]
    public async Task System_user_id_is_resolved_by_the_configured_username_and_cached_when_found()
    {
        var log = new CallLog();
        var repository = new CountingBootstrapRepository(log) { Id = 123 };
        var cache = new SystemUserIdCache();

        // Two scopes share the singleton cache.
        var first = new DatabaseSystemUserProvider(cache, repository, new FakeSystemUserSettings("BATCH"));
        var second = new DatabaseSystemUserProvider(cache, repository, new FakeSystemUserSettings("BATCH"));

        Assert.Equal(123, await first.GetSystemUserIdAsync());
        Assert.Equal(123, await second.GetSystemUserIdAsync());
        Assert.Equal(1, repository.Lookups);
        Assert.Equal(["BATCH"], log);
    }

    [Fact]
    public async Task A_missing_System_user_is_not_cached()
    {
        var repository = new CountingBootstrapRepository(new CallLog()) { Id = null };
        var provider = new DatabaseSystemUserProvider(new SystemUserIdCache(), repository, new FakeSystemUserSettings());

        Assert.Null(await provider.GetSystemUserIdAsync());
        Assert.Null(await provider.GetSystemUserIdAsync());
        repository.Id = 9; // the bootstrap created it meanwhile

        Assert.Equal(9, await provider.GetSystemUserIdAsync());
        Assert.Equal(3, repository.Lookups);
    }

    [Fact]
    public async Task A_non_positive_lookup_result_is_treated_as_missing()
    {
        var repository = new CountingBootstrapRepository(new CallLog()) { Id = 0 };
        var provider = new DatabaseSystemUserProvider(new SystemUserIdCache(), repository, new FakeSystemUserSettings());

        Assert.Null(await provider.GetSystemUserIdAsync());
    }

    [Fact]
    public async Task The_cache_is_thread_safe()
    {
        var cache = new SystemUserIdCache();

        await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => Task.Run(() => cache.Set(5))));

        Assert.True(cache.TryGet(out var id));
        Assert.Equal(5, id);
    }

    #endregion

    #region Dependency injection

    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables() // the throwaway keys set by TestEnvironment
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:FarmFMS"] = TestEnvironment.PlaceholderConnectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<ICurrentUserService, NoUser>();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        // Validates that no singleton captures a scoped service and that every dependency is registered.
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    private sealed class NoUser : ICurrentUserService
    {
        public bool IsAuthenticated => false;
        public int? UserId => null;
        public int? SessionId => null;
        public string? UserName => null;
    }

    [Fact]
    public void Lifetimes_are_valid_and_every_Step0_service_resolves()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        Assert.NotNull(sp.GetRequiredService<IAuthenticationService>());
        Assert.NotNull(sp.GetRequiredService<ISessionValidationService>());
        Assert.NotNull(sp.GetRequiredService<IPermissionService>());
        Assert.NotNull(sp.GetRequiredService<ICurrentUserProfileService>());
        Assert.NotNull(sp.GetRequiredService<IAdministratorBootstrapService>());
        Assert.NotNull(sp.GetRequiredService<ISystemUserProvider>());
        Assert.NotNull(sp.GetRequiredService<IPasswordProtector>());
        Assert.NotNull(sp.GetRequiredService<IAccessTokenIssuer>());
        Assert.NotNull(sp.GetRequiredService<ISessionTokenFactory>());
        Assert.NotNull(sp.GetRequiredService<ISessionSettings>());
        Assert.NotNull(sp.GetRequiredService<ISystemUserSettings>());
        Assert.NotNull(sp.GetRequiredService<IUnitOfWork>());
    }

    [Fact]
    public void The_permission_service_is_scoped_and_the_security_services_are_singletons()
    {
        using var provider = BuildProvider();

        using (var first = provider.CreateScope())
        using (var second = provider.CreateScope())
        {
            Assert.NotSame(first.ServiceProvider.GetRequiredService<IPermissionService>(), second.ServiceProvider.GetRequiredService<IPermissionService>());
            Assert.NotSame(first.ServiceProvider.GetRequiredService<ISystemUserProvider>(), second.ServiceProvider.GetRequiredService<ISystemUserProvider>());
            Assert.Same(first.ServiceProvider.GetRequiredService<IPasswordProtector>(), second.ServiceProvider.GetRequiredService<IPasswordProtector>());
            Assert.Same(first.ServiceProvider.GetRequiredService<SystemUserIdCache>(), second.ServiceProvider.GetRequiredService<SystemUserIdCache>());
        }
    }

    [Fact]
    public void The_registered_protector_round_trips_with_the_configured_test_keys()
    {
        using var provider = BuildProvider();
        var protector = provider.GetRequiredService<IPasswordProtector>();

        var payload = protector.Protect("Sup3r-Secret!", 5);

        Assert.Equal(PasswordVerificationResult.Match, protector.Verify("Sup3r-Secret!", payload, 5));
    }

    #endregion
}
