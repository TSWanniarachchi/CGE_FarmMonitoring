using FarmFMS.Application.Abstractions.Identity;
using FarmFMS.Application.Common.Identity;
using FarmFMS.Application.Common.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace FarmFMS.UnitTests.Application;

public class AuditActorServiceTests
{
    private sealed class StubCurrentUser(bool isAuthenticated, int? userId) : ICurrentUserService
    {
        public bool IsAuthenticated { get; } = isAuthenticated;
        public int? UserId { get; } = userId;
        public int? SessionId => null;
        public string? UserName => IsAuthenticated ? "user" : null;
    }

    private sealed class StubSystemUser(int? id) : ISystemUserProvider
    {
        public ValueTask<int?> GetSystemUserIdAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(id);
    }

    private static AuditActorService Create(ICurrentUserService currentUser, int? systemUserId) =>
        new(currentUser, new StubSystemUser(systemUserId), NullLogger<AuditActorService>.Instance);

    [Fact]
    public async Task Authenticated_request_uses_the_authenticated_user_id()
    {
        var service = Create(new StubCurrentUser(true, 42), systemUserId: 1);

        var result = await service.GetActorIdAsync();

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task Unauthenticated_request_is_rejected()
    {
        var service = Create(new StubCurrentUser(false, null), systemUserId: 1);

        var result = await service.GetActorIdAsync();

        Assert.Same(AuditErrors.ActorRequired, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task Authenticated_user_without_a_valid_id_is_rejected()
    {
        var service = Create(new StubCurrentUser(true, null), systemUserId: 1);

        Assert.Same(AuditErrors.ActorRequired, (await service.GetActorIdAsync()).Error);
    }

    [Fact]
    public async Task System_operation_uses_the_configured_System_user()
    {
        var service = Create(new StubCurrentUser(false, null), systemUserId: 7);

        using (service.BeginSystemOperation("NightlyCleanup"))
        {
            Assert.Equal(7, (await service.GetActorIdAsync()).Value);
        }

        // Back to normal rules once the scope is disposed.
        Assert.Same(AuditErrors.ActorRequired, (await service.GetActorIdAsync()).Error);
    }

    [Fact]
    public async Task System_operation_fails_closed_when_the_System_user_is_not_configured()
    {
        var service = Create(new StubCurrentUser(false, null), systemUserId: null);

        using var _ = service.BeginSystemOperation("NightlyCleanup");

        var result = await service.GetActorIdAsync();
        Assert.Same(AuditErrors.SystemUserNotConfigured, result.Error);
    }

    [Fact]
    public async Task Nested_system_operations_end_with_the_outer_scope()
    {
        var service = Create(new StubCurrentUser(false, null), systemUserId: 7);

        var outer = service.BeginSystemOperation("Outer");
        var inner = service.BeginSystemOperation("Inner");
        inner.Dispose();
        inner.Dispose(); // idempotent

        Assert.Equal(7, (await service.GetActorIdAsync()).Value);

        outer.Dispose();
        Assert.True((await service.GetActorIdAsync()).IsFailure);
    }

    [Fact]
    public void System_operation_start_and_end_are_logged_at_Debug_only()
    {
        var logger = new FarmFMS.UnitTests.Module01.Fakes.ListLogger<AuditActorService>();
        var service = new AuditActorService(new StubCurrentUser(false, null), new StubSystemUser(7), logger);

        service.BeginSystemOperation("Login").Dispose();

        Assert.Equal(2, logger.Entries.Count);
        Assert.All(logger.Entries, e => Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Debug, e.Level));
    }

    [Fact]
    public void System_operation_requires_a_name()
    {
        var service = Create(new StubCurrentUser(false, null), systemUserId: 7);

        Assert.Throws<ArgumentException>(() => service.BeginSystemOperation(" "));
    }
}
