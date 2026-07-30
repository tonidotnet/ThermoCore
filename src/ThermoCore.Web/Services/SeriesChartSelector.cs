using ThermoCore.Api.Contracts;

namespace ThermoCore.Web.Services;

/// <summary>Picks a small set of preferred series channels for MVP charts.</summary>
public static class SeriesChartSelector
{
    private static readonly string[] PreferredIdFragments =
    [
        "waterTank",
        "liquidWater",
        "ambient",
        "condenser",
        "battery",
        "pv.",
        "peltier",
        "silica",
        "energyResidual",
        "waterResidual"
    ];

    public static IReadOnlyList<SimulationSeriesChannelDto> Select(
        IReadOnlyList<SimulationSeriesChannelDto> channels,
        int maxCharts = 6)
    {
        if (channels.Count == 0 || maxCharts <= 0)
        {
            return Array.Empty<SimulationSeriesChannelDto>();
        }

        var selected = new List<SimulationSeriesChannelDto>(maxCharts);
        foreach (var fragment in PreferredIdFragments)
        {
            foreach (var channel in channels)
            {
                if (channel.Values.Count < 2)
                {
                    continue;
                }

                if (selected.Any(s => s.Id == channel.Id))
                {
                    continue;
                }

                if (channel.Id.Contains(fragment, StringComparison.OrdinalIgnoreCase)
                    || channel.DisplayName.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                {
                    selected.Add(channel);
                    if (selected.Count >= maxCharts)
                    {
                        return selected;
                    }
                }
            }
        }

        foreach (var channel in channels.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            if (channel.Values.Count < 2 || selected.Any(s => s.Id == channel.Id))
            {
                continue;
            }

            selected.Add(channel);
            if (selected.Count >= maxCharts)
            {
                break;
            }
        }

        return selected;
    }
}
