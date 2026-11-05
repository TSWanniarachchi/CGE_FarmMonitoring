using System.Runtime.CompilerServices;

namespace FarmFMS.UnitTests;

/// <summary>
/// Configuration for every in-memory API host in this test assembly.
/// <c>appsettings.json</c> carries no connection string (secrets come from user-secrets / environment variables),
/// so the tests supply a placeholder. Environment variables override user-secrets, which guarantees the tests never
/// use a developer's real database. Nothing in the tests opens this connection; database access is faked.
/// </summary>
internal static class TestEnvironment
{
    public const string PlaceholderConnectionString =
        "Server=farmfms-tests.invalid;Database=FarmFmsTests;Integrated Security=True;TrustServerCertificate=True";

    [ModuleInitializer]
    internal static void Initialize() =>
        Environment.SetEnvironmentVariable("ConnectionStrings__FarmFMS", PlaceholderConnectionString);
}
