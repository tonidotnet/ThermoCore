using ThermoCore.AWG.Optimization;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;

namespace ThermoCore.AWG.Tests;

public class AwgRandomSearchTests
{
    [Fact]
    public void RandomSearch_ProducesRequestedSampleCount()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false);
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
        var options = AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1));

        var result = new AwgRandomSearchRunner().Run(
            configuration,
            initial,
            options,
            sampleCount: 3,
            seed: 42);

        Assert.Equal(3, result.Points.Count);
        Assert.Contains(result.Points, p => p.Succeeded);
    }
}
