using System.Data.Common;
using System.Net;
using System.Text.Json;
using FarmFMS.Application.Abstractions.Data;
using FarmFMS.UnitTests.Infrastructure.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace FarmFMS.UnitTests.Api;

/// <summary>
/// Hosts the API in memory. No database connection is needed: nothing in the pipeline touches SQL Server at startup.
/// </summary>
public class ApiHostTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Health_endpoint_returns_200()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Liveness_does_not_depend_on_the_database()
    {
        using var client = WithConnectionFactory(new UnreachableConnectionFactory()).CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_returns_200_when_the_database_is_reachable()
    {
        using var client = WithConnectionFactory(new FakeConnectionFactory()).CreateClient();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Readiness_returns_503_without_details_when_the_database_is_unreachable()
    {
        using var client = WithConnectionFactory(new UnreachableConnectionFactory()).CreateClient();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    private WebApplicationFactory<Program> WithConnectionFactory(IDbConnectionFactory connectionFactory) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton(connectionFactory)));

    private sealed class UnreachableConnectionFactory : IDbConnectionFactory
    {
        public Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<DbConnection>(new InvalidOperationException("Server unreachable"));
    }

    [Fact]
    public async Task Unknown_route_returns_ProblemDetails_with_traceId()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
        Assert.True(body.RootElement.TryGetProperty("traceId", out _));
    }
}
