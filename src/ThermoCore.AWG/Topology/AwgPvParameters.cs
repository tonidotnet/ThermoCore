using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Topology;

/// <summary>PV parameters for constant-efficiency or dynamic rear-air models.</summary>
public sealed record AwgPvParameters
{
    public required double EfficiencyFraction { get; init; }

    public required double AreaM2 { get; init; }

    public double RatedPowerW { get; init; } = 270.0;

    public double OpticalAbsorptanceFraction { get; init; } = 0.90;

    public double EffectiveThermalCapacityJPerK { get; init; } = 5_000.0;

    public double EnvironmentalLossUaWPerK { get; init; } = 12.0;

    public double RearAirUaWPerK { get; init; } = 35.0;

    public AwgPvParameters Validate()
    {
        FiniteNumber.Require(EfficiencyFraction, nameof(EfficiencyFraction));
        if (EfficiencyFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(EfficiencyFraction));
        }

        FiniteNumber.RequirePositive(AreaM2, nameof(AreaM2));
        FiniteNumber.RequirePositive(RatedPowerW, nameof(RatedPowerW));
        FiniteNumber.Require(OpticalAbsorptanceFraction, nameof(OpticalAbsorptanceFraction));
        if (OpticalAbsorptanceFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(OpticalAbsorptanceFraction));
        }

        FiniteNumber.RequirePositive(EffectiveThermalCapacityJPerK, nameof(EffectiveThermalCapacityJPerK));
        FiniteNumber.RequireNonNegative(EnvironmentalLossUaWPerK, nameof(EnvironmentalLossUaWPerK));
        FiniteNumber.RequireNonNegative(RearAirUaWPerK, nameof(RearAirUaWPerK));
        return this;
    }
}
