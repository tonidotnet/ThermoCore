using ThermoCore.AWG.Calibration;
using ThermoCore.Core.Calibration;

namespace ThermoCore.AWG.Tests;

public class AwgSyntheticCampaignTests
{
    [Fact]
    public void GenerateCsv_ThreeRegimes_ImportableAndSplittable()
    {
        var csv = AwgSyntheticCampaignGenerator.GenerateCsv(
            AwgSyntheticCampaignGenerator.CreateDefaultThreeRegimeSegments());
        Assert.Contains(MeasurementCsvSchema.HeaderLine, csv);

        var dataset = MeasurementCsvImporter.Import(csv, "synthetic-campaign");
        Assert.True(dataset.Samples.Count >= 20);
        Assert.Contains("condenser.outlet.temperature", dataset.ChannelIds[0], StringComparison.Ordinal);

        var split = MeasurementDatasetSplitter.SplitChronologically(dataset, trainFraction: 0.7);
        Assert.True(split.TrainTimestampCount >= 1);
        Assert.True(split.HoldoutTimestampCount >= 1);
        Assert.True(split.SplitAfterUtc < dataset.Samples.Max(s => s.TimestampUtc));
    }
}
