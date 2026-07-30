using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Power;

/// <summary>
/// Battery parameters for SOC / charge-discharge bookkeeping
/// (docs/03_Components/12_BatteryAndPowerManagement.md §4).
/// </summary>
public sealed record BatteryParameters
{
    public required double NominalCapacityJ { get; init; }

    public required double MinimumSocFraction { get; init; }

    public required double MaximumSocFraction { get; init; }

    public required double ChargeEfficiencyFraction { get; init; }

    public required double DischargeEfficiencyFraction { get; init; }

    public required double MaximumChargePowerW { get; init; }

    public required double MaximumDischargePowerW { get; init; }

    public double SelfDischargePowerW { get; init; }

    public double InitialTemperatureK { get; init; } = 298.15;

    public BatteryParameters Validate()
    {
        FiniteNumber.RequirePositive(NominalCapacityJ, nameof(NominalCapacityJ));
        FiniteNumber.Require(MinimumSocFraction, nameof(MinimumSocFraction));
        FiniteNumber.Require(MaximumSocFraction, nameof(MaximumSocFraction));
        FiniteNumber.RequirePositive(ChargeEfficiencyFraction, nameof(ChargeEfficiencyFraction));
        FiniteNumber.RequirePositive(DischargeEfficiencyFraction, nameof(DischargeEfficiencyFraction));
        FiniteNumber.RequireNonNegative(MaximumChargePowerW, nameof(MaximumChargePowerW));
        FiniteNumber.RequireNonNegative(MaximumDischargePowerW, nameof(MaximumDischargePowerW));
        FiniteNumber.RequireNonNegative(SelfDischargePowerW, nameof(SelfDischargePowerW));
        FiniteNumber.RequirePositive(InitialTemperatureK, nameof(InitialTemperatureK));

        if (MinimumSocFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumSocFraction), "SOC fraction must be in [0, 1].");
        }

        if (MaximumSocFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumSocFraction), "SOC fraction must be in [0, 1].");
        }

        if (MinimumSocFraction > MaximumSocFraction)
        {
            throw new ArgumentException("Minimum SOC must not exceed maximum SOC.");
        }

        if (ChargeEfficiencyFraction > 1.0 || DischargeEfficiencyFraction > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                "Charge/discharge efficiency fractions must be in (0, 1].");
        }

        return this;
    }
}
