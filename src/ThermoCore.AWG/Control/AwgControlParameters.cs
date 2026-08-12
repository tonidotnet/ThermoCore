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

    /// <summary>Lower clamp when actively cooling toward a warm surface (W).</summary>
    public double MinimumPeltierPowerRequestW { get; init; }

    /// <summary>Upper power clamp (W). Null ⇒ <see cref="NominalPeltierPowerRequestW"/>.</summary>
    public double? MaximumPeltierPowerRequestW { get; init; }

    /// <summary>
    /// Optional current limit. Combined with <see cref="TecOperatingVoltageV"/> as an electrical power cap.
    /// </summary>
    public double? MaximumPeltierCurrentA { get; init; }

    /// <summary>Operating voltage used with <see cref="MaximumPeltierCurrentA"/> (V).</summary>
    public double? TecOperatingVoltageV { get; init; }

    /// <summary>
    /// Maximum |ΔP|/Δt for anti-chatter. Non-finite ⇒ slew limiting disabled (default).
    /// </summary>
    public double PeltierPowerRampLimitWPerSecond { get; init; } = double.PositiveInfinity;

    /// <summary>Reduced hold fraction of nominal when surface is already at/below target.</summary>
    public double HoldPowerFractionWhenAtOrBelowTarget { get; init; } = 0.35;

    /// <summary>Hard floor for commanded condenser surface temperature (K).</summary>
    public double MinimumCondenserSurfaceTemperatureK { get; init; } = 255.0;

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
        FiniteNumber.RequireNonNegative(MinimumPeltierPowerRequestW, nameof(MinimumPeltierPowerRequestW));
        FiniteNumber.RequirePositive(MinimumCondenserSurfaceTemperatureK, nameof(MinimumCondenserSurfaceTemperatureK));
        FiniteNumber.Require(HoldPowerFractionWhenAtOrBelowTarget, nameof(HoldPowerFractionWhenAtOrBelowTarget));
        if (HoldPowerFractionWhenAtOrBelowTarget is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(HoldPowerFractionWhenAtOrBelowTarget),
                "Hold fraction must be in [0, 1].");
        }

        if (MaximumPeltierPowerRequestW is { } maxPower)
        {
            FiniteNumber.RequirePositive(maxPower, nameof(MaximumPeltierPowerRequestW));
            if (MinimumPeltierPowerRequestW > maxPower)
            {
                throw new ArgumentException("Minimum Peltier power exceeds maximum Peltier power.");
            }
        }
        else if (MinimumPeltierPowerRequestW > NominalPeltierPowerRequestW)
        {
            throw new ArgumentException("Minimum Peltier power exceeds nominal Peltier power.");
        }

        if (MaximumPeltierCurrentA is { } imax)
        {
            FiniteNumber.RequirePositive(imax, nameof(MaximumPeltierCurrentA));
        }

        if (TecOperatingVoltageV is { } voltage)
        {
            FiniteNumber.RequirePositive(voltage, nameof(TecOperatingVoltageV));
        }

        if (MaximumPeltierCurrentA is not null && TecOperatingVoltageV is null
            || MaximumPeltierCurrentA is null && TecOperatingVoltageV is not null)
        {
            throw new ArgumentException(
                "MaximumPeltierCurrentA and TecOperatingVoltageV must be set together.");
        }

        if (double.IsFinite(PeltierPowerRampLimitWPerSecond))
        {
            FiniteNumber.RequirePositive(PeltierPowerRampLimitWPerSecond, nameof(PeltierPowerRampLimitWPerSecond));
        }
        else if (double.IsNaN(PeltierPowerRampLimitWPerSecond) || double.IsNegativeInfinity(PeltierPowerRampLimitWPerSecond))
        {
            throw new ArgumentOutOfRangeException(
                nameof(PeltierPowerRampLimitWPerSecond),
                "Ramp limit must be positive finite or +∞.");
        }

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
