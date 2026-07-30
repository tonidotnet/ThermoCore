using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Environment;
using ThermoCore.Core.Results;

namespace ThermoCore.AWG.Tests;

public class AwgBalanceAndExportBundleTests
{
    [Fact]
    public void SystemBalanceVerifier_PassesOnSuccessfulShortRun()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false);
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
        var run = new AwgSimulationRunner().Run(
            configuration,
            initial,
            AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)));

        Assert.True(run.EngineResult.Succeeded);
        Assert.True(run.BalanceReport.WaterBalancePassed);
        Assert.True(run.BalanceReport.EnergyBalancePassed);
        Assert.True(run.BalanceReport.DryAirBalancePassed);
        Assert.True(run.BalanceReport.AllPassed);
        Assert.Equal(5, run.BalanceReport.CheckedStepCount);
    }

    [Fact]
    public void SystemBalanceVerifier_PassesOnWeatherDrivenDaySegment()
    {
        var start = DateTimeOffset.Parse("2026-07-01T10:00:00Z");
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false);
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
        var run = new AwgSimulationRunner().Run(
            configuration,
            initial,
            new AwgSimulationOptions
            {
                StartTimeUtc = start,
                Duration = TimeSpan.FromHours(1),
                TimeStep = TimeSpan.FromSeconds(5),
                WeatherProvider = SyntheticDiurnalWeatherProvider.CreateDefault(
                    DateTimeOffset.Parse("2026-07-01T00:00:00Z"))
            }.Validate());

        Assert.True(run.EngineResult.Succeeded, string.Join("; ", run.EngineResult.Diagnostics.Select(d => d.Code)));
        Assert.True(run.BalanceReport.WaterBalancePassed, $"water max={run.BalanceReport.MaxAbsWaterResidualKg}");
        Assert.True(run.BalanceReport.EnergyBalancePassed, $"energy max={run.BalanceReport.MaxAbsEnergyResidualJ}");
        Assert.True(run.BalanceReport.AllPassed);
    }

    [Fact]
    public void ExportBundle_WritesManifestConfigurationAndBalanceVerification()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: true);
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
        var run = new AwgSimulationRunner().Run(
            configuration,
            initial,
            AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1)));

        var directory = Path.Combine(Path.GetTempPath(), "awg-bundle-" + Guid.NewGuid().ToString("N"));
        try
        {
            var (result, manifest) = AwgResultExporter.ExportBundle(run, directory, simulationId: "smoke-001");
            Assert.Equal("smoke-001", manifest.SimulationId);
            Assert.Equal(SimulationResultBundleExporter.PackageType, manifest.PackageType);
            Assert.True(result.Summary.ScalarMetrics.ContainsKey("balance.water.maximumAbsoluteResidualKg"));

            Assert.True(File.Exists(Path.Combine(directory, "manifest.json")));
            Assert.True(File.Exists(Path.Combine(directory, "configuration.json")));
            Assert.True(File.Exists(Path.Combine(directory, "metadata.json")));
            Assert.True(File.Exists(Path.Combine(directory, "summary.json")));
            Assert.True(File.Exists(Path.Combine(directory, "channels.json")));
            Assert.True(File.Exists(Path.Combine(directory, "summary.csv")));
            Assert.True(File.Exists(Path.Combine(directory, "series-wide.csv")));
            Assert.True(File.Exists(Path.Combine(directory, "diagnostics.csv")));
            Assert.True(File.Exists(Path.Combine(directory, "balances.csv")));
            Assert.True(File.Exists(Path.Combine(directory, "balance-verification.json")));
            Assert.True(File.Exists(Path.Combine(directory, "awg-summary.json")));
            Assert.True(File.Exists(Path.Combine(directory, "README.txt")));

            Assert.Contains(manifest.Files, f => f.Path == "configuration.json" && f.Sha256.Length == 64);
            Assert.Contains(manifest.Files, f => f.Path == "balance-verification.json");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
