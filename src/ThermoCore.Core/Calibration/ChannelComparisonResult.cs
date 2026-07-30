namespace ThermoCore.Core.Calibration;

/// <summary>Per-channel alignment and error metrics for simulation-to-measurement comparison.</summary>
public sealed record ChannelComparisonResult
{
    public required string ChannelId { get; init; }

    public required string Unit { get; init; }

    public required ErrorMetrics Metrics { get; init; }

    public required int MatchedSampleCount { get; init; }

    public required int UnmatchedMeasurementCount { get; init; }
}
