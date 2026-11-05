using System.Text.Json;
using System.Text.RegularExpressions;
using FarmFMS.ArchitectureTests.Support;

/*
 * Description  : Verifies that committed appsettings files carry no credentials (AD-17).
 * Author       : T.S.Wanniarachchi
 * Created Date : 05-11-2026
 */
namespace FarmFMS.ArchitectureTests;

/// <summary>
/// AD-17: <c>appsettings*.json</c> holds non-secret values only. Credentials come from user-secrets (Development)
/// or environment variables / a secret store. Failure messages name the file and key only, never the value.
/// </summary>
public partial class ConfigurationSecretsTests
{
    [Fact]
    public void Settings_files_are_found()
    {
        // Guards against the scan silently checking nothing.
        Assert.Contains(SettingsFiles(), f => f.EndsWith("FarmFMS.API/appsettings.json", StringComparison.Ordinal));
    }

    [Fact]
    public void Committed_settings_files_contain_no_credentials()
    {
        var offenders = SettingsFiles()
            .SelectMany(path => FindCredentials(File.ReadAllText(path))
                .Select(key => $"{Path.GetRelativePath(SolutionFiles.BackendRoot, path).Replace('\\', '/')}: {key}"))
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Credentials must come from user-secrets or environment variables, not appsettings: {string.Join("; ", offenders)}");
    }

    [Theory]
    [InlineData("""{ "ConnectionStrings": { "FarmFMS": "Server=x;Database=y;User ID=u;Password=p" } }""", "ConnectionStrings:FarmFMS")]
    [InlineData("""{ "ConnectionStrings": { "FarmFMS": "Data Source=x;Uid=u;Pwd=p" } }""", "ConnectionStrings:FarmFMS")]
    [InlineData("""{ "Jwt": { "SigningKey": "abc" } }""", "Jwt:SigningKey")]
    [InlineData("""{ "Encryption": { "Password": "abc" } }""", "Encryption:Password")]
    public void Detector_finds_credentials(string json, string expectedKey)
    {
        Assert.Contains(expectedKey, FindCredentials(json));
    }

    [Theory]
    [InlineData("""{ "ConnectionStrings": { "FarmFMS": "" } }""")]
    [InlineData("""{ "ConnectionStrings": { "FarmFMS": "Server=x;Database=y;Trusted_Connection=True" } }""")]
    [InlineData("""{ "Jwt": { "SigningKey": "" }, "Database": { "CommandTimeoutSeconds": 30 } }""")]
    public void Detector_accepts_settings_without_credentials(string json)
    {
        Assert.Empty(FindCredentials(json));
    }

    private static IEnumerable<string> SettingsFiles() =>
        Directory.EnumerateFiles(SolutionFiles.SourceRoot, "appsettings*.json", SearchOption.AllDirectories)
            .Select(path => path.Replace('\\', '/'))
            .Where(path => !path.Contains("/bin/", StringComparison.Ordinal) && !path.Contains("/obj/", StringComparison.Ordinal));

    /// <summary>
    /// Returns the keys (never the values) that hold credentials: connection strings with SQL credentials,
    /// and non-empty values of password / secret / key / token settings.
    /// </summary>
    internal static List<string> FindCredentials(string json)
    {
        var keys = new List<string>();
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        Walk(document.RootElement, string.Empty, keys);
        return keys;
    }

    private static void Walk(JsonElement element, string path, List<string> keys)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                    Walk(property.Value, path.Length == 0 ? property.Name : $"{path}:{property.Name}", keys);
                break;
            case JsonValueKind.String:
                var value = element.GetString() ?? string.Empty;
                var isConnectionString = path.StartsWith("ConnectionStrings:", StringComparison.OrdinalIgnoreCase);
                if ((isConnectionString && SqlCredentialRegex().IsMatch(value))
                    || (!isConnectionString && value.Length > 0 && SecretKeyRegex().IsMatch(path.Split(':')[^1])))
                {
                    keys.Add(path);
                }
                break;
        }
    }

    [GeneratedRegex(@"(?:^|;)\s*(?:Password|Pwd|User\s*ID|UID)\s*=", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SqlCredentialRegex();

    [GeneratedRegex(@"(?:Password|Secret|ApiKey|SigningKey|EncryptionKey|Token)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SecretKeyRegex();
}
