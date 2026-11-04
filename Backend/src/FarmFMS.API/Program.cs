using FarmFMS.API;
using FarmFMS.Application;
using FarmFMS.Infrastructure;
using FarmFMS.Infrastructure.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddInfrastructureHealthChecks();

var app = builder.Build();

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
app.UseHttpLogging();

app.UseAuthorization();

app.MapControllers();
// Health: liveness (process up, no dependencies) and readiness (SQL Server reachable).
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = check => !check.Tags.Contains(HealthCheckTags.Ready) });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(HealthCheckTags.Ready) });

app.Run();

/// <summary>
/// Entry point; public partial so integration tests can host the API with WebApplicationFactory.
/// </summary>
public partial class Program;
