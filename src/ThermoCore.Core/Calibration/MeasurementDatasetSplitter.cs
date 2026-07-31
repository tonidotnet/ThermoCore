namespace ThermoCore.Core.Calibration;

/// <summary>
/// Chronological train/holdout split of a measurement dataset by unique sample timestamps.
/// </summary>
public static class MeasurementDatasetSplitter
{
    /// <summary>
    /// Splits samples into an earlier training window and a later holdout window.
    /// Fraction applies to distinct UTC timestamps (not individual channel rows).
    /// </summary>
    public static MeasurementDatasetSplit SplitChronologically(
        MeasurementDataset dataset,
        double trainFraction = 0.7)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        if (trainFraction is <= 0.0 or >= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(trainFraction), "Train fraction must be in (0, 1).");
        }

        var timestamps = dataset.Samples
            .Select(s => s.TimestampUtc)
            .Distinct()
            .OrderBy(t => t)
            .ToArray();

        if (timestamps.Length < 2)
        {
            throw new ArgumentException(
                "Holdout split requires at least two distinct measurement timestamps.",
                nameof(dataset));
        }

        var trainCount = Math.Max(1, (int)Math.Floor(timestamps.Length * trainFraction));
        if (trainCount >= timestamps.Length)
        {
            trainCount = timestamps.Length - 1;
        }

        var cut = timestamps[trainCount - 1];
        var trainSamples = dataset.Samples.Where(s => s.TimestampUtc <= cut).ToArray();
        var holdoutSamples = dataset.Samples.Where(s => s.TimestampUtc > cut).ToArray();

        if (holdoutSamples.Length == 0)
        {
            throw new ArgumentException(
                "Holdout split produced an empty holdout set; increase measurement span or lower train fraction.",
                nameof(dataset));
        }

        return new MeasurementDatasetSplit
        {
            Train = new MeasurementDataset
            {
                SourcePath = $"{dataset.SourcePath}#train",
                Samples = trainSamples,
                ChannelIds = dataset.ChannelIds,
                Warnings = dataset.Warnings
            },
            Holdout = new MeasurementDataset
            {
                SourcePath = $"{dataset.SourcePath}#holdout",
                Samples = holdoutSamples,
                ChannelIds = dataset.ChannelIds,
                Warnings = dataset.Warnings
            },
            TrainFraction = trainFraction,
            TrainTimestampCount = trainCount,
            HoldoutTimestampCount = timestamps.Length - trainCount,
            SplitAfterUtc = cut
        };
    }
}
