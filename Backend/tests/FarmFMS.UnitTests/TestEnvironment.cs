using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace FarmFMS.UnitTests;

/// <summary>
/// Configuration for every in-memory API host in this test assembly.
/// <c>appsettings.json</c> carries no connection string and no key (secrets come from user-secrets / environment
/// variables), so the tests supply a placeholder connection string and throwaway keys generated at runtime.
/// Environment variables override user-secrets, which guarantees the tests never use a developer's real database or
/// real keys. Nothing is committed: the key values exist only in this process. Nothing in the tests opens the connection;
/// database access is faked.
/// </summary>
internal static class TestEnvironment
{
    public const string PlaceholderConnectionString =
        "Server=farmfms-tests.invalid;Database=FarmFmsTests;Integrated Security=True;TrustServerCertificate=True";

    public const string TestKeyVersion = "test-1";

    [ModuleInitializer]
    internal static void Initialize()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__FarmFMS", PlaceholderConnectionString);

        // Throwaway secrets (Step 0, AC-15: the API refuses to start without them).
        Environment.SetEnvironmentVariable("Jwt__SigningKey", NewKey());
        Environment.SetEnvironmentVariable("PasswordEncryption__ActiveKeyVersion", TestKeyVersion);
        Environment.SetEnvironmentVariable($"PasswordEncryption__Keys__{TestKeyVersion}", NewKey());
    }

    private static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
