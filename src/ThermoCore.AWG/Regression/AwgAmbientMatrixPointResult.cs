namespace ThermoCore.AWG.Regression;

/// <summary>One evaluated point in a full-AWG ambient temperature × humidity matrix.</summary>
public sealed record AwgAmbientMatrixPointResult
{
    public required string ScenarioId { get; init; }

    public required double AmbientTemperatureC { get; init; }

    public required double RelativeHumidityPercent { get; init; }

    public required bool Passed { get; init; }

    public required double CollectedWaterKg { get; init; }

    public required double LitersPerDay { get; init; }

    public double? FinalBusPowerW { get; init; }

    public double? FinalBatterySocFraction { get; init; }

    public string? FailureMessage { get; init; }
}
