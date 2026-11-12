using System.Collections.Concurrent;
using FarmFMS.API.Common.Security;
using FarmFMS.Application.Abstractions.Data;
using FarmFMS.Application.Abstractions.Identity;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Application.Module01.Authorization;
using FarmFMS.Application.Module01.Users;
using FarmFMS.UnitTests.Module01.Fakes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace FarmFMS.UnitTests.Module01.Api;

/// <summary>Captures every log message (formatted text and exception text) of the host.</summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<(LogLevel Level, string Category, string Text)> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CategoryLogger(this, categoryName);

    public void Dispose()
    {
    }

    public string AllText => string.Join('\n', Entries.Select(e => $"{e.Level} {e.Category}: {e.Text}"));

    private sealed class CategoryLogger(CapturingLoggerProvider owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            owner.Entries.Enqueue((logLevel, category, formatter(state, exception) + (exception is null ? "" : "\n" + exception)));
    }
}

internal sealed class StubSystemUserProvider : ISystemUserProvider
{
    public int? Id { get; set; } = 7;

    public ValueTask<int?> GetSystemUserIdAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(Id);
}

/// <summary>Test-only controller: no [AllowAnonymous], so the secure-by-default rule must protect it.</summary>
[ApiController]
[Route("api/test-probe")]
public sealed class SecureProbeController : ControllerBase
{
    [HttpGet("secure")]
    public IActionResult Secure() => Ok("secure");

    [HttpGet("farm-view")]
    [Authorize(Policy = "Perm.Farm.View")]
    public IActionResult FarmView() => Ok("farm-view");
}

/// <summary>
/// The API in memory with fake repositories (no database) and the throwaway keys from <see cref="TestEnvironment"/>.
/// </summary>
internal sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _settings;

    public AuthApiFactory(Dictionary<string, string?>? settings = null)
    {
        _settings = settings ?? [];
        Login = new FakeLoginRepository(Log);
        Sessions = new FakeUserSessionRepository(Log);
        History = new FakeLoginHistoryRepository(Log);
        UnitOfWork = new FakeUnitOfWork(Log);
    }

    public CallLog Log { get; } = new();
    public FakeLoginRepository Login { get; }
    public FakeUserSessionRepository Sessions { get; }
    public FakeLoginHistoryRepository History { get; }
    public FakeUnitOfWork UnitOfWork { get; }
    public FakeUserPermissionRepository Permissions { get; } = new();
    public FakeAppUserReadRepository Users { get; } = new();
    public StubSystemUserProvider SystemUser { get; } = new();
    public CapturingLoggerProvider Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_settings));

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(Logs);
            logging.SetMinimumLevel(LogLevel.Trace);
        });

        builder.ConfigureTestServices(services =>
        {
            Replace<ILoginRepository>(services, Login);
            Replace<IUserSessionRepository>(services, Sessions);
            Replace<ILoginHistoryRepository>(services, History);
            Replace<IUserPermissionRepository>(services, Permissions);
            Replace<IAppUserReadRepository>(services, Users);
            Replace<IUnitOfWork>(services, UnitOfWork);
            Replace<ISystemUserProvider>(services, SystemUser);

            services.AddControllers().AddApplicationPart(typeof(SecureProbeController).Assembly);
            services.AddAuthorizationBuilder().AddPermissionPolicy("Farm", "View");
        });
    }

    private static void Replace<T>(IServiceCollection services, T instance) where T : class
    {
        services.RemoveAll<T>();
        services.AddSingleton(instance);
    }
}
