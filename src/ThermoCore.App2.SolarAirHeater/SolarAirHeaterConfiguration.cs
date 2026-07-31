using ThermoCore.Core.Validation;

namespace ThermoCore.App2.SolarAirHeater;

/// <summary>MVP configuration for a forced-air solar collector chain (APP2).</summary>
public sealed record SolarAirHeaterConfiguration
{
    public double AmbientTemperatureK { get; init; } = 288.15;

    public double AmbientRelativeHumidityFraction { get; init; } = 0.40;

    public double AmbientPressurePa { get; init; } = 101_325.0;

    public double DryAirMassFlowKgPerSecond { get; init; } = 0.05;

    public double FanPressureRisePa { get; init; } = 80.0;

    public double CollectorEfficiencyFraction { get; init; } = 0.55;

    public double CollectorApertureAreaM2 { get; init; } = 2.0;

    public double SolarIrradianceWPerM2 { get; init; } = 800.0;

    public SolarAirHeaterConfiguration Validate()
    {
        FiniteNumber.RequirePositive(AmbientTemperatureK, nameof(AmbientTemperatureK));
        FiniteNumber.Require(AmbientRelativeHumidityFraction, nameof(AmbientRelativeHumidityFraction));
        if (AmbientRelativeHumidityFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(AmbientRelativeHumidityFraction));
        }

        FiniteNumber.RequirePositive(AmbientPressurePa, nameof(AmbientPressurePa));
        FiniteNumber.RequirePositive(DryAirMassFlowKgPerSecond, nameof(DryAirMassFlowKgPerSecond));
        FiniteNumber.RequireNonNegative(FanPressureRisePa, nameof(FanPressureRisePa));
        FiniteNumber.Require(CollectorEfficiencyFraction, nameof(CollectorEfficiencyFraction));
        if (CollectorEfficiencyFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(CollectorEfficiencyFraction));
        }

        FiniteNumber.RequirePositive(CollectorApertureAreaM2, nameof(CollectorApertureAreaM2));
        FiniteNumber.RequireNonNegative(SolarIrradianceWPerM2, nameof(SolarIrradianceWPerM2));
        return this;
    }
}
