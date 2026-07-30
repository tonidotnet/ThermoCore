using ThermoCore.AWG.Calibration;
using ThermoCore.AWG.Configuration;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Calibration;
using ThermoCore.Persistence;

namespace ThermoCore.Persistence.Tests;

public class SqliteThermoCoreStoreTests
{
    [Fact]
    public void SaveConfiguration_AndCalibration_RoundTrip()
    {
        var path = Path.Combine(Path.GetTempPath(), "thermocore-persist-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            using var store = new SqliteThermoCoreStore(path);
            store.EnsureCreated();

            var document = AwgConfigurationLoader.CreateDefaultDocument(enableElectricalSubsystem: false);
            var saved = store.SaveConfiguration(document, "mvp-default");
            Assert.False(string.IsNullOrWhiteSpace(saved.ContentHash));
            Assert.Equal(saved.Id, store.GetConfigurationVersion(saved.Id)!.Id);

            var run = new AwgSimulationRunner().Run(
                document.System,
                document.InitialState,
                AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1)));
            var summary = store.SaveSimulationSummary(run, saved.Id);
            Assert.Equal(saved.Id, summary.ConfigurationVersionId);
            Assert.Equal(summary.Id, store.GetSimulationSummary(summary.Id)!.Id);
            Assert.Contains(store.ListSimulationSummaries(), s => s.Id == summary.Id);

            var series = store.SaveResultSeries(summary.Id, run);
            Assert.NotEmpty(series.Channels);
            var reloaded = store.GetResultSeries(summary.Id);
            Assert.NotNull(reloaded);
            Assert.Equal(series.Channels.Count, reloaded!.Channels.Count);
            Assert.NotNull(reloaded.ValuesByChannelId);
            Assert.Equal(
                series.ValuesByChannelId![series.Channels[0].ChannelId].Count,
                reloaded.ValuesByChannelId![series.Channels[0].ChannelId].Count);

            var calibration = new AwgParameterCalibrationResult
            {
                Fitting = new ParameterFittingResult
                {
                    InitialValues = new Dictionary<string, double> { ["condenser.bypassFactor"] = 0.2 },
                    FittedValues = new Dictionary<string, double> { ["condenser.bypassFactor"] = 0.18 },
                    InitialObjective = 1.0,
                    FinalObjective = 0.5,
                    EvaluationCount = 10,
                    PassCount = 1
                },
                BaselineConfiguration = document.System,
                FittedConfiguration = document.System,
                BaselineReport = new MeasurementComparisonReport
                {
                    MeasurementSourcePath = "m.csv",
                    Channels = Array.Empty<ChannelComparisonResult>(),
                    MissingChannels = Array.Empty<string>(),
                    Warnings = Array.Empty<string>()
                },
                FittedReport = new MeasurementComparisonReport
                {
                    MeasurementSourcePath = "m.csv",
                    Channels = Array.Empty<ChannelComparisonResult>(),
                    MissingChannels = Array.Empty<string>(),
                    Warnings = Array.Empty<string>()
                }
            };

            var cal = store.SaveCalibrationRun(calibration, "m.csv", saved.Id, saved.Id);
            var listed = store.ListCalibrationRuns();
            Assert.Contains(listed, r => r.Id == cal.Id);
            Assert.Equal("bounded-coordinate-descent-golden-section", cal.Algorithm);
        }
        finally
        {
            SqliteConnectionClear(path);
        }
    }

    private static void SqliteConnectionClear(string path)
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var seriesDir = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".", "series");
        if (Directory.Exists(seriesDir))
        {
            Directory.Delete(seriesDir, recursive: true);
        }
    }
}
