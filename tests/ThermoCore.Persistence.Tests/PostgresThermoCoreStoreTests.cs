using ThermoCore.AWG.Configuration;
using ThermoCore.AWG.Simulation;
using ThermoCore.Persistence;

namespace ThermoCore.Persistence.Tests;

public class PostgresThermoCoreStoreTests
{
    [Fact]
    public void SaveConfiguration_AndSeries_RoundTrip_WhenConnectionConfigured()
    {
        var connection = Environment.GetEnvironmentVariable("THERMOCORE_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection))
        {
            return; // Skip when Postgres is unavailable (CI default).
        }

        using var store = new PostgresThermoCoreStore(connection);
        store.EnsureCreated();

        var document = AwgConfigurationLoader.CreateDefaultDocument(enableElectricalSubsystem: false);
        var saved = store.SaveConfiguration(document, "pg-mvp-" + Guid.NewGuid().ToString("N")[..8]);
        Assert.Equal(saved.Id, store.GetConfigurationVersion(saved.Id)!.Id);

        var run = new AwgSimulationRunner().Run(
            document.System,
            document.InitialState,
            AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1)));
        var summary = store.SaveSimulationSummary(run, saved.Id);
        var series = store.SaveResultSeries(summary.Id, run);
        Assert.NotEmpty(series.Channels);

        var reloaded = store.GetResultSeries(summary.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(series.Channels.Count, reloaded!.Channels.Count);
        Assert.StartsWith("pg://result_series_payloads/", reloaded.Channels[0].StorageLocation, StringComparison.Ordinal);
    }

    [Fact]
    public void Factory_ParsesPostgresSpecifier()
    {
        var store = ThermoCoreStoreFactory.CreateFromSpecifier(
            "postgres:Host=localhost;Username=thermocore;Password=x;Database=thermocore");
        Assert.IsType<PostgresThermoCoreStore>(store);
        (store as IDisposable)?.Dispose();
    }

    [Fact]
    public void Factory_ParsesSqlitePath()
    {
        var path = Path.Combine(Path.GetTempPath(), "thermocore-factory-" + Guid.NewGuid().ToString("N") + ".db");
        var store = ThermoCoreStoreFactory.CreateFromSpecifier(path);
        Assert.IsType<SqliteThermoCoreStore>(store);
        (store as IDisposable)?.Dispose();
    }
}
