using ThermoCore.AWG.Configuration;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Simulation;

namespace ThermoCore.AWG.Tests;

public class AwgConfigurationAndGraphBuilderTests
{
    [Fact]
    public void CreateMvpConfiguration_ValidatesAndBuildsAcyclicGraph()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration();
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);

        var built = new AwgV3SystemGraphBuilder().Build(configuration, initial);

        Assert.Equal(AwgV3TopologyIds.TopologyId, built.Metadata.TopologyId);
        Assert.True(built.Metadata.EnableElectricalSubsystem);
        Assert.False(built.Metadata.EnableRecirculation);
        Assert.Contains(built.Graph.Components, c => c.Id == AwgV3TopologyIds.SilicaGelBed);
        Assert.Contains(built.Graph.Components, c => c.Id == AwgV3TopologyIds.WaterTank);
        Assert.Contains(built.Graph.Components, c => c.Id == AwgV3TopologyIds.PowerManager);
        Assert.False(string.IsNullOrWhiteSpace(built.Metadata.GraphFingerprint));

        var validation = built.Graph.Validate();
        Assert.True(validation.IsValid, string.Join("; ", validation.Diagnostics.Select(d => d.Message)));
    }

    [Fact]
    public void BuiltMvpGraph_RunsOneSecondSmokeSimulation()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration();
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
        var built = new AwgV3SystemGraphBuilder().Build(configuration, initial);

        var result = new AcyclicSimulationEngine().Run(new SimulationRequest
        {
            Graph = built.Graph,
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(1),
            TimeStep = TimeSpan.FromSeconds(1)
        });

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => $"{d.Code}:{d.Message}")));
        Assert.True(result.Steps[0].PortStates.ContainsKey($"{AwgV3TopologyIds.Condenser}.outlet"));
        Assert.True(result.Steps[0].PortStates.ContainsKey($"{AwgV3TopologyIds.PowerManager}.bus"));
    }

    [Fact]
    public void Builder_RejectsRecirculationUntilCyclicIntegration()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableRecirculation: true);

        var ex = Assert.Throws<AwgConfigurationException>(() =>
            new AwgV3SystemGraphBuilder().Build(
                configuration,
                AwgSystemDefaults.CreateMvpInitialState(configuration)));

        Assert.Contains(ex.Diagnostics, d => d.Code == "AWG.RECIRCULATION_UNSUPPORTED");
    }

    [Fact]
    public void Configuration_RoundTripsThroughJson()
    {
        var original = AwgConfigurationLoader.CreateDefaultDocument();
        var json = AwgConfigurationLoader.SaveToJson(original);
        var loaded = AwgConfigurationLoader.LoadFromJson(json);

        Assert.Equal(original.System.TopologyId, loaded.System.TopologyId);
        Assert.Equal(original.System.Ambient.TemperatureK, loaded.System.Ambient.TemperatureK);
        Assert.Equal(original.InitialState.SilicaGelLoadingKgPerKg, loaded.InitialState.SilicaGelLoadingKgPerKg);
        Assert.Equal(original.System.ElectricalLoads.Count, loaded.System.ElectricalLoads.Count);

        var built = new AwgV3SystemGraphBuilder().Build(loaded.System, loaded.InitialState);
        Assert.Equal(AwgV3TopologyIds.TopologyId, built.Metadata.TopologyId);
    }

    [Fact]
    public void Configuration_LoadFromFile_Works()
    {
        var path = Path.Combine(Path.GetTempPath(), $"awg-config-{Guid.NewGuid():N}.json");
        try
        {
            AwgConfigurationLoader.SaveToFile(AwgConfigurationLoader.CreateDefaultDocument(), path);
            var loaded = AwgConfigurationLoader.LoadFromFile(path);
            Assert.Equal(AwgV3TopologyIds.TopologyId, loaded.System.TopologyId);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void AirflowOnlyConfiguration_OmitsElectricalComponents()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false);
        var built = new AwgV3SystemGraphBuilder().Build(
            configuration,
            AwgSystemDefaults.CreateMvpInitialState(configuration));

        Assert.False(built.Metadata.EnableElectricalSubsystem);
        Assert.DoesNotContain(built.Graph.Components, c => c.Id == AwgV3TopologyIds.PowerManager);
        Assert.Contains(built.Graph.Components, c => c.Id == AwgV3TopologyIds.ProcessFan);
    }
}
