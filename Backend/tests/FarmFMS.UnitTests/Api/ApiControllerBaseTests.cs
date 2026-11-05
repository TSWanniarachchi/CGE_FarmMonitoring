using FarmFMS.API.Controllers;
using FarmFMS.Application.Common.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FarmFMS.UnitTests.Api;

/// <summary>
/// Result → HTTP mapping of <see cref="ApiControllerBase"/> (AD-15, AD-18), using the real MVC ProblemDetailsFactory.
/// </summary>
public class ApiControllerBaseTests
{
    private sealed class TestController : ApiControllerBase
    {
        public ActionResult<T> Ok<T>(Result<T> result) => OkOrProblem(result);

        public ActionResult NoContent(Result result) => NoContentOrProblem(result);

        public ActionResult<CreatedResponse> Created(Result<int> result) => CreatedOrProblem(result, "GetById");
    }

    private static TestController CreateController()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        services.AddProblemDetails();

        return new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() }
            }
        };
    }

    private static ProblemDetails AssertProblem(object actionResult, int expectedStatus, string expectedCode)
    {
        var converted = actionResult is IConvertToActionResult convertible ? convertible.Convert() : (IActionResult)actionResult;
        var objectResult = Assert.IsType<ObjectResult>(converted);
        Assert.Equal(expectedStatus, objectResult.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(objectResult.Value, exactMatch: false);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.Equal(expectedCode, problem.Extensions["code"]);
        return problem;
    }

    [Fact]
    public void OkOrProblem_returns_200_with_the_value()
    {
        var result = CreateController().Ok(Result.Success("farm"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("farm", ok.Value);
    }

    [Fact]
    public void NoContentOrProblem_returns_204()
    {
        Assert.IsType<NoContentResult>(CreateController().NoContent(Result.Success()));
    }

    public static TheoryData<ErrorType, int> StatusByErrorType => new()
    {
        { ErrorType.Validation, StatusCodes.Status400BadRequest },
        { ErrorType.Unauthorized, StatusCodes.Status401Unauthorized },
        { ErrorType.Forbidden, StatusCodes.Status403Forbidden },
        { ErrorType.NotFound, StatusCodes.Status404NotFound },
        { ErrorType.Conflict, StatusCodes.Status409Conflict },
        { ErrorType.Unavailable, StatusCodes.Status503ServiceUnavailable },
        { ErrorType.Failure, StatusCodes.Status500InternalServerError }
    };

    [Theory]
    [MemberData(nameof(StatusByErrorType))]
    public void Failed_results_become_ProblemDetails_with_status_detail_and_code(ErrorType type, int status)
    {
        var error = new Error("Sample.Code", "Safe message.", type);

        var problem = AssertProblem(CreateController().Ok(Result.Failure<string>(error)), status, "Sample.Code");

        Assert.Equal("Safe message.", problem.Detail);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
        AssertProblem(CreateController().NoContent(Result.Failure(error)), status, "Sample.Code");
    }

    [Fact]
    public void Validation_errors_with_fields_become_ValidationProblemDetails()
    {
        var error = Error.Validation("Validation.Required", "FarmId is required.",
            new Dictionary<string, string[]> { ["FarmId"] = ["FarmId is required."] });

        var problem = AssertProblem(CreateController().Ok(Result.Failure<int>(error)), 400, "Validation.Required");

        var validation = Assert.IsType<ValidationProblemDetails>(problem);
        Assert.Equal(["FarmId is required."], validation.Errors["FarmId"]);
    }

    [Fact]
    public void CreatedOrProblem_returns_201_with_the_id_and_a_location_route()
    {
        var result = CreateController().Created(Result.Success(42));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal("GetById", created.ActionName);
        Assert.Equal(42, created.RouteValues!["id"]);
        Assert.Equal(new CreatedResponse(42), created.Value);
    }

    [Fact]
    public void CreatedOrProblem_returns_ProblemDetails_for_a_failed_result()
    {
        var result = CreateController().Created(Result.Failure<int>(Error.Conflict("Farm.Duplicate", "The farm already exists.")));

        AssertProblem(result, StatusCodes.Status409Conflict, "Farm.Duplicate");
    }
}
