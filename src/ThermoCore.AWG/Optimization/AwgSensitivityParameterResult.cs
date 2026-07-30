namespace ThermoCore.AWG.Optimization;

/// <summary>Local sensitivity metrics for a single calibratable parameter.</summary>
public sealed record AwgSensitivityParameterResult
{
    public required string ParameterId { get; init; }
    public required double BaselineValue { get; init; }
    public required double LowValue { get; init; }
    public required double HighValue { get; init; }
    public required double BaselineLitersPerDay { get; init; }
    public required double LowLitersPerDay { get; init; }
    public required double HighLitersPerDay { get; init; }
    public required bool Succeeded { get; init; }
    public string? FailureMessage { get; init; }

    /// <summary>
    /// Relative elasticity of liters/day vs parameter:
    /// ((y_high - y_low) / y_baseline) / ((x_high - x_low) / x_baseline).
    /// When baseline liters/day is ~0, falls back to (y_high - y_low) / ((x_high - x_low) / x_baseline).
    /// Null when the parameter span is unavailable.
    /// </summary>
    public double? LitersPerDayElasticity { get; init; }

    /// <summary>Absolute central difference dy/dx for liters/day.</summary>
    public double? LitersPerDayDerivative { get; init; }

    public double RankingMagnitude =>
        LitersPerDayElasticity is { } e ? Math.Abs(e) : 0.0;
}
