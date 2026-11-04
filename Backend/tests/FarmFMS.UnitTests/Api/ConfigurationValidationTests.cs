using FarmFMS.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FarmFMS.UnitTests.Api;

/// <summary>
/// Startup validation of strongly typed options (ValidateOnStart).
/// </summary>
public class ConfigurationValidationTests
{
    private static WebApplicationFactory<Program> WithSetting(string key, string value) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseSetting(key, value));

    [Theory]
    [InlineData("SystemUser:UserId", "0")]
    [InlineData("SystemUser:UserId", "-1")]
    [InlineData("Database:CommandTimeoutSeconds", "0")]
    [InlineData("Database:CommandTimeoutSeconds", "601")]
    public void Invalid_settings_stop_the_API_at_startup(string key, string value)
    {
        using var factory = WithSetting(key, value);

        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }

    [Fact]
    public async Task System_user_id_comes_from_configuration()
    {
        using var factory = WithSetting("SystemUser:UserId", "123");
        using var scope = factory.Services.CreateScope();

        var provider = scope.ServiceProvider.GetRequiredService<ISystemUserProvider>();

        Assert.Equal(123, await provider.GetSystemUserIdAsync());
    }

    [Fact]
    public async Task System_user_id_is_not_set_by_default()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var provider = scope.ServiceProvider.GetRequiredService<ISystemUserProvider>();

        Assert.Null(await provider.GetSystemUserIdAsync());
    }
}
