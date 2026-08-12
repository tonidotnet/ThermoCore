using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Hybrid;

/// <summary>One hybrid comparison case (climate × architecture variant).</summary>
public sealed record HybridComparisonScenario
{
    public required string ScenarioId { get; init; }

    public required HybridComparisonVariant Variant { get; init; }

    public required double AmbientTemperatureC { get; init; }

    public required double RelativeHumidityFraction { get; init; }

    public double DryAirMassFlowKgPerSecond { get; init; } = 0.02;

    /// <summary>Heating-only control temperature rise (K).</summary>
    public double HeatingTemperatureRiseK { get; init; } = 25.0;

    /// <summary>Regeneration dry-bulb (°C) for sorbent+ variants.</summary>
    public double RegenerationTemperatureC { get; init; } = 55.0;

    /// <summary>Dew-point boost applied to regeneration stream (K).</summary>
    public double RegenerationDewPointBoostK { get; init; } = 8.0;

    /// <summary>Cold/evaporating surface temperature (°C).</summary>
    public double CoolingSurfaceTemperatureC { get; init; } = 8.0;

    /// <summary>Condensing / rejection temperature (°C) for compressor variants.</summary>
    public double CondensingTemperatureC { get; init; } = 40.0;

    public double TecAvailableCoolingPowerW { get; init; } = 120.0;

    public double CompressorSpeedFraction { get; init; } = 1.0;

    public double ProcessFanElectricalPowerW { get; init; } = 10.0;

    public TimeSpan TimeStep { get; init; } = TimeSpan.FromSeconds(1);

    public string? Notes { get; init; }

    public HybridComparisonScenario Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ScenarioId);
        if (!Enum.IsDefined(Variant))
        {
            throw new ArgumentOutOfRangeException(nameof(Variant));
        }

        FiniteNumber.Require(AmbientTemperatureC, nameof(AmbientTemperatureC));
        FiniteNumber.Require(RelativeHumidityFraction, nameof(RelativeHumidityFraction));
        if (RelativeHumidityFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(RelativeHumidityFraction));
        }

        FiniteNumber.RequirePositive(DryAirMassFlowKgPerSecond, nameof(DryAirMassFlowKgPerSecond));
        FiniteNumber.RequireNonNegative(HeatingTemperatureRiseK, nameof(HeatingTemperatureRiseK));
        FiniteNumber.Require(RegenerationTemperatureC, nameof(RegenerationTemperatureC));
        FiniteNumber.RequireNonNegative(RegenerationDewPointBoostK, nameof(RegenerationDewPointBoostK));
        FiniteNumber.Require(CoolingSurfaceTemperatureC, nameof(CoolingSurfaceTemperatureC));
        FiniteNumber.Require(CondensingTemperatureC, nameof(CondensingTemperatureC));
        FiniteNumber.RequireNonNegative(TecAvailableCoolingPowerW, nameof(TecAvailableCoolingPowerW));
        FiniteNumber.Require(CompressorSpeedFraction, nameof(CompressorSpeedFraction));
        if (CompressorSpeedFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(CompressorSpeedFraction));
        }

        FiniteNumber.RequireNonNegative(ProcessFanElectricalPowerW, nameof(ProcessFanElectricalPowerW));
        if (TimeStep <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(TimeStep));
        }

        return this;
    }
}
