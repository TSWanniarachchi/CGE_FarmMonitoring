using FarmFMS.API;
using FarmFMS.API.Commands;
using FarmFMS.API.Common.Health;
using FarmFMS.Application;
using FarmFMS.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Layers: each registers its own services; Program.cs only composes them and builds the pipeline.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi();

var app = builder.Build();

// Console command instead of the web host (Development only; refused elsewhere). Never runs at normal startup:
//   dotnet run --project Backend/src/FarmFMS.API -- bootstrap-admin
if (BootstrapAdminCommand.IsRequested(args))
{
    await using var scope = app.Services.CreateAsyncScope();
    return await scope.ServiceProvider.GetRequiredService<BootstrapAdminCommand>().RunAsync();
}

// Pipeline order (Step 0 spec 8.2). Authentication and authorization must come after rate limiting and forwarded headers.

// Errors: unhandled exceptions and empty error responses (404, 405, ...) become ProblemDetails.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // OpenAPI document (/openapi/v1.json) and Swagger UI (/swagger), which renders that same document.
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Farm FMS API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Farm FMS API";
    });
}

app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

// Client IP: X-Forwarded-For is honoured only from the configured proxies (ForwardedHeaders:KnownProxies, default none).
app.UseForwardedHeaders();

// Request logging: method, path, status and duration only. Headers (Authorization) and bodies are never logged.
app.UseHttpLogging();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Secure by default (AD-13): every controller endpoint requires an authenticated user with a valid session unless it is
// explicitly [AllowAnonymous] (login, refresh, logout). Health endpoints are mapped anonymous below.
app.MapControllers().RequireAuthorization();
// Health: liveness (process up, no dependencies) and readiness (SQL Server reachable).
app.MapHealthEndpoints();

app.Run();
return 0;

/// <summary>
/// Entry point; public partial so integration tests can host the API with WebApplicationFactory.
/// </summary>
public partial class Program;
