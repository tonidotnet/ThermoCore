using ThermoCore.Core.Calibration;

namespace ThermoCore.Core.Tests;

public class MeasurementDatasetSplitterTests
{
    [Fact]
    public void SplitChronologically_SeparatesEarlierAndLaterTimestamps()
    {
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var samples = new List<MeasurementSample>();
        for (var i = 0; i < 10; i++)
        {
            samples.Add(new MeasurementSample
            {
                TimestampUtc = start.AddSeconds(i),
                ChannelId = "ch",
                Value = i,
                Unit = "K"
            });
        }

        var dataset = new MeasurementDataset
        {
            SourcePath = "synthetic",
            Samples = samples,
            ChannelIds = ["ch"]
        };

        var split = MeasurementDatasetSplitter.SplitChronologically(dataset, trainFraction: 0.7);
        Assert.Equal(7, split.TrainTimestampCount);
        Assert.Equal(3, split.HoldoutTimestampCount);
        Assert.All(split.Train.Samples, s => Assert.True(s.TimestampUtc <= split.SplitAfterUtc));
        Assert.All(split.Holdout.Samples, s => Assert.True(s.TimestampUtc > split.SplitAfterUtc));
    }
}
