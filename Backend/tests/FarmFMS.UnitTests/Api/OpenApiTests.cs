using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FarmFMS.UnitTests.Api;

/// <summary>
/// The built-in OpenAPI document and Swagger UI (Swashbuckle UI only, approved AD-18 exception) are served in Development only.
/// </summary>
public class OpenApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private HttpClient ClientFor(string environment) =>
        factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment)).CreateClient();

    [Fact]
    public async Task OpenApi_document_is_served_in_Development()
    {
        using var client = ClientFor("Development");

        using var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.TryGetProperty("openapi", out _));
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    public async Task Health_endpoints_are_documented_with_200_and_503_text_responses(string path)
    {
        using var client = ClientFor("Development");

        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var get = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty("get");

        Assert.False(string.IsNullOrWhiteSpace(get.GetProperty("summary").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(get.GetProperty("description").GetString()));
        Assert.Equal("Health", get.GetProperty("tags")[0].GetString());
        foreach (var status in new[] { "200", "503" })
            Assert.True(get.GetProperty("responses").GetProperty(status).GetProperty("content").TryGetProperty("text/plain", out _));
    }

    [Fact]
    public async Task Swagger_UI_is_served_in_Development_and_loads_the_OpenApi_document()
    {
        using var client = ClientFor("Development");

        using var page = await client.GetAsync("/swagger/index.html");
        using var config = await client.GetAsync("/swagger/index.js");

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("/openapi/v1.json", await config.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Swagger_UI_root_redirects_to_the_UI_page()
    {
        using var client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"))
            .CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/swagger");

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.EndsWith("swagger/index.html", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Swagger_UI_renders_only_the_built_in_document()
    {
        // AD-18 approved exception: Swashbuckle provides the UI only; the built-in generator provides the document.
        using var client = ClientFor("Development");

        var config = await client.GetStringAsync("/swagger/index.js");
        using var swashbuckleDocument = await client.GetAsync("/swagger/v1/swagger.json");
        using var builtInDocument = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));

        Assert.Contains("\"url\":\"/openapi/v1.json\"", config);
        Assert.DoesNotContain("swagger.json", config);
        Assert.Equal(HttpStatusCode.NotFound, swashbuckleDocument.StatusCode);
        Assert.StartsWith("3.", builtInDocument.RootElement.GetProperty("openapi").GetString());
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/swagger")]
    [InlineData("/swagger/index.html")]
    [InlineData("/swagger/index.js")]
    public async Task OpenApi_and_Swagger_UI_are_not_exposed_in_Production(string path)
    {
        using var client = ClientFor("Production");

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
