using ThermoCore.Core.Numerics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Tests;

public class NumericsAndValidationTests
{
    [Fact]
    public void FiniteNumber_Require_RejectsInfinity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FiniteNumber.Require(double.PositiveInfinity, "x"));
    }

    [Fact]
    public void FiniteNumber_RequirePositive_RejectsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FiniteNumber.RequirePositive(0.0, "x"));
    }

    [Fact]
    public void AreApproximatelyEqual_WithinRelativeTolerance_ReturnsTrue()
    {
        var equal = NumericComparisons.AreApproximatelyEqual(1000.0, 1000.00005, NumericalTolerances.Default);
        Assert.True(equal);
    }

    [Fact]
    public void AreApproximatelyEqual_FarApart_ReturnsFalse()
    {
        var equal = NumericComparisons.AreApproximatelyEqual(1.0, 2.0, NumericalTolerances.Default);
        Assert.False(equal);
    }

    [Fact]
    public void NumericalTolerances_Validate_RejectsNonPositiveAbsolute()
    {
        var tolerances = new NumericalTolerances { Absolute = 0.0 };
        Assert.Throws<ArgumentOutOfRangeException>(() => tolerances.Validate());
    }

    [Fact]
    public void AreTemperaturesEqual_UsesTemperatureTolerance()
    {
        Assert.True(NumericComparisons.AreTemperaturesEqual(300.0, 300.00005));
        Assert.False(NumericComparisons.AreTemperaturesEqual(300.0, 301.0));
    }
}
