using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Power;

/// <summary>
/// Battery stored-energy state (docs/03_Components/12_BatteryAndPowerManagement.md §3).
/// </summary>
public sealed record BatteryState
{
    public required double StoredEnergyJ { get; init; }

    public required double StateOfChargeFraction { get; init; }

    public required double BatteryTemperatureK { get; init; }

    public required double CumulativeChargeEnergyJ { get; init; }

    public required double CumulativeDischargeEnergyJ { get; init; }

    public static BatteryState Create(
        double storedEnergyJ,
        double nominalCapacityJ,
        double batteryTemperatureK,
        double cumulativeChargeEnergyJ = 0.0,
        double cumulativeDischargeEnergyJ = 0.0)
    {
        FiniteNumber.RequireNonNegative(storedEnergyJ, nameof(storedEnergyJ));
        FiniteNumber.RequirePositive(nominalCapacityJ, nameof(nominalCapacityJ));
        FiniteNumber.RequirePositive(batteryTemperatureK, nameof(batteryTemperatureK));
        FiniteNumber.RequireNonNegative(cumulativeChargeEnergyJ, nameof(cumulativeChargeEnergyJ));
        FiniteNumber.RequireNonNegative(cumulativeDischargeEnergyJ, nameof(cumulativeDischargeEnergyJ));

        if (storedEnergyJ > nominalCapacityJ)
        {
            throw new ArgumentOutOfRangeException(nameof(storedEnergyJ), "Stored energy exceeds nominal capacity.");
        }

        return new BatteryState
        {
            StoredEnergyJ = storedEnergyJ,
            StateOfChargeFraction = storedEnergyJ / nominalCapacityJ,
            BatteryTemperatureK = batteryTemperatureK,
            CumulativeChargeEnergyJ = cumulativeChargeEnergyJ,
            CumulativeDischargeEnergyJ = cumulativeDischargeEnergyJ
        };
    }
}
