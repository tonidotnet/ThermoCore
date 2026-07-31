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
    /// Approximate Wh/liter using final bus power as a constant-power proxy over the run.
    /// Returns null when electrical power or water production is unavailable.
    /// </summary>
    public static double? WattHoursPerLiter(AwgRunSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        var waterKg = summary.FinalWaterTankContentKg ?? 0.0;
        if (waterKg <= 0.0 || summary.FinalBusPowerW is not { } powerW || powerW <= 0.0)
        {
            return null;
        }

        var liters = waterKg;
        var wattHours = powerW * summary.Duration.TotalHours;
        return wattHours / liters;
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
