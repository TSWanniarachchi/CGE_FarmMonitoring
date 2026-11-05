using FarmFMS.Application.Common.Extensions;

namespace FarmFMS.UnitTests.Application;

public class StringExtensionsTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  Farm A  ", "Farm A")]
    public void TrimToNull(string? input, string? expected) => Assert.Equal(expected, input.TrimToNull());

    [Theory]
    [InlineData(null, "")]
    [InlineData("  ", "")]
    [InlineData(" x ", "x")]
    public void TrimOrEmpty(string? input, string expected) => Assert.Equal(expected, input.TrimOrEmpty());

    [Theory]
    [InlineData(null, null)]
    [InlineData(" \t ", null)]
    [InlineData("  Green   Valley \t\r\n Farm ", "Green Valley Farm")]
    public void CollapseWhitespace(string? input, string? expected) => Assert.Equal(expected, input.CollapseWhitespace());

    [Theory]
    [InlineData(null, null)]
    [InlineData("  ", null)]
    [InlineData(" brl ", "BRL")]
    [InlineData("fm 001", "FM001")]
    public void ToCode(string? input, string? expected) => Assert.Equal(expected, input.ToCode());

    [Theory]
    [InlineData(null, null)]
    [InlineData(" ", null)]
    [InlineData("  John.Doe@Example.LK ", "john.doe@example.lk")]
    public void ToEmail(string? input, string? expected) => Assert.Equal(expected, input.ToEmail());

    [Theory]
    [InlineData(null, null)]
    [InlineData("abc", null)]
    [InlineData("+94 77-123 4567", "94771234567")]
    public void ToDigitsOnly(string? input, string? expected) => Assert.Equal(expected, input.ToDigitsOnly());

    [Theory]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    [InlineData("  green   valley ", "green valley")]
    public void ToSearchTerm(string? input, string? expected) => Assert.Equal(expected, input.ToSearchTerm());

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("1234", "****")]
    [InlineData("12345678", "********")]
    [InlineData("0771234567", "******4567")]
    public void Mask_shows_only_the_last_characters(string? input, string? expected) => Assert.Equal(expected, input.Mask());

    [Fact]
    public void Mask_rejects_a_negative_visible_length()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => "secret".Mask(-1));
    }
}
