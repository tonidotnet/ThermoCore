using ThermoCore.Api.Services;

namespace ThermoCore.Api.Tests;

public class SeriesDownsampleTests
{
    [Fact]
    public void SliceAndDownsample_AppliesStrideAndKeepsLast()
    {
        var values = Enumerable.Range(0, 10).Select(i => (double)i).ToArray();
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var sliced = SimulationResultQueryService.SliceAndDownsample(
            values,
            start,
            nativeIntervalSeconds: 1,
            from: start,
            to: start.AddSeconds(9),
            intervalSeconds: 3);

        Assert.True(sliced.Count >= 3);
        Assert.Equal(9, sliced[^1]);
    }

    [Fact]
    public void SliceAndDownsample_EmptySeries_ReturnsEmpty()
    {
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var sliced = SimulationResultQueryService.SliceAndDownsample(
            Array.Empty<double>(),
            start,
            nativeIntervalSeconds: 1,
            from: start,
            to: start.AddSeconds(10),
            intervalSeconds: 2);
        Assert.Empty(sliced);
    }

    [Fact]
    public void SliceAndDownsample_IntervalLargerThanSpan_KeepsEndpoints()
    {
        var values = Enumerable.Range(0, 5).Select(i => (double)i).ToArray();
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var sliced = SimulationResultQueryService.SliceAndDownsample(
            values,
            start,
            nativeIntervalSeconds: 1,
            from: start,
            to: start.AddSeconds(4),
            intervalSeconds: 100);

        Assert.Equal(2, sliced.Count);
        Assert.Equal(0, sliced[0]);
        Assert.Equal(4, sliced[1]);
    }

    [Fact]
    public void SliceAndDownsample_FromAfterTo_ClampsToSinglePoint()
    {
        var values = Enumerable.Range(0, 10).Select(i => (double)i).ToArray();
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var sliced = SimulationResultQueryService.SliceAndDownsample(
            values,
            start,
            nativeIntervalSeconds: 1,
            from: start.AddSeconds(8),
            to: start.AddSeconds(2),
            intervalSeconds: null);

        Assert.Single(sliced);
        Assert.Equal(8, sliced[0]);
    }
}
