using ThermoCore.Core.Physics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Psychrometrics;

/// <summary>
/// Buck (1981) saturation vapor pressure over liquid water.
/// Validated engineering range: -45 °C to 100 °C.
/// Below 0 °C the formula is used over supercooled liquid water.
/// </summary>
public sealed class BuckSaturationPressureProvider : ISaturationPressureProvider
{
    public static BuckSaturationPressureProvider Instance { get; } = new();

    public SaturationPressureModelInfo ModelInfo { get; } = new()
    {
        ModelName = "Buck1981LiquidWater",
        MinimumTemperatureK = PhysicalConstants.CelsiusOffsetK - 45.0,
        MaximumTemperatureK = PhysicalConstants.CelsiusOffsetK + 100.0,
        Reference = "Buck, A. L. (1981). New equations for computing vapor pressure and enhancement factor. J. Appl. Meteorol.",
        PhaseBasis = "LiquidWaterIncludingSupercooled"
    };

    public double CalculatePressurePa(double temperatureK)
    {
        FiniteNumber.Require(temperatureK, nameof(temperatureK));

        if (temperatureK < ModelInfo.MinimumTemperatureK || temperatureK > ModelInfo.MaximumTemperatureK)
        {
            throw new PsychrometricInputException(
                $"Temperature {temperatureK} K is outside the Buck model range " +
                $"[{ModelInfo.MinimumTemperatureK}, {ModelInfo.MaximumTemperatureK}] K.");
        }

        var temperatureC = temperatureK - PhysicalConstants.CelsiusOffsetK;
        var denominator = 257.14 + temperatureC;
        if (Math.Abs(denominator) < 1e-12)
        {
            throw new PsychrometricStateException("Buck equation denominator is singular.");
        }

        var exponent = (18.678 - temperatureC / 234.5) * (temperatureC / denominator);
        var pressurePa = 611.21 * Math.Exp(exponent);
        FiniteNumber.RequirePositive(pressurePa, nameof(pressurePa));
        return pressurePa;
    }
}
