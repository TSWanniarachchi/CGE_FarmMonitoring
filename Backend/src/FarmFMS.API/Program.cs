using FarmFMS.API;
using FarmFMS.API.Common.Health;
using FarmFMS.Application;
using FarmFMS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Layers: each registers its own services; Program.cs only composes them and builds the pipeline.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi();

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
app.MapHealthEndpoints();

app.Run();

/// <summary>
/// Entry point; public partial so integration tests can host the API with WebApplicationFactory.
/// </summary>
public partial class Program;
