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
}
