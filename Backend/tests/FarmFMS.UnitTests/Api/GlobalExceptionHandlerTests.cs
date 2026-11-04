using System.Text.Json;
using FarmFMS.API.Common.Errors;
using FarmFMS.Application.Common.Exceptions;
using FarmFMS.Application.Common.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FarmFMS.UnitTests.Api;

public class GlobalExceptionHandlerTests
{
    private static async Task<(HttpContext Context, JsonElement? Body)> HandleAsync(Exception exception, bool aborted = false)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";
        context.Request.Headers.Accept = "application/json";
        context.Response.Body = new MemoryStream();

        if (aborted)
        {
            var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            context.RequestAborted = cancelled.Token;
        }

        var handler = new GlobalExceptionHandler(
            services.GetRequiredService<Microsoft.AspNetCore.Http.IProblemDetailsService>(),
            NullLogger<GlobalExceptionHandler>.Instance);

        Assert.True(await handler.TryHandleAsync(context, exception, CancellationToken.None));

        context.Response.Body.Position = 0;
        if (context.Response.Body.Length == 0) return (context, null);

        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return (context, document.RootElement.Clone());
    }

    [Fact]
    public async Task DataAccessException_uses_its_status_code_and_user_safe_message()
    {
        var exception = new DataAccessException(
            Error.Conflict("Database.Duplicate", "A record with the same unique value already exists."),
            databaseErrorNumber: 2601,
            isTransient: false);

        var (context, body) = await HandleAsync(exception);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("Database.Duplicate", body!.Value.GetProperty("code").GetString());
        Assert.Equal("A record with the same unique value already exists.", body.Value.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Unexpected_exception_returns_500_without_internal_details()
    {
        var (context, body) = await HandleAsync(new InvalidOperationException("Connection string contains Password=secret"));

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("Server.Unexpected", body!.Value.GetProperty("code").GetString());
        Assert.DoesNotContain("secret", body.Value.GetRawText());
    }

    [Fact]
    public async Task Client_cancellation_returns_499_without_a_body()
    {
        var (context, body) = await HandleAsync(new OperationCanceledException(), aborted: true);

        Assert.Equal(499, context.Response.StatusCode);
        Assert.Null(body);
    }

    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Unavailable, 503)]
    [InlineData(ErrorType.Failure, 500)]
    public void Error_types_map_to_status_codes(ErrorType type, int statusCode)
    {
        Assert.Equal(statusCode, ErrorStatusCodes.For(type));
    }
}
