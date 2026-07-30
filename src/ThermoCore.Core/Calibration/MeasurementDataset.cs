namespace ThermoCore.Core.Calibration;

/// <summary>Imported measurement package used for simulation-to-measurement comparison.</summary>
public sealed record MeasurementDataset
{
    public required string SourcePath { get; init; }

    public required IReadOnlyList<MeasurementSample> Samples { get; init; }

    public IReadOnlyList<string> ChannelIds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
