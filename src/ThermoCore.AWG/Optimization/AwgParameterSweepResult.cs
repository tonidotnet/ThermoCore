namespace ThermoCore.AWG.Optimization;

/// <summary>Full parameter-sweep report with best liters/day and Wh/liter points.</summary>
public sealed record AwgParameterSweepResult
{
    public required IReadOnlyList<AwgParameterSweepPointResult> Points { get; init; }

    public AwgParameterSweepPointResult? BestLitersPerDay
        => Points.Where(p => p.Succeeded).OrderByDescending(p => p.LitersPerDay).FirstOrDefault();

    public AwgParameterSweepPointResult? BestWattHoursPerLiter
        => Points
            .Where(p => p.Succeeded && p.WattHoursPerLiter is > 0)
            .OrderBy(p => p.WattHoursPerLiter!.Value)
            .FirstOrDefault();

    public AwgParameterSweepPointResult? BestSolarUtilization
        => Points
            .Where(p => p.Succeeded && p.SolarUtilizationFraction is > 0)
            .OrderByDescending(p => p.SolarUtilizationFraction!.Value)
            .FirstOrDefault();

    public AwgParameterSweepPointResult? BestBatteryThroughput
        => Points
            .Where(p => p.Succeeded && p.BatteryThroughputFraction is >= 0)
            .OrderBy(p => p.BatteryThroughputFraction!.Value)
            .FirstOrDefault();

    /// <summary>Bi-objective Pareto front (max L/day, min Wh/L) when electrical proxy exists.</summary>
    public IReadOnlyList<AwgParameterSweepPointResult> ParetoFrontLitersPerDayVsWattHoursPerLiter
        => AwgParetoFront.LitersPerDayVsWattHoursPerLiter(Points);
}
