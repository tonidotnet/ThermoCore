using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

public class DeterminismTests
{
    [Fact]
    public void PsychrometricCalculator_RepeatedRuns_AreIdentical()
    {
        var calculator = new PsychrometricCalculator();
        MoistAirState Create() => calculator.CreateFromRelativeHumidity(
            UnitConversions.CelsiusToKelvin(25.0),
            PhysicalConstants.StandardAtmosphericPressurePa,
            relativeHumidityFraction: 0.50,
            dryAirMassFlowKgPerSecond: 0.01);

        var first = Create();
        var second = Create();

        Assert.Equal(first.HumidityRatioKgPerKgDryAir, second.HumidityRatioKgPerKgDryAir);
        Assert.Equal(first.DewPointTemperatureK, second.DewPointTemperatureK);
        Assert.Equal(first.SpecificEnthalpyJPerKgDryAir, second.SpecificEnthalpyJPerKgDryAir);
        Assert.Equal(first.SpecificVolumeM3PerKgDryAir, second.SpecificVolumeM3PerKgDryAir);
        Assert.Equal(first.RelativeHumidityFraction, second.RelativeHumidityFraction);
    }
}
