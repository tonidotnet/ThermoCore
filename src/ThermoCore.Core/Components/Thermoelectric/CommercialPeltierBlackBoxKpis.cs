namespace ThermoCore.Core.Components.Thermoelectric;

/// <summary>
/// KPI helpers for commercial black-box vs analytical TEC comparison (common definitions).
/// </summary>
public static class CommercialPeltierBlackBoxKpis
{
    public const double JoulesPerKilowattHour = 3_600_000.0;

    /// <summary>L/kWh_electric from water rate and electrical power (ρ≈1 kg/L).</summary>
    public static double? LitersPerKwhElectric(double waterRateKgPerSecond, double electricalPowerW)
    {
        if (waterRateKgPerSecond < 0.0 || electricalPowerW <= 0.0)
        {
            return null;
        }

        var litersPerSecond = waterRateKgPerSecond;
        var kwhPerSecond = electricalPowerW / JoulesPerKilowattHour;
        return litersPerSecond / kwhPerSecond;
    }

    /// <summary>Bare device COP = delivered cooling / electrical power.</summary>
    public static double? BareCoolingDeviceCop(double deliveredCoolingPowerW, double electricalPowerW)
    {
        if (deliveredCoolingPowerW < 0.0 || electricalPowerW <= 0.0)
        {
            return null;
        }

        return deliveredCoolingPowerW / electricalPowerW;
    }

    /// <summary>Energy-integrated COP using the same null semantics as AWG cooling KPIs.</summary>
    public static double? BareCoolingDeviceCopFromEnergy(double coolingEnergyJ, double electricalEnergyJ)
    {
        if (coolingEnergyJ is not > 0.0 || electricalEnergyJ is not > 0.0)
        {
            return null;
        }

        return coolingEnergyJ / electricalEnergyJ;
    }
}
