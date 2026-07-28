using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Numerics;

/// <summary>
/// Approximate equality using absolute and relative tolerances together.
/// </summary>
public static class NumericComparisons
{
    public static bool AreApproximatelyEqual(
        double a,
        double b,
        double absoluteTolerance,
        double relativeTolerance)
    {
        FiniteNumber.Require(a, nameof(a));
        FiniteNumber.Require(b, nameof(b));
        FiniteNumber.RequireNonNegative(absoluteTolerance, nameof(absoluteTolerance));
        FiniteNumber.RequireNonNegative(relativeTolerance, nameof(relativeTolerance));

        var difference = Math.Abs(a - b);
        var scale = Math.Max(Math.Abs(a), Math.Abs(b));
        return difference <= absoluteTolerance + relativeTolerance * scale;
    }

    public static bool AreApproximatelyEqual(double a, double b, NumericalTolerances tolerances)
    {
        ArgumentNullException.ThrowIfNull(tolerances);
        tolerances.Validate();
        return AreApproximatelyEqual(a, b, tolerances.Absolute, tolerances.Relative);
    }

    public static bool AreTemperaturesEqual(double temperatureAK, double temperatureBK, NumericalTolerances? tolerances = null)
    {
        var t = (tolerances ?? NumericalTolerances.Default).Validate();
        return AreApproximatelyEqual(temperatureAK, temperatureBK, t.TemperatureK, t.Relative);
    }

    public static bool ArePressuresEqual(double pressureAPa, double pressureBPa, NumericalTolerances? tolerances = null)
    {
        var t = (tolerances ?? NumericalTolerances.Default).Validate();
        return AreApproximatelyEqual(pressureAPa, pressureBPa, t.PressurePa, t.Relative);
    }
}
