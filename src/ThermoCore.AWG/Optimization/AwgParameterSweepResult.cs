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
}
