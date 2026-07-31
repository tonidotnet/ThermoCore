namespace ThermoCore.Core.Calibration;

/// <summary>Result of a chronological train/holdout measurement split.</summary>
public sealed record MeasurementDatasetSplit
{
    public required MeasurementDataset Train { get; init; }

    public required MeasurementDataset Holdout { get; init; }

    public required double TrainFraction { get; init; }

    public required int TrainTimestampCount { get; init; }

    public required int HoldoutTimestampCount { get; init; }

    public required DateTimeOffset SplitAfterUtc { get; init; }
}
