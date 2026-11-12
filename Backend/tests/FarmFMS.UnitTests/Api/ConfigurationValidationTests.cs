using FarmFMS.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FarmFMS.UnitTests.Api;

/// <summary>
/// Startup validation of strongly typed options (ValidateOnStart).
/// </summary>
public class ConfigurationValidationTests
{
    // Added last, so it overrides the throwaway keys that TestEnvironment sets through environment variables.
    private static WebApplicationFactory<Program> WithSetting(string key, string value) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })));

    [Theory]
    [InlineData("SystemUser:Username", "   ")]
    [InlineData("Session:AbsoluteLifetimeHours", "0")]
    [InlineData("Session:AbsoluteLifetimeHours", "25")]
    [InlineData("Session:ActivityTouchIntervalSeconds", "301")]
    [InlineData("Jwt:AccessTokenLifetimeMinutes", "31")]
    [InlineData("Jwt:ClockSkewSeconds", "61")]
    [InlineData("Jwt:SigningKey", "c2hvcnQ=")] // base64 of "short": fewer than 32 bytes
    [InlineData("Jwt:Issuer", " ")]
    [InlineData("RateLimiting:Login:PermitLimit", "0")]
    [InlineData("RateLimiting:Refresh:WindowSeconds", "0")]
    [InlineData("ForwardedHeaders:KnownProxies:0", "not-an-ip")]
    [InlineData("PasswordEncryption:ActiveKeyVersion", "missing-version")]
    [InlineData("PasswordEncryption:ActiveKeyVersion", "bad label")]
    [InlineData("Database:CommandTimeoutSeconds", "0")]
    [InlineData("Database:CommandTimeoutSeconds", "601")]
    public void Invalid_settings_stop_the_API_at_startup(string key, string value)
    {
        using var factory = WithSetting(key, value);

        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_connection_string_stops_the_API_at_startup(string? connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:FarmFMS"] = connectionString })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(configuration));

        Assert.Contains("FarmFMS", exception.Message);
    }

    [Fact]
    public void Missing_signing_key_stops_the_API_at_startup()
    {
        using var factory = WithSetting("Jwt:SigningKey", "");

        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }

    [Fact]
    public void Missing_password_keys_stop_the_API_at_startup()
    {
        using var factory = WithSetting("PasswordEncryption:ActiveKeyVersion", "");

        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }

    [Fact]
    public void A_wrong_length_password_key_stops_the_API_at_startup()
    {
        using var factory = WithSetting($"PasswordEncryption:Keys:{TestEnvironment.TestKeyVersion}", "c2hvcnQ=");

        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }

    [Fact]
    public void The_default_test_configuration_starts_the_API()
    {
        using var factory = new WebApplicationFactory<Program>();

        using var client = factory.CreateClient();

        Assert.NotNull(client);
    }

    [Fact]
    public void System_user_username_defaults_to_SYSTEM()
    {
        using var factory = new WebApplicationFactory<Program>();

        var settings = factory.Services.GetRequiredService<FarmFMS.Application.Module01.Users.ISystemUserSettings>();

        Assert.Equal("SYSTEM", settings.Username);
    }

    [Fact]
    public void System_user_username_comes_from_configuration()
    {
        using var factory = WithSetting("SystemUser:Username", "BATCH_SYSTEM");

        var settings = factory.Services.GetRequiredService<FarmFMS.Application.Module01.Users.ISystemUserSettings>();

        Assert.Equal("BATCH_SYSTEM", settings.Username);
    }
}
