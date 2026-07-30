namespace ThermoCore.AWG.Optimization;

/// <summary>One evaluated point in a parameter sweep grid.</summary>
public sealed record AwgParameterSweepPointResult
{
    public required IReadOnlyDictionary<string, double> ParameterValues { get; init; }

    public required bool Succeeded { get; init; }

    public required double CollectedWaterKg { get; init; }

    public required double LitersPerDay { get; init; }

    public double? WattHoursPerLiter { get; init; }

    public required double AggregatedEnergyResidualJ { get; init; }

    public required double AggregatedWaterResidualKg { get; init; }

    public string? FailureMessage { get; init; }
}
