using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Topology;

/// <summary>Dynamic lumped solar-collector parameters used by the MVP builder.</summary>
public sealed record AwgSolarCollectorParameters
{
    public required double OpticalEfficiencyFraction { get; init; }

    public required double ApertureAreaM2 { get; init; }

    public required double EffectiveThermalCapacityJPerK { get; init; }

    public required double AbsorberToAirUaWPerK { get; init; }

    public required double OverallLossCoefficientWPerM2K { get; init; }

    public double IncidenceAngleModifierFraction { get; init; } = 1.0;

    public double WindSpeedMPerSecond { get; init; }

    public double WindLossCoefficientWPerM2KPerMps { get; init; }

    public double MaximumAllowedAbsorberTemperatureK { get; init; } = 423.15;

    public AwgSolarCollectorParameters Validate()
    {
        FiniteNumber.Require(OpticalEfficiencyFraction, nameof(OpticalEfficiencyFraction));
        if (OpticalEfficiencyFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(OpticalEfficiencyFraction));
        }

        FiniteNumber.RequirePositive(ApertureAreaM2, nameof(ApertureAreaM2));
        FiniteNumber.RequirePositive(EffectiveThermalCapacityJPerK, nameof(EffectiveThermalCapacityJPerK));
        FiniteNumber.RequirePositive(AbsorberToAirUaWPerK, nameof(AbsorberToAirUaWPerK));
        FiniteNumber.RequireNonNegative(OverallLossCoefficientWPerM2K, nameof(OverallLossCoefficientWPerM2K));
        FiniteNumber.RequirePositive(IncidenceAngleModifierFraction, nameof(IncidenceAngleModifierFraction));
        FiniteNumber.RequireNonNegative(WindSpeedMPerSecond, nameof(WindSpeedMPerSecond));
        FiniteNumber.RequireNonNegative(WindLossCoefficientWPerM2KPerMps, nameof(WindLossCoefficientWPerM2KPerMps));
        FiniteNumber.RequirePositive(MaximumAllowedAbsorberTemperatureK, nameof(MaximumAllowedAbsorberTemperatureK));
        return this;
    }
}
