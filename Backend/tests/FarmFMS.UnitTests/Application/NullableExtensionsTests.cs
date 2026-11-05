using FarmFMS.Application.Common.Extensions;
using FarmFMS.Application.Common.Results;

namespace FarmFMS.UnitTests.Application;

public class NullableExtensionsTests
{
    private sealed record SampleRequest(int? FarmId, string? Name, object? Payload);

    [Fact]
    public void Required_value_type_returns_the_value_when_present()
    {
        var request = new SampleRequest(5, "Farm", new object());

        var result = request.FarmId.Required();

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public void Required_value_type_returns_a_validation_error_named_after_the_field()
    {
        var request = new SampleRequest(null, "Farm", null);

        var result = request.FarmId.Required();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(NullableExtensions.RequiredErrorCode, result.Error.Code);
        Assert.Equal(["FarmId is required."], result.Error.ValidationErrors["FarmId"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Required_string_rejects_null_and_blank(string? name)
    {
        var request = new SampleRequest(1, name, null);

        var result = request.Name.Required();

        Assert.True(result.IsFailure);
        Assert.True(result.Error.ValidationErrors.ContainsKey("Name"));
    }

    [Fact]
    public void Required_reference_type_returns_the_value_when_present()
    {
        var payload = new object();
        var request = new SampleRequest(1, "Farm", payload);

        Assert.Same(payload, request.Payload.Required().Value);
        Assert.Equal("Farm", request.Name.Required().Value);
    }

    [Fact]
    public void Required_uses_a_plain_variable_name_as_the_field()
    {
        int? quantity = null;

        var result = quantity.Required();

        Assert.True(result.Error.ValidationErrors.ContainsKey("quantity"));
    }
}
