using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;
using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Hybrid;

/// <summary>Builds comparable moist-air streams for hybrid scenario variants.</summary>
public static class HybridStreamFactory
{
    /// <summary>
    /// Scientific heating-only control: raise dry-bulb at constant humidity ratio
    /// (does not increase dew point).
    /// </summary>
    public static MoistAirState CreateHeatedControlStream(
        MoistAirState ambient,
        double temperatureRiseK,
        IPsychrometricCalculator calculator)
    {
        ArgumentNullException.ThrowIfNull(ambient);
        ArgumentNullException.ThrowIfNull(calculator);
        FiniteNumber.RequireNonNegative(temperatureRiseK, nameof(temperatureRiseK));

        return calculator.CreateFromHumidityRatio(
            ambient.TemperatureK + temperatureRiseK,
            ambient.PressurePa,
            ambient.HumidityRatioKgPerKgDryAir,
            ambient.DryAirMassFlowKgPerSecond);
    }

    /// <summary>
    /// Approximate regeneration / desorption stream: higher dew point at a warm
    /// regeneration temperature (sorbent concentration hypothesis).
    /// </summary>
    public static MoistAirState CreateRegenerationStream(
        MoistAirState ambient,
        double regenerationTemperatureK,
        double dewPointBoostK,
        IPsychrometricCalculator calculator)
    {
        ArgumentNullException.ThrowIfNull(ambient);
        ArgumentNullException.ThrowIfNull(calculator);
        FiniteNumber.RequirePositive(regenerationTemperatureK, nameof(regenerationTemperatureK));
        FiniteNumber.RequireNonNegative(dewPointBoostK, nameof(dewPointBoostK));

        var targetDewPointK = ambient.DewPointTemperatureK + dewPointBoostK;
        targetDewPointK = Math.Min(targetDewPointK, regenerationTemperatureK - 0.5);

        return calculator.CreateFromDewPoint(
            regenerationTemperatureK,
            ambient.PressurePa,
            targetDewPointK,
            ambient.DryAirMassFlowKgPerSecond);
    }

    public static MoistAirState CreateAmbient(
        double temperatureC,
        double relativeHumidityFraction,
        double dryAirMassFlowKgPerSecond,
        IPsychrometricCalculator calculator,
        double pressurePa = ThermoCore.Core.Physics.PhysicalConstants.StandardAtmosphericPressurePa)
    {
        ArgumentNullException.ThrowIfNull(calculator);
        FiniteNumber.Require(relativeHumidityFraction, nameof(relativeHumidityFraction));
        if (relativeHumidityFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(relativeHumidityFraction));
        }

        FiniteNumber.RequirePositive(dryAirMassFlowKgPerSecond, nameof(dryAirMassFlowKgPerSecond));

        return calculator.CreateFromRelativeHumidity(
            UnitConversions.CelsiusToKelvin(temperatureC),
            pressurePa,
            relativeHumidityFraction,
            dryAirMassFlowKgPerSecond);
    }
}
