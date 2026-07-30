using ThermoCore.Api.Contracts;
using ThermoCore.Web.Services;

namespace ThermoCore.Web.Tests;

public class SeriesChartSelectorTests
{
    [Fact]
    public void Select_PrefersWaterAndAmbientChannels()
    {
        var channels = new[]
        {
            Channel("fan.power", "Fan power", [1, 2, 3]),
            Channel("ambient.temperature", "Ambient temperature", [300, 301, 302]),
            Channel("waterTank.content", "Water tank content", [0.1, 0.2, 0.3]),
            Channel("noise.signal", "Noise", [0])
        };

        var selected = SeriesChartSelector.Select(channels, maxCharts: 2);

        Assert.Equal(2, selected.Count);
        Assert.Contains(selected, c => c.Id.Contains("waterTank", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(selected, c => c.Id.Contains("ambient", StringComparison.OrdinalIgnoreCase));
    }

    private static SimulationSeriesChannelDto Channel(string id, string name, double[] values)
        => new()
        {
            Id = id,
            DisplayName = name,
            Unit = "1",
            ComponentId = "c",
            Values = values
        };
}
