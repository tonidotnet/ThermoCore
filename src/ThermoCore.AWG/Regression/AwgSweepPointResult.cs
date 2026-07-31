namespace ThermoCore.AWG.Regression;

/// <summary>One evaluated point in a 1-D parameter sweep.</summary>
public sealed record AwgSweepPointResult
{
    public required string ScenarioId { get; init; }

    public required string ParameterName { get; init; }

    public required double ParameterValue { get; init; }

    public required string ParameterUnit { get; init; }

    public required bool Passed { get; init; }

    public required double CollectedWaterKg { get; init; }

    public required double LitersPerDay { get; init; }

    public double? FinalBusPowerW { get; init; }

    public double? FinalBatterySocFraction { get; init; }

    public string? FailureMessage { get; init; }
}
