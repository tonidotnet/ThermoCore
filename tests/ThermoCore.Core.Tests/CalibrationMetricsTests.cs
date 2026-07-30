using ThermoCore.Core.Calibration;
using ThermoCore.Core.Results;

namespace ThermoCore.Core.Tests;

public class CalibrationMetricsTests
{
    [Fact]
    public void ErrorMetrics_Compute_RmseMaeBias()
    {
        var metrics = ErrorMetricsCalculator.Compute(
            measured: [1.0, 2.0, 3.0],
            simulated: [1.0, 3.0, 5.0]);

        Assert.Equal(Math.Sqrt((0 + 1 + 4) / 3.0), metrics.Rmse, precision: 12);
        Assert.Equal(1.0, metrics.Mae, precision: 12);
        Assert.Equal(1.0, metrics.Bias, precision: 12);
        Assert.Equal(3, metrics.SampleCount);
    }

    [Fact]
    public void MeasurementCsvImporter_ReadsLongFormat()
    {
        var csv =
            """
            timestamp_utc,channel_id,value,unit
            2026-01-01T00:00:00Z,ambient-source.outlet.temperature,298.15,K
            2026-01-01T00:00:01Z,ambient-source.outlet.temperature,299.15,K
            """;

        var dataset = MeasurementCsvImporter.Import(csv, "inline");
        Assert.Equal(2, dataset.Samples.Count);
        Assert.Equal(["ambient-source.outlet.temperature"], dataset.ChannelIds);
    }

    [Fact]
    public void SimulationMeasurementComparer_AlignsAndReportsNearZeroError()
    {
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var measurements = MeasurementCsvImporter.Import(
            """
            timestamp_utc,channel_id,value,unit
            2026-01-01T00:00:00Z,demo.temperature,300,K
            2026-01-01T00:00:01Z,demo.temperature,301,K
            2026-01-01T00:00:02Z,demo.temperature,302,K
            """,
            "inline");

        var channels = new[]
        {
            new ResultTimeSeriesChannel
            {
                Definition = new ResultChannelDefinition
                {
                    Id = "demo.temperature",
                    DisplayName = "Temperature",
                    QuantityType = "Temperature",
                    Unit = "K",
                    ComponentId = "demo"
                },
                Values = [300.0, 301.0, 302.0]
            }
        };

        var report = SimulationMeasurementComparer.Compare(
            measurements,
            channels,
            start,
            TimeSpan.FromSeconds(1));

        Assert.Single(report.Channels);
        Assert.Equal(0.0, report.Channels[0].Metrics.Rmse, precision: 12);
        Assert.Empty(report.MissingChannels);
    }
}
