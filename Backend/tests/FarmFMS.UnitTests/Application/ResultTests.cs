using FarmFMS.Application.Common.Results;

namespace FarmFMS.UnitTests.Application;

public class ResultTests
{
    private static readonly Error NotFound = Error.NotFound("AppUser.NotFound", "User not found.");

    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Same(Error.None, result.Error);
    }

    [Fact]
    public void Failure_carries_the_error()
    {
        var result = Result.Failure(NotFound);

        Assert.True(result.IsFailure);
        Assert.Same(NotFound, result.Error);
    }

    [Fact]
    public void Failure_without_an_error_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));
    }

    [Fact]
    public void Value_of_a_successful_result_is_returned()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Value_of_a_failed_result_throws()
    {
        Result<int> result = NotFound;

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Validation_error_carries_field_errors()
    {
        var error = Error.Validation(
            "AppUser.Invalid",
            "Please check the highlighted fields.",
            new Dictionary<string, string[]> { ["Username"] = ["Username is required."] });

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal(["Username is required."], error.ValidationErrors["Username"]);
    }

    [Fact]
    public void Non_validation_error_has_no_field_errors()
    {
        Assert.Empty(NotFound.ValidationErrors);
    }
}
