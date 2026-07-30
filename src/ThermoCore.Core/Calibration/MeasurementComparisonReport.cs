namespace ThermoCore.Core.Calibration;

/// <summary>Aggregated simulation-to-measurement comparison report.</summary>
public sealed record MeasurementComparisonReport
{
    public required string MeasurementSourcePath { get; init; }

    public required IReadOnlyList<ChannelComparisonResult> Channels { get; init; }

    public required IReadOnlyList<string> MissingChannels { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }

    public double OverallRmse
        => Channels.Count == 0
            ? double.NaN
            : Math.Sqrt(Channels.Average(c => c.Metrics.Rmse * c.Metrics.Rmse));
}
