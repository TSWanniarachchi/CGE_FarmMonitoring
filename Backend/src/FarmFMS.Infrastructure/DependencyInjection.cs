using FarmFMS.Application.Abstractions.Data;
using FarmFMS.Application.Abstractions.Identity;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Application.Module01.Authorization;
using FarmFMS.Application.Module01.Users;
using FarmFMS.Infrastructure.Data;
using FarmFMS.Infrastructure.HealthChecks;
using FarmFMS.Infrastructure.Identity;
using FarmFMS.Infrastructure.Persistence;
using FarmFMS.Infrastructure.Repositories.Module01;
using FarmFMS.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/*
 * Description  : Registers the Infrastructure-layer services (EF Core schema context, Dapper data access).
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "FarmFMS";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        // Options (validated at startup)
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SystemUserOptions>()
            .Bind(configuration.GetSection(SystemUserOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Security options (validated at startup, AC-15). Secret values come from user-secrets / environment variables.
        services.AddOptions<PasswordEncryptionOptions>()
            .Bind(configuration.GetSection(PasswordEncryptionOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<PasswordEncryptionOptions>, PasswordEncryptionOptionsValidator>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

        services.AddOptions<SessionOptions>()
            .Bind(configuration.GetSection(SessionOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SessionOptions>, SessionOptionsValidator>();

        // Application-level views of the options (Application never references configuration packages).
        services.AddSingleton<ISessionSettings>(sp => sp.GetRequiredService<IOptions<SessionOptions>>().Value);
        services.AddSingleton<ISystemUserSettings>(sp => sp.GetRequiredService<IOptions<SystemUserOptions>>().Value);

        // Security services: AES-256-GCM password protector (key ring zeroed on shutdown), JWT issuer, refresh tokens.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(sp => new PasswordKeyRing(sp.GetRequiredService<IOptions<PasswordEncryptionOptions>>()));
        services.AddSingleton<IPasswordProtector, AesGcmPasswordProtector>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddSingleton<ISessionTokenFactory, SessionTokenFactory>();

        // Identity: the System user Id is resolved from the database (dbo.SreAppUsrSystemId, AD-14 / D-19) and cached
        // for the process lifetime (singleton cache + scoped resolver; "not found" is never cached).
        services.AddSingleton<SystemUserIdCache>();
        services.AddScoped<ISystemUserProvider, DatabaseSystemUserProvider>();

        // EF Core: schema + migrations only. Never inject FarmFmsDbContext into repositories or services.
        services.AddDbContext<FarmFmsDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(FarmFmsDbContext).Assembly.FullName)));

        // Dapper: all application data access through Stored Procedures.
        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddScoped<UnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UnitOfWork>());
        services.AddScoped<ITransactionAccessor>(sp => sp.GetRequiredService<UnitOfWork>());
        services.AddScoped<IStoredProcedureExecutor, StoredProcedureExecutor>();

        // Health: readiness checks for the resources this layer owns (SQL Server).
        services.AddHealthChecks()
            .AddInfrastructureHealthChecks();

        // Repositories: registered here, grouped by module, as modules are implemented.
        // Module 01 - Step 0 authentication foundation
        services.AddScoped<ILoginRepository, LoginRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<ILoginHistoryRepository, LoginHistoryRepository>();
        services.AddScoped<IUserPermissionRepository, UserPermissionRepository>();
        services.AddScoped<IAppUserReadRepository, AppUserReadRepository>();
        services.AddScoped<IAdministratorBootstrapRepository, AdministratorBootstrapRepository>();

        return services;
    }

    /// <summary>
    /// Registers the readiness checks (SQL Server connectivity), tagged <see cref="HealthCheckTags.Ready"/>.
    /// </summary>
    public static IHealthChecksBuilder AddInfrastructureHealthChecks(this IHealthChecksBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddCheck<DatabaseHealthCheck>(
            DatabaseHealthCheck.Name,
            tags: [HealthCheckTags.Ready],
            timeout: TimeSpan.FromSeconds(10));
    }
}
