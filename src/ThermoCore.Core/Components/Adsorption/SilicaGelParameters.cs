using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Adsorption;

/// <summary>
/// Engineering parameters for the LDF silica-gel bed (docs/03_Components/09_SilicaGel.md §7).
/// </summary>
public sealed record SilicaGelParameters
{
    public required double DryAdsorbentMassKg { get; init; }

    public required double MaximumWaterLoadingKgPerKgDryAdsorbent { get; init; }

    public required double MinimumRegeneratedLoadingKgPerKgDryAdsorbent { get; init; }

    public required double EffectiveSpecificHeatJPerKgK { get; init; }

    public required double BedHousingThermalCapacityJPerK { get; init; }

    public required double EffectiveHeatOfAdsorptionJPerKgWater { get; init; }

    public required double BedHeatLossCoefficientWPerK { get; init; }

    public required double ReferenceMassTransferCoefficientPerSecond { get; init; }

    public double ActivationEnergyJPerMol { get; init; }

    public double ReferenceKineticTemperatureK { get; init; } = 298.15;

    public double AmbientTemperatureK { get; init; } = 298.15;

    public double AirBedHeatTransferCoefficientWPerK { get; init; } = 50.0;

    public double NearEquilibriumLoadingToleranceKgPerKg { get; init; } = 1e-4;

    /// <summary>
    /// When true, desorption magnitude is capped by available heat
    /// (docs/03_Components/09_SilicaGel.md §65–§66 / SG-008).
    /// </summary>
    public bool EnableEnergyLimitedDesorption { get; init; } = true;

    /// <summary>
    /// Lowest bed temperature allowed when drawing stored thermal energy for desorption.
    /// </summary>
    public double MinimumDesorptionBedTemperatureK { get; init; } = 250.0;

    /// <summary>Reference pressure drop for quadratic flow scaling (SG-009 simple model).</summary>
    public double ReferencePressureDropPa { get; init; }

    public double ReferenceVolumetricFlowM3PerSecond { get; init; } = 0.01;

    public double PressureDropFlowExponent { get; init; } = 2.0;

    /// <summary>When true and bed geometry is set, use Ergun packed-bed Δp.</summary>
    public bool EnableErgunPressureDrop { get; init; }

    public double BedVoidFraction { get; init; } = 0.4;

    public double BedCrossSectionAreaM2 { get; init; }

    public double BedLengthM { get; init; }

    public double ParticleDiameterM { get; init; }

    public SilicaGelParameters Validate()
    {
        FiniteNumber.RequirePositive(DryAdsorbentMassKg, nameof(DryAdsorbentMassKg));
        FiniteNumber.RequirePositive(MaximumWaterLoadingKgPerKgDryAdsorbent, nameof(MaximumWaterLoadingKgPerKgDryAdsorbent));
        FiniteNumber.RequireNonNegative(MinimumRegeneratedLoadingKgPerKgDryAdsorbent, nameof(MinimumRegeneratedLoadingKgPerKgDryAdsorbent));
        FiniteNumber.RequirePositive(EffectiveSpecificHeatJPerKgK, nameof(EffectiveSpecificHeatJPerKgK));
        FiniteNumber.RequireNonNegative(BedHousingThermalCapacityJPerK, nameof(BedHousingThermalCapacityJPerK));
        FiniteNumber.RequirePositive(EffectiveHeatOfAdsorptionJPerKgWater, nameof(EffectiveHeatOfAdsorptionJPerKgWater));
        FiniteNumber.RequireNonNegative(BedHeatLossCoefficientWPerK, nameof(BedHeatLossCoefficientWPerK));
        FiniteNumber.RequirePositive(ReferenceMassTransferCoefficientPerSecond, nameof(ReferenceMassTransferCoefficientPerSecond));
        FiniteNumber.RequireNonNegative(ActivationEnergyJPerMol, nameof(ActivationEnergyJPerMol));
        FiniteNumber.RequirePositive(ReferenceKineticTemperatureK, nameof(ReferenceKineticTemperatureK));
        FiniteNumber.RequirePositive(AmbientTemperatureK, nameof(AmbientTemperatureK));
        FiniteNumber.RequireNonNegative(AirBedHeatTransferCoefficientWPerK, nameof(AirBedHeatTransferCoefficientWPerK));
        FiniteNumber.RequirePositive(NearEquilibriumLoadingToleranceKgPerKg, nameof(NearEquilibriumLoadingToleranceKgPerKg));
        FiniteNumber.RequirePositive(MinimumDesorptionBedTemperatureK, nameof(MinimumDesorptionBedTemperatureK));
        FiniteNumber.RequireNonNegative(ReferencePressureDropPa, nameof(ReferencePressureDropPa));
        FiniteNumber.RequirePositive(ReferenceVolumetricFlowM3PerSecond, nameof(ReferenceVolumetricFlowM3PerSecond));
        FiniteNumber.RequirePositive(PressureDropFlowExponent, nameof(PressureDropFlowExponent));
        FiniteNumber.Require(BedVoidFraction, nameof(BedVoidFraction));
        FiniteNumber.RequireNonNegative(BedCrossSectionAreaM2, nameof(BedCrossSectionAreaM2));
        FiniteNumber.RequireNonNegative(BedLengthM, nameof(BedLengthM));
        FiniteNumber.RequireNonNegative(ParticleDiameterM, nameof(ParticleDiameterM));

        if (BedVoidFraction is <= 0.0 or >= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(BedVoidFraction), "Bed void fraction must be in (0, 1).");
        }

        if (MinimumRegeneratedLoadingKgPerKgDryAdsorbent > MaximumWaterLoadingKgPerKgDryAdsorbent)
        {
            throw new ArgumentException(
                "Minimum regenerated loading must not exceed maximum water loading.");
        }

        if (EnableErgunPressureDrop
            && (BedCrossSectionAreaM2 <= 0.0 || BedLengthM <= 0.0 || ParticleDiameterM <= 0.0))
        {
            throw new ArgumentException(
                "Ergun pressure drop requires positive bed area, length, and particle diameter.");
        }

        return this;
    }
}
