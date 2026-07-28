using ThermoCore.Core.Physics;
using ThermoCore.Core.Units;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Tests;

public class UnitConversionsTests
{
    [Fact]
    public void CelsiusToKelvin_ZeroCelsius_Returns273_15()
    {
        var kelvin = UnitConversions.CelsiusToKelvin(0.0);
        Assert.Equal(PhysicalConstants.CelsiusOffsetK, kelvin);
    }

    [Fact]
    public void KelvinToCelsius_273_15_ReturnsZero()
    {
        var celsius = UnitConversions.KelvinToCelsius(PhysicalConstants.CelsiusOffsetK);
        Assert.Equal(0.0, celsius);
    }

    [Fact]
    public void RelativeHumidity_RoundTrip_PreservesValue()
    {
        const double percent = 55.0;
        var fraction = UnitConversions.RelativeHumidityPercentToFraction(percent);
        var back = UnitConversions.RelativeHumidityFractionToPercent(fraction);
        Assert.Equal(percent, back, precision: 12);
        Assert.Equal(0.55, fraction, precision: 12);
    }

    [Fact]
    public void Airflow_RoundTrip_PreservesValue()
    {
        const double m3PerHour = 360.0;
        var m3PerSecond = UnitConversions.CubicMetresPerHourToCubicMetresPerSecond(m3PerHour);
        Assert.Equal(0.1, m3PerSecond);
        Assert.Equal(m3PerHour, UnitConversions.CubicMetresPerSecondToCubicMetresPerHour(m3PerSecond));
    }

    [Fact]
    public void Energy_WattHoursToJoules_Uses3600()
    {
        Assert.Equal(3600.0, UnitConversions.WattHoursToJoules(1.0));
        Assert.Equal(1.0, UnitConversions.JoulesToWattHours(3600.0));
    }

    [Fact]
    public void CelsiusToKelvin_NaN_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UnitConversions.CelsiusToKelvin(double.NaN));
    }
}
