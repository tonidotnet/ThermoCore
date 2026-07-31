namespace ThermoCore.AWG.Regression;

/// <summary>Report for a 1-D AWG parameter sweep.</summary>
public sealed record AwgSweepReport
{
    public required string Title { get; init; }

    public required string ParameterName { get; init; }

    public required string ParameterUnit { get; init; }

    public required string BoundarySummary { get; init; }

    public required string ConsoleCommand { get; init; }

    public required IReadOnlyList<AwgSweepPointResult> Points { get; init; }

    public AwgSweepPointResult? BestLitersPerDay
        => Points.Where(p => p.Passed).OrderByDescending(p => p.LitersPerDay).FirstOrDefault();
}
