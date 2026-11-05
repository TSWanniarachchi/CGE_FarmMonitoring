using FarmFMS.Application.Abstractions.Data;
using FarmFMS.Application.Abstractions.Identity;
using FarmFMS.Infrastructure.Data;
using FarmFMS.Infrastructure.HealthChecks;
using FarmFMS.Infrastructure.Identity;
using FarmFMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        // Identity: System user for background / system operations (ID per environment, never hard-coded).
        services.AddSingleton<ISystemUserProvider, ConfiguredSystemUserProvider>();

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
