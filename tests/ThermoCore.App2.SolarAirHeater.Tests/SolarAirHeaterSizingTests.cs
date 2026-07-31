using ThermoCore.App2.SolarAirHeater;

namespace ThermoCore.App2.SolarAirHeater.Tests;

public class SolarAirHeaterSizingTests
{
    [Fact]
    public void Sizing_RanksLargerApertureHigherUsefulHeat()
    {
        var result = new SolarAirHeaterSizingRunner().Run(
            new SolarAirHeaterConfiguration(),
            apertureAreasM2: [1.0, 3.0],
            dryAirMassFlowsKgPerSecond: [0.05],
            irradiancesWPerM2: [800.0]);

        Assert.Equal(2, result.Points.Count);
        Assert.All(result.Points, p => Assert.True(p.Succeeded, p.FailureMessage));
        Assert.NotNull(result.BestUsefulHeat);
        Assert.Equal(3.0, result.BestUsefulHeat!.ApertureAreaM2);
        Assert.True(result.BestUsefulHeat.UsefulHeatW > result.Points.Min(p => p.UsefulHeatW));
    }
}
