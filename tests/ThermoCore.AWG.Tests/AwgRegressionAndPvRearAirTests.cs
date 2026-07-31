using ThermoCore.AWG.Regression;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Components;

namespace ThermoCore.AWG.Tests;

public class AwgRegressionAndPvRearAirTests
{
    [Fact]
    public void DefaultRegressionScenarios_AllPass()
    {
        var runner = new AwgRegressionScenarioRunner();
        var results = runner.RunAll(AwgRegressionScenarioCatalog.CreateDefaultScenarios());
        Assert.All(results, r => Assert.True(r.Passed, $"{r.Scenario.Id}: {string.Join("; ", r.Failures)}"));
    }

    [Fact]
    public void DrySunnyMatrixScenarios_AllPass()
    {
        var scenarios = AwgRegressionScenarioCatalog.CreateDrySunnyMatrixScenarios();
        Assert.Equal(30, scenarios.Count);
        Assert.All(scenarios, s =>
        {
            Assert.Equal(0.30, s.RelativeHumidityFraction);
            Assert.Equal(950.0, s.SolarIrradianceWPerSquareMeter);
            Assert.Equal(0.90, s.InitialBatterySocFraction);
            Assert.NotNull(s.SilicaGelDryAdsorbentMassKg);
        });

        var runner = new AwgRegressionScenarioRunner();
        var results = runner.RunAll(scenarios);
        Assert.All(results, r => Assert.True(r.Passed, $"{r.Scenario.Id}: {string.Join("; ", r.Failures)}"));
    }

    [Fact]
    public void ScenarioCatalog_RoundTripsThroughJsonDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "awg-scenarios-" + Guid.NewGuid().ToString("N"));
        try
        {
            AwgRegressionScenarioCatalog.WriteDefaultScenarios(directory);
            var loaded = AwgRegressionScenarioCatalog.LoadFromDirectory(directory);
            Assert.Equal(AwgRegressionScenarioCatalog.CreateDefaultScenarios().Count, loaded.Count);
            Assert.Contains(loaded, s => s.Id == "pv-rear-air");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void PvRearAirChannel_WiresDynamicPanelIntoProcessTrain()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enablePvRearAirChannel: true);
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
        var built = new AwgV3SystemGraphBuilder().Build(configuration, initial);

        Assert.True(built.Metadata.EnableElectricalSubsystem);
        Assert.Contains(built.Graph.Components, c => c is DynamicElectrothermalSolarPanelComponent);
        Assert.Contains(
            built.Graph.Connections,
            c => c.SourceComponentId == AwgV3TopologyIds.PeltierHotSideHx
                && c.TargetComponentId == AwgV3TopologyIds.PvPanel
                && c.TargetPortId == "rear_air_in");
        Assert.Contains(
            built.Graph.Connections,
            c => c.SourceComponentId == AwgV3TopologyIds.PvPanel
                && c.SourcePortId == "rear_air_out"
                && c.TargetComponentId == AwgV3TopologyIds.SolarCollector);
    }
}
