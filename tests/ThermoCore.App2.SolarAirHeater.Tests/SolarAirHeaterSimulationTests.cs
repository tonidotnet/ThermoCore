using ThermoCore.App2.SolarAirHeater;

namespace ThermoCore.App2.SolarAirHeater.Tests;

public class SolarAirHeaterSimulationTests
{
    [Fact]
    public void Run_DefaultConfiguration_HeatsAirAndMatchesCollectorEfficiency()
    {
        var configuration = new SolarAirHeaterConfiguration();
        var result = new SolarAirHeaterSimulationRunner().Run(configuration);

        Assert.True(
            result.EngineResult.Succeeded,
            string.Join("; ", result.EngineResult.Diagnostics.Select(d => $"{d.Code}:{d.Message}")));
        Assert.True(result.TemperatureRiseK > 1.0, $"ΔT was {result.TemperatureRiseK}");
        Assert.Equal(
            configuration.CollectorEfficiencyFraction,
            result.SolarUtilizationFraction,
            precision: 6);
        Assert.False(string.IsNullOrWhiteSpace(result.BuiltSystem.GraphFingerprint));
    }
}
