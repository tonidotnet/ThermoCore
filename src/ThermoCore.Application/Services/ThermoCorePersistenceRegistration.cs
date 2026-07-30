using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ThermoCore.Persistence;

namespace ThermoCore.Api.Services;

/// <summary>Registers SQLite/PostgreSQL store and persistence helpers for API/Web hosts.</summary>
public static class ThermoCorePersistenceRegistration
{
    public static IServiceCollection AddThermoCorePersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<IThermoCoreStore>(_ =>
        {
            var providerName = configuration["Persistence:Provider"] ?? "Sqlite";
            IThermoCoreStore store;
            if (string.Equals(providerName, "PostgreSql", StringComparison.OrdinalIgnoreCase)
                || string.Equals(providerName, "Postgres", StringComparison.OrdinalIgnoreCase))
            {
                var connection = configuration["Persistence:ConnectionString"]
                    ?? throw new InvalidOperationException(
                        "Persistence:ConnectionString is required when Persistence:Provider is PostgreSql.");
                store = ThermoCoreStoreFactory.Create(ThermoCoreStoreProvider.PostgreSql, connection);
            }
            else
            {
                var path = configuration["Persistence:SqlitePath"]
                    ?? Path.Combine(environment.ContentRootPath, "App_Data", "thermocore.db");
                store = ThermoCoreStoreFactory.Create(ThermoCoreStoreProvider.Sqlite, path);
            }

            store.EnsureCreated();
            return store;
        });

        services.AddSingleton<SimulationRunPersistenceService>();
        services.AddSingleton<PersistedSimulationQueryService>();
        services.AddSingleton<SimulationCompareService>();
        return services;
    }
}
