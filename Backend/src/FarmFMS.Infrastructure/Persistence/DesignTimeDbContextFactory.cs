/*
 * Description  : Design-time factory so the EF Core tools can scaffold migrations and apply them without credentials on the command line.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FarmFMS.Infrastructure.Persistence;

/// <summary>
/// Used only by the EF Core tools.
/// <para>
/// The connection string is read from <b>environment variables</b> (<c>ConnectionStrings__FarmFMS</c>) and the API's
/// <b>user-secrets</b> (<c>ConnectionStrings:FarmFMS</c>); never from an appsettings file, so no credential is ever
/// passed on the command line (<c>--connection</c>) or committed. When no connection string is configured it falls back to
/// <c>UseSqlServer()</c> without one, which is enough for <c>migrations add</c> / <c>migrations script</c> (model building
/// and scaffolding never open a connection). Applying a migration still needs the real connection and explicit user approval.
/// </para>
/// The connection string is never logged or printed.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FarmFmsDbContext>
{
    /// <summary>
    /// <c>UserSecretsId</c> of <c>FarmFMS.API.csproj</c>. It is an identifier of the secrets store, not a secret.
    /// </summary>
    internal const string ApiUserSecretsId = "c9722ae3-bb03-4665-b3dc-3144e9817d2d";

    internal const string ConnectionStringKey = "ConnectionStrings:" + DependencyInjection.ConnectionStringName;

    public FarmFmsDbContext CreateDbContext(string[] args) => Create(BuildConfiguration());

    /// <summary>Environment variables plus the API's user-secrets (user-secrets win, as in the running API).</summary>
    internal static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets(ApiUserSecretsId, reloadOnChange: false)
            .Build();

    /// <summary>Uses the configured connection string, or the provider without one (offline scaffolding).</summary>
    internal static FarmFmsDbContext Create(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var builder = new DbContextOptionsBuilder<FarmFmsDbContext>();
        var connectionString = configuration[ConnectionStringKey];

        if (string.IsNullOrWhiteSpace(connectionString))
            builder.UseSqlServer();
        else
            builder.UseSqlServer(connectionString);

        return new FarmFmsDbContext(builder.Options);
    }
}
