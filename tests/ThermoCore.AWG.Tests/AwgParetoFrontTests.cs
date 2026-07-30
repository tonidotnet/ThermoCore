using ThermoCore.AWG.Optimization;

namespace ThermoCore.AWG.Tests;

public class AwgParetoFrontTests
{
    [Fact]
    public void LitersPerDayVsWattHoursPerLiter_KeepsNonDominatedPoints()
    {
        var points = new[]
        {
            Point(liters: 10, wh: 100), // dominated by better L and better Wh
            Point(liters: 12, wh: 90),  // front
            Point(liters: 8, wh: 70),   // front (lower energy)
            Point(liters: 11, wh: 95),  // dominated by 12/90
            Point(liters: 15, wh: null) // ignored (no Wh/L)
        };

        var front = AwgParetoFront.LitersPerDayVsWattHoursPerLiter(points);
        Assert.Equal(2, front.Count);
        Assert.Contains(front, p => p.LitersPerDay == 12 && p.WattHoursPerLiter == 90);
        Assert.Contains(front, p => p.LitersPerDay == 8 && p.WattHoursPerLiter == 70);
        Assert.Equal(12, front[0].LitersPerDay);
    }

    private static AwgParameterSweepPointResult Point(double liters, double? wh) =>
        new()
        {
            ParameterValues = new Dictionary<string, double>(),
            Succeeded = true,
            CollectedWaterKg = liters,
            LitersPerDay = liters,
            WattHoursPerLiter = wh,
            AggregatedEnergyResidualJ = 0,
            AggregatedWaterResidualKg = 0
        };
}
