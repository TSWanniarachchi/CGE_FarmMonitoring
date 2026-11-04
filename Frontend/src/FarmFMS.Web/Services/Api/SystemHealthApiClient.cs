namespace FarmFMS.Web.Services.Api;

/// <summary>
/// Typed HttpClient for the Backend health endpoint. Serves as the reference pattern
/// for module API clients (one typed client per Backend resource).
/// </summary>
public sealed class SystemHealthApiClient(HttpClient httpClient)
{
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync("health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }
}
