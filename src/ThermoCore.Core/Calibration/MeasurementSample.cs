namespace ThermoCore.Core.Calibration;

/// <summary>Single measured value at a UTC timestamp for a model channel id.</summary>
public sealed record MeasurementSample
{
    public required DateTimeOffset TimestampUtc { get; init; }

    public required string ChannelId { get; init; }

    public required double Value { get; init; }

    public required string Unit { get; init; }
}
