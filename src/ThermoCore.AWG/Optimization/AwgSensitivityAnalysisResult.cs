namespace ThermoCore.AWG.Optimization;

/// <summary>OAT sensitivity report ranked by |liters/day elasticity|.</summary>
public sealed record AwgSensitivityAnalysisResult
{
    public required double BaselineLitersPerDay { get; init; }
    public required double BaselineCollectedWaterKg { get; init; }
    public required bool BaselineSucceeded { get; init; }
    public string? BaselineFailureMessage { get; init; }
    public required IReadOnlyList<AwgSensitivityParameterResult> Parameters { get; init; }

    public IReadOnlyList<AwgSensitivityParameterResult> RankedByElasticityMagnitude =>
        Parameters
            .Where(p => p.Succeeded && p.LitersPerDayElasticity is not null)
            .OrderByDescending(p => p.RankingMagnitude)
            .ToArray();
}
