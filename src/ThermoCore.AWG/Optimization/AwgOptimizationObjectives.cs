using ThermoCore.AWG.Simulation;

namespace ThermoCore.AWG.Optimization;

/// <summary>Scalar objective helpers for AWG optimization (OPT-004/005/008/009).</summary>
public static class AwgOptimizationObjectives
{
    /// <summary>Approximate liters/day from collected water mass, extrapolating the run duration to 24 h.</summary>
    public static double LitersPerDay(double collectedWaterKg, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        // Liquid water density ≈ 1 kg/L for MVP reporting.
        var liters = Math.Max(0.0, collectedWaterKg);
        return liters * (TimeSpan.FromDays(1).TotalSeconds / duration.TotalSeconds);
    }

    /// <summary>
    /// Approximate Wh/liter. Prefers integrated electrical energy (bus + Peltier proxy);
    /// falls back to final bus power × duration when energy was not integrated.
    /// Returns null when electrical energy or water production is unavailable.
    /// </summary>
    public static double? WattHoursPerLiter(AwgRunSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        var waterKg = summary.FinalWaterTankContentKg ?? 0.0;
        if (waterKg <= 0.0)
        {
            return null;
        }

        if (summary.WattHoursElectricPerLiter is { } integrated)
        {
            return integrated;
        }

        if (summary.FinalBusPowerW is not { } powerW || powerW <= 0.0)
        {
            return null;
        }

        var liters = waterKg;
        var wattHours = powerW * summary.Duration.TotalHours;
        return wattHours / liters;
    }

    /// <summary>L/kWh_electric (KPI-001). Null when electric energy ≤ 0.</summary>
    public static double? LitersPerKwhElectric(AwgRunSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return summary.LitersPerKwhElectric;
    }

    /// <summary>L/kWh_solar_primary (KPI-002). Null when incident solar ≤ 0.</summary>
    public static double? LitersPerKwhSolarPrimary(AwgRunSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return summary.LitersPerKwhSolarPrimary;
    }

    /// <summary>L/day/m² solar aperture (KPI-003).</summary>
    public static double? LitersPerDayPerSquareMeterAperture(AwgRunSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return summary.LitersPerDayPerSquareMeterAperture;
    }

    /// <summary>Collected / ambient moisture intake (KPI-004).</summary>
    public static double? WaterRecoveryFraction(AwgRunSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return summary.WaterRecoveryFraction;
    }

    /// <summary>Collected / desorbed bed water when desorption occurred.</summary>
    public static double? DesorptionCaptureFraction(AwgRunSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return summary.DesorptionCaptureFraction;
    }

    /// <summary>
    /// Solar utilization = useful collector energy / incident aperture solar energy.
    /// Prefer higher values. Null when solar ports were not observed.
    /// </summary>
    public static double? SolarUtilizationFraction(AwgRunSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return summary.SolarUtilizationFraction;
    }

    /// <summary>
    /// Battery cycling intensity as (charge + discharge throughput) / nominal capacity.
    /// Prefer lower values. Null when the electrical subsystem was not present.
    /// </summary>
    public static double? BatteryThroughputFraction(AwgRunSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return summary.BatteryThroughputFraction;
    }

    /// <summary>
    /// Observed SOC swing (max − min) during the run. Prefer lower values when minimizing cycling.
    /// </summary>
    public static double? BatterySocSwingFraction(AwgRunSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return summary.BatteryStateOfChargeSwingFraction;
    }
}
