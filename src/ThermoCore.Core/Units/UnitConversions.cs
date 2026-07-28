using ThermoCore.Core.Physics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Units;

/// <summary>
/// Explicit SI boundary conversions. Internal calculation units remain SI
/// (docs/02_Mathematics/27_Units.md).
/// </summary>
public static class UnitConversions
{
    public static double CelsiusToKelvin(double temperatureC)
    {
        FiniteNumber.Require(temperatureC, nameof(temperatureC));
        return temperatureC + PhysicalConstants.CelsiusOffsetK;
    }

    public static double KelvinToCelsius(double temperatureK)
    {
        FiniteNumber.Require(temperatureK, nameof(temperatureK));
        return temperatureK - PhysicalConstants.CelsiusOffsetK;
    }

    public static double RelativeHumidityPercentToFraction(double relativeHumidityPercent)
    {
        FiniteNumber.Require(relativeHumidityPercent, nameof(relativeHumidityPercent));
        return relativeHumidityPercent / 100.0;
    }

    public static double RelativeHumidityFractionToPercent(double relativeHumidityFraction)
    {
        FiniteNumber.Require(relativeHumidityFraction, nameof(relativeHumidityFraction));
        return relativeHumidityFraction * 100.0;
    }

    public static double CubicMetresPerHourToCubicMetresPerSecond(double volumetricFlowM3PerHour)
    {
        FiniteNumber.Require(volumetricFlowM3PerHour, nameof(volumetricFlowM3PerHour));
        return volumetricFlowM3PerHour / 3600.0;
    }

    public static double CubicMetresPerSecondToCubicMetresPerHour(double volumetricFlowM3PerSecond)
    {
        FiniteNumber.Require(volumetricFlowM3PerSecond, nameof(volumetricFlowM3PerSecond));
        return volumetricFlowM3PerSecond * 3600.0;
    }

    public static double WattHoursToJoules(double energyWh)
    {
        FiniteNumber.Require(energyWh, nameof(energyWh));
        return energyWh * 3600.0;
    }

    public static double JoulesToWattHours(double energyJ)
    {
        FiniteNumber.Require(energyJ, nameof(energyJ));
        return energyJ / 3600.0;
    }

    public static double KilowattHoursToJoules(double energyKWh)
    {
        FiniteNumber.Require(energyKWh, nameof(energyKWh));
        return energyKWh * 3_600_000.0;
    }

    public static double JoulesToKilowattHours(double energyJ)
    {
        FiniteNumber.Require(energyJ, nameof(energyJ));
        return energyJ / 3_600_000.0;
    }

    public static double KilopascalsToPascals(double pressureKPa)
    {
        FiniteNumber.Require(pressureKPa, nameof(pressureKPa));
        return pressureKPa * 1000.0;
    }

    public static double PascalsToKilopascals(double pressurePa)
    {
        FiniteNumber.Require(pressurePa, nameof(pressurePa));
        return pressurePa / 1000.0;
    }

    public static double HectopascalsToPascals(double pressureHPa)
    {
        FiniteNumber.Require(pressureHPa, nameof(pressureHPa));
        return pressureHPa * 100.0;
    }

    public static double PascalsToHectopascals(double pressurePa)
    {
        FiniteNumber.Require(pressurePa, nameof(pressurePa));
        return pressurePa / 100.0;
    }

    public static double DegreesToRadians(double angleDegrees)
    {
        FiniteNumber.Require(angleDegrees, nameof(angleDegrees));
        return angleDegrees * Math.PI / 180.0;
    }

    public static double RadiansToDegrees(double angleRadians)
    {
        FiniteNumber.Require(angleRadians, nameof(angleRadians));
        return angleRadians * 180.0 / Math.PI;
    }

    public static double MassKgToVolumeM3(double massKg, double densityKgPerM3)
    {
        FiniteNumber.Require(massKg, nameof(massKg));
        FiniteNumber.RequirePositive(densityKgPerM3, nameof(densityKgPerM3));
        return massKg / densityKgPerM3;
    }
}
