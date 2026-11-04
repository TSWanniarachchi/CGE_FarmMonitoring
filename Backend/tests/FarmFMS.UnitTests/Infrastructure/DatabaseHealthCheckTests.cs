using System.Data.Common;
using FarmFMS.Application.Abstractions.Data;
using FarmFMS.Infrastructure.HealthChecks;
using FarmFMS.UnitTests.Infrastructure.Fakes;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace FarmFMS.UnitTests.Infrastructure;

public class DatabaseHealthCheckTests
{
    private static HealthCheckContext Context(IHealthCheck check) => new()
    {
        Registration = new HealthCheckRegistration(DatabaseHealthCheck.Name, check, HealthStatus.Unhealthy, [HealthCheckTags.Ready])
    };

    [Fact]
    public async Task Healthy_when_the_connection_opens_and_the_connection_is_disposed()
    {
        var factory = new FakeConnectionFactory();
        var check = new DatabaseHealthCheck(factory, NullLogger<DatabaseHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(Context(check));

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.True(Assert.Single(factory.Created).IsDisposed);
    }

    [Fact]
    public async Task Unhealthy_without_exposing_the_exception_when_the_connection_fails()
    {
        var check = new DatabaseHealthCheck(
            new FailingConnectionFactory(new InvalidOperationException("Connection string contains Password=secret")),
            NullLogger<DatabaseHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(Context(check));

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Null(result.Exception);
        Assert.DoesNotContain("secret", result.Description);
    }

    [Fact]
    public async Task Cancellation_is_not_reported_as_a_database_failure()
    {
        var check = new DatabaseHealthCheck(
            new FailingConnectionFactory(new OperationCanceledException()),
            NullLogger<DatabaseHealthCheck>.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(() => check.CheckHealthAsync(Context(check)));
    }

    private sealed class FailingConnectionFactory(Exception exception) : IDbConnectionFactory
    {
        public Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<DbConnection>(exception);
    }
}
