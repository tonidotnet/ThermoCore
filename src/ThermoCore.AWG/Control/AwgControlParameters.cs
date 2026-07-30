using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Control;

/// <summary>Tunable thresholds for the rule-based AWG controller.</summary>
public sealed record AwgControlParameters
{
    public required double AdsorptionTargetLoadingKgPerKg { get; init; }

    public required double RegenerationEntryLoadingKgPerKg { get; init; }

    public required double RegenerationExitLoadingKgPerKg { get; init; }

    public required double MinimumAdsorptionDrivingForceKgPerKg { get; init; }

    public required double CondensationDewPointMarginK { get; init; }

    public required double TargetDewPointApproachK { get; init; }

    public required double MaximumRecirculationFraction { get; init; }

    public required double ReserveBatterySocFraction { get; init; }

    public required double CriticalBatterySocFraction { get; init; }

    public required TimeSpan MinimumModeDwell { get; init; }

    public required double PeltierHotSideLimitK { get; init; }

    public required double SilicaGelTemperatureLimitK { get; init; }

    public required double CollectorAbsorberTemperatureLimitK { get; init; }

    public required double MinimumSafeDryAirMassFlowKgPerSecond { get; init; }

    public double NominalFanControlFraction { get; init; } = 1.0;

    public double NominalPeltierPowerRequestW { get; init; } = 100.0;

    public double MinimumSolarIrradianceForRegenerationWPerSquareMeter { get; init; } = 200.0;

    public double ReservePeltierDerateFraction { get; init; } = 0.25;

    public double DefaultRecirculationFraction { get; init; }

    public int FaultLatchThreshold { get; init; } = 1;

    public AwgControlParameters Validate()
    {
        FiniteNumber.RequirePositive(AdsorptionTargetLoadingKgPerKg, nameof(AdsorptionTargetLoadingKgPerKg));
        FiniteNumber.RequirePositive(RegenerationEntryLoadingKgPerKg, nameof(RegenerationEntryLoadingKgPerKg));
        FiniteNumber.RequireNonNegative(RegenerationExitLoadingKgPerKg, nameof(RegenerationExitLoadingKgPerKg));
        FiniteNumber.RequirePositive(MinimumAdsorptionDrivingForceKgPerKg, nameof(MinimumAdsorptionDrivingForceKgPerKg));
        FiniteNumber.RequireNonNegative(CondensationDewPointMarginK, nameof(CondensationDewPointMarginK));
        FiniteNumber.RequireNonNegative(TargetDewPointApproachK, nameof(TargetDewPointApproachK));
        FiniteNumber.Require(MaximumRecirculationFraction, nameof(MaximumRecirculationFraction));
        FiniteNumber.Require(ReserveBatterySocFraction, nameof(ReserveBatterySocFraction));
        FiniteNumber.Require(CriticalBatterySocFraction, nameof(CriticalBatterySocFraction));
        FiniteNumber.RequirePositive(PeltierHotSideLimitK, nameof(PeltierHotSideLimitK));
        FiniteNumber.RequirePositive(SilicaGelTemperatureLimitK, nameof(SilicaGelTemperatureLimitK));
        FiniteNumber.RequirePositive(CollectorAbsorberTemperatureLimitK, nameof(CollectorAbsorberTemperatureLimitK));
        FiniteNumber.RequirePositive(MinimumSafeDryAirMassFlowKgPerSecond, nameof(MinimumSafeDryAirMassFlowKgPerSecond));
        FiniteNumber.Require(NominalFanControlFraction, nameof(NominalFanControlFraction));
        FiniteNumber.RequireNonNegative(NominalPeltierPowerRequestW, nameof(NominalPeltierPowerRequestW));
        FiniteNumber.RequireNonNegative(MinimumSolarIrradianceForRegenerationWPerSquareMeter, nameof(MinimumSolarIrradianceForRegenerationWPerSquareMeter));
        FiniteNumber.Require(ReservePeltierDerateFraction, nameof(ReservePeltierDerateFraction));
        FiniteNumber.Require(DefaultRecirculationFraction, nameof(DefaultRecirculationFraction));

        if (MinimumModeDwell < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumModeDwell));
        }

        if (RegenerationExitLoadingKgPerKg >= RegenerationEntryLoadingKgPerKg)
        {
            throw new ArgumentException(
                "Regeneration exit loading must be below regeneration entry loading (hysteresis).");
        }

        if (CriticalBatterySocFraction >= ReserveBatterySocFraction)
        {
            throw new ArgumentException("Critical SOC must be below reserve SOC.");
        }

        if (MaximumRecirculationFraction is < 0.0 or > 1.0
            || NominalFanControlFraction is < 0.0 or > 1.0
            || ReservePeltierDerateFraction is < 0.0 or > 1.0
            || DefaultRecirculationFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException("Fraction parameters must be in [0, 1].");
        }

        if (DefaultRecirculationFraction > MaximumRecirculationFraction)
        {
            throw new ArgumentException("Default recirculation exceeds maximum recirculation.");
        }

        if (FaultLatchThreshold < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(FaultLatchThreshold));
        }

        return this;
    }
}
