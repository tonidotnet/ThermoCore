using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Topology;

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
