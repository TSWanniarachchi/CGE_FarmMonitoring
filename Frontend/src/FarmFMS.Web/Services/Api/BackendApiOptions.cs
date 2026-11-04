namespace FarmFMS.Web.Services.Api;

/// <summary>
/// Backend API connection settings, bound from the "BackendApi" configuration section.
/// </summary>
public sealed class BackendApiOptions
{
    public const string SectionName = "BackendApi";

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;
}
