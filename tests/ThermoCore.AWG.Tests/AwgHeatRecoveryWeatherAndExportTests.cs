using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Components;
using ThermoCore.Core.Environment;

namespace ThermoCore.AWG.Tests;

public class AwgHeatRecoveryWeatherAndExportTests
{
    [Fact]
    public void HeatRecoveryTopology_ConvergesWithTornLoop()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(
            enableElectricalSubsystem: false,
            enableHeatRecovery: true);
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);

        var run = new AwgSimulationRunner().Run(
            configuration,
            initial,
            AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1)));

        Assert.True(run.BuiltSystem.RequiresCyclicSolver);
        Assert.Contains(run.BuiltSystem.Graph.Components, c => c.Id == AwgV3TopologyIds.HeatRecovery);
        Assert.True(
            run.EngineResult.Succeeded,
            string.Join("; ", run.EngineResult.Diagnostics.Select(d => $"{d.Code}:{d.Message}")));
        Assert.IsType<SensibleHeatRecoveryComponent>(
            run.BuiltSystem.Graph.Components.Single(c => c.Id == AwgV3TopologyIds.HeatRecovery));
    }

    [Fact]
    public void HeatRecoveryAndRecirculation_ConvergesWithTwoTears()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(
            enableElectricalSubsystem: false,
            enableRecirculation: true,
            enableHeatRecovery: true);
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);

        var run = new AwgSimulationRunner().Run(
            configuration,
            initial,
            AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1)));

        Assert.Equal(2, run.BuiltSystem.Loops.Count);
        Assert.Contains(run.BuiltSystem.Loops, l => l.TearConnectionId == AwgV3TopologyIds.RecirculationTearConnectionId);
        Assert.Contains(run.BuiltSystem.Loops, l => l.TearConnectionId == AwgV3TopologyIds.HeatRecoveryTearConnectionId);
        Assert.True(
            run.EngineResult.Succeeded,
            string.Join("; ", run.EngineResult.Diagnostics.Select(d => $"{d.Code}:{d.Message}")));
        Assert.True(run.BalanceReport.WaterBalancePassed);
        Assert.True(run.BalanceReport.EnergyBalancePassed);
    }

    [Fact]
    public void TwentyFourHourSyntheticWeatherRun_Succeeds()
    {
        var start = DateTimeOffset.Parse("2026-07-01T00:00:00Z");
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false);
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
        var options = new AwgSimulationOptions
        {
            StartTimeUtc = start,
            Duration = TimeSpan.FromHours(24),
            TimeStep = TimeSpan.FromSeconds(5),
            WeatherProvider = SyntheticDiurnalWeatherProvider.CreateDefault(start)
        }.Validate();

        var run = new AwgSimulationRunner().Run(configuration, initial, options);
        Assert.True(
            run.EngineResult.Succeeded,
            string.Join("; ", run.EngineResult.Diagnostics
                .Where(d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error)
                .Select(d => $"{d.Code}:{d.Message}")));
        Assert.Equal(17_280, run.EngineResult.Steps.Count);
        Assert.Contains(
            run.BuiltSystem.Graph.Components,
            c => c is WeatherDrivenAmbientAirSourceComponent);
        Assert.Contains(
            run.BuiltSystem.Graph.Components,
            c => c is WeatherDrivenSolarRadiationSourceComponent);
    }

    [Fact]
    public void AwgResultExporter_WritesCsvBundle()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false);
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
        var run = new AwgSimulationRunner().Run(
            configuration,
            initial,
            AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1)));

        var directory = Path.Combine(Path.GetTempPath(), "awg-csv-" + Guid.NewGuid().ToString("N"));
        try
        {
            var result = AwgResultExporter.ExportCsv(run, directory);
            Assert.True(result.Channels.Count > 0);
            Assert.True(File.Exists(Path.Combine(directory, "series-wide.csv")));
            Assert.True(File.Exists(Path.Combine(directory, "balances.csv")));
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
