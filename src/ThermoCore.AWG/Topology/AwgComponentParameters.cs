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

/// <summary>Prescribed-flow fan parameters for the process airflow driver.</summary>
public sealed record AwgFanParameters
{
    public required double DryAirMassFlowKgPerSecond { get; init; }

    public required double PressureRisePa { get; init; }

    public double FanEfficiency { get; init; } = 0.60;

    public double DriverEfficiency { get; init; } = 0.90;

    public AwgFanParameters Validate()
    {
        FiniteNumber.RequirePositive(DryAirMassFlowKgPerSecond, nameof(DryAirMassFlowKgPerSecond));
        FiniteNumber.RequireNonNegative(PressureRisePa, nameof(PressureRisePa));
        FiniteNumber.RequirePositive(FanEfficiency, nameof(FanEfficiency));
        FiniteNumber.RequirePositive(DriverEfficiency, nameof(DriverEfficiency));
        if (FanEfficiency > 1.0 || DriverEfficiency > 1.0)
        {
            throw new ArgumentOutOfRangeException("Fan and driver efficiencies must be in (0, 1].");
        }

        return this;
    }
}

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

/// <summary>Condenser parameters for the MVP bypass-factor model.</summary>
public sealed record AwgCondenserParameters
{
    public required double BypassFactor { get; init; }

    public required double DrainageEfficiency { get; init; }

    public required double FallbackSurfaceTemperatureK { get; init; }

    public required double FallbackAvailableCoolingPowerW { get; init; }

    public double MaximumRetainedFilmKg { get; init; } = 0.05;

    public double FilmCarryoverFraction { get; init; }

    public AwgCondenserParameters Validate()
    {
        FiniteNumber.Require(BypassFactor, nameof(BypassFactor));
        if (BypassFactor is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(BypassFactor));
        }

        FiniteNumber.Require(DrainageEfficiency, nameof(DrainageEfficiency));
        if (DrainageEfficiency is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(DrainageEfficiency));
        }

        FiniteNumber.RequirePositive(FallbackSurfaceTemperatureK, nameof(FallbackSurfaceTemperatureK));
        FiniteNumber.RequireNonNegative(FallbackAvailableCoolingPowerW, nameof(FallbackAvailableCoolingPowerW));
        FiniteNumber.RequireNonNegative(MaximumRetainedFilmKg, nameof(MaximumRetainedFilmKg));
        FiniteNumber.Require(FilmCarryoverFraction, nameof(FilmCarryoverFraction));
        if (FilmCarryoverFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(FilmCarryoverFraction));
        }

        return this;
    }
}

/// <summary>Constant-efficiency PV parameters for the MVP electrical subsystem.</summary>
public sealed record AwgPvParameters
{
    public required double EfficiencyFraction { get; init; }

    public required double AreaM2 { get; init; }

    public AwgPvParameters Validate()
    {
        FiniteNumber.Require(EfficiencyFraction, nameof(EfficiencyFraction));
        if (EfficiencyFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(EfficiencyFraction));
        }

        FiniteNumber.RequirePositive(AreaM2, nameof(AreaM2));
        return this;
    }
}
