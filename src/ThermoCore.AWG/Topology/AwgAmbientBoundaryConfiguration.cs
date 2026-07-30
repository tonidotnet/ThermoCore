using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Topology;

/// <summary>Ambient moist-air boundary used to construct the process inlet state.</summary>
public sealed record AwgAmbientBoundaryConfiguration
{
    public required double TemperatureK { get; init; }

    public required double PressurePa { get; init; }

    public required double RelativeHumidityFraction { get; init; }

    public required double DryAirMassFlowKgPerSecond { get; init; }

    public double SolarIrradianceWPerSquareMeter { get; init; }

    public AwgAmbientBoundaryConfiguration Validate()
    {
        FiniteNumber.RequirePositive(TemperatureK, nameof(TemperatureK));
        FiniteNumber.RequirePositive(PressurePa, nameof(PressurePa));
        FiniteNumber.Require(RelativeHumidityFraction, nameof(RelativeHumidityFraction));
        if (RelativeHumidityFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RelativeHumidityFraction),
                "Relative humidity must be in [0, 1].");
        }

        FiniteNumber.RequirePositive(DryAirMassFlowKgPerSecond, nameof(DryAirMassFlowKgPerSecond));
        FiniteNumber.RequireNonNegative(SolarIrradianceWPerSquareMeter, nameof(SolarIrradianceWPerSquareMeter));
        return this;
    }
}
