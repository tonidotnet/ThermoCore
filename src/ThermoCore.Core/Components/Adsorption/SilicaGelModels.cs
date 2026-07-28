using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Adsorption;

public enum SilicaGelOperatingRegime
{
    Idle,
    Adsorption,
    Desorption,
    NearEquilibrium,
    Saturated,
    Regenerated,
    Invalid
}

/// <summary>
/// Immutable silica-gel bed state (docs/03_Components/09_SilicaGel.md §6).
/// </summary>
public sealed record SilicaGelState
{
    public required double DryAdsorbentMassKg { get; init; }

    public required double AdsorbedWaterMassKg { get; init; }

    public required double WaterLoadingKgPerKgDryAdsorbent { get; init; }

    public required double BedTemperatureK { get; init; }

    public required double StoredThermalEnergyJ { get; init; }

    public required double EquilibriumLoadingKgPerKgDryAdsorbent { get; init; }

    public required double LoadingFraction { get; init; }

    public required double LastWaterTransferRateKgPerSecond { get; init; }

    public required double LastAdsorptionHeatW { get; init; }

    public required SilicaGelOperatingRegime OperatingRegime { get; init; }

    public required bool HasReachedEquilibrium { get; init; }

    public static SilicaGelState Create(
        double dryAdsorbentMassKg,
        double waterLoadingKgPerKgDryAdsorbent,
        double bedTemperatureK,
        double maximumWaterLoadingKgPerKgDryAdsorbent,
        double minimumRegeneratedLoadingKgPerKgDryAdsorbent,
        double effectiveSpecificHeatJPerKgK,
        double bedHousingThermalCapacityJPerK,
        double equilibriumLoadingKgPerKgDryAdsorbent = 0.0,
        double lastWaterTransferRateKgPerSecond = 0.0,
        double lastAdsorptionHeatW = 0.0,
        SilicaGelOperatingRegime operatingRegime = SilicaGelOperatingRegime.Idle,
        bool hasReachedEquilibrium = false)
    {
        FiniteNumber.RequirePositive(dryAdsorbentMassKg, nameof(dryAdsorbentMassKg));
        FiniteNumber.RequireNonNegative(waterLoadingKgPerKgDryAdsorbent, nameof(waterLoadingKgPerKgDryAdsorbent));
        FiniteNumber.RequirePositive(bedTemperatureK, nameof(bedTemperatureK));
        FiniteNumber.RequirePositive(maximumWaterLoadingKgPerKgDryAdsorbent, nameof(maximumWaterLoadingKgPerKgDryAdsorbent));
        FiniteNumber.RequireNonNegative(minimumRegeneratedLoadingKgPerKgDryAdsorbent, nameof(minimumRegeneratedLoadingKgPerKgDryAdsorbent));
        FiniteNumber.RequirePositive(effectiveSpecificHeatJPerKgK, nameof(effectiveSpecificHeatJPerKgK));
        FiniteNumber.RequireNonNegative(bedHousingThermalCapacityJPerK, nameof(bedHousingThermalCapacityJPerK));
        FiniteNumber.RequireNonNegative(equilibriumLoadingKgPerKgDryAdsorbent, nameof(equilibriumLoadingKgPerKgDryAdsorbent));
        FiniteNumber.Require(lastWaterTransferRateKgPerSecond, nameof(lastWaterTransferRateKgPerSecond));
        FiniteNumber.Require(lastAdsorptionHeatW, nameof(lastAdsorptionHeatW));

        if (minimumRegeneratedLoadingKgPerKgDryAdsorbent > maximumWaterLoadingKgPerKgDryAdsorbent)
        {
            throw new ArgumentException(
                "Minimum regenerated loading must not exceed maximum water loading.",
                nameof(minimumRegeneratedLoadingKgPerKgDryAdsorbent));
        }

        if (waterLoadingKgPerKgDryAdsorbent > maximumWaterLoadingKgPerKgDryAdsorbent)
        {
            throw new ArgumentOutOfRangeException(
                nameof(waterLoadingKgPerKgDryAdsorbent),
                "Water loading exceeds configured maximum capacity.");
        }

        var adsorbedWaterMassKg = waterLoadingKgPerKgDryAdsorbent * dryAdsorbentMassKg;
        var loadingSpan = maximumWaterLoadingKgPerKgDryAdsorbent - minimumRegeneratedLoadingKgPerKgDryAdsorbent;
        var loadingFraction = loadingSpan <= 0.0
            ? 0.0
            : Math.Clamp(
                (waterLoadingKgPerKgDryAdsorbent - minimumRegeneratedLoadingKgPerKgDryAdsorbent) / loadingSpan,
                0.0,
                1.0);

        var storedThermalEnergyJ =
            (dryAdsorbentMassKg * effectiveSpecificHeatJPerKgK
             + adsorbedWaterMassKg * Physics.ReferenceThermophysicalProperties.LiquidWaterSpecificHeatJPerKgK
             + bedHousingThermalCapacityJPerK)
            * bedTemperatureK;

        return new SilicaGelState
        {
            DryAdsorbentMassKg = dryAdsorbentMassKg,
            AdsorbedWaterMassKg = adsorbedWaterMassKg,
            WaterLoadingKgPerKgDryAdsorbent = waterLoadingKgPerKgDryAdsorbent,
            BedTemperatureK = bedTemperatureK,
            StoredThermalEnergyJ = storedThermalEnergyJ,
            EquilibriumLoadingKgPerKgDryAdsorbent = equilibriumLoadingKgPerKgDryAdsorbent,
            LoadingFraction = loadingFraction,
            LastWaterTransferRateKgPerSecond = lastWaterTransferRateKgPerSecond,
            LastAdsorptionHeatW = lastAdsorptionHeatW,
            OperatingRegime = operatingRegime,
            HasReachedEquilibrium = hasReachedEquilibrium
        };
    }
}

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

        if (MinimumRegeneratedLoadingKgPerKgDryAdsorbent > MaximumWaterLoadingKgPerKgDryAdsorbent)
        {
            throw new ArgumentException(
                "Minimum regenerated loading must not exceed maximum water loading.");
        }

        return this;
    }
}
