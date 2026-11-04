using Microsoft.Extensions.Options;

namespace FarmFMS.Web.Services.Api;

public static class ApiClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers all typed HttpClients that talk to the FarmFMS Backend API.
    /// </summary>
    public static IServiceCollection AddBackendApiClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BackendApiOptions>()
            .Bind(configuration.GetSection(BackendApiOptions.SectionName))
            .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _), "BackendApi:BaseUrl must be an absolute URL.")
            .ValidateOnStart();

        services.AddHttpClient<SystemHealthApiClient>(ConfigureBackendClient);

        return services;
    }

    private static void ConfigureBackendClient(IServiceProvider serviceProvider, HttpClient client)
    {
        var options = serviceProvider.GetRequiredService<IOptions<BackendApiOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    }
}
