using ThermoCore.AWG.WeatherProfiles;
using ThermoCore.AWG.Sizing;

namespace ThermoCore.AWG.Tests;

public class AwgDiurnalSizingTests
{
    [Fact]
    public void SummerDiurnalWeather_HasAntiCorrelatedDayNight()
    {
        var start = DateTimeOffset.Parse("2026-07-15T00:00:00Z");
        var weather = SummerDiurnalWeatherFactory.CreateProvider(start);

        var night = weather.GetState(start.AddHours(2));
        var day = weather.GetState(start.AddHours(15));

        Assert.True(day.AmbientTemperatureK > night.AmbientTemperatureK);
        Assert.True(day.RelativeHumidityFraction < night.RelativeHumidityFraction);
        Assert.True(day.GlobalHorizontalIrradianceWPerM2 > 500.0);
        Assert.Equal(0.0, night.GlobalHorizontalIrradianceWPerM2);
        Assert.True(SummerDiurnalWeatherFactory.EstimatePeakSunHours() > 4.0);
    }

    [Fact]
    public void SummerDiurnalSizing_24hRun_SucceedsAndSizesTargets()
    {
        var report = new AwgDiurnalSizingRunner().Run(
            dayStartUtc: DateTimeOffset.Parse("2026-07-15T00:00:00Z"),
            timeStep: TimeSpan.FromSeconds(10));

        Assert.True(report.SimulationSucceeded, report.FailureMessage);
        Assert.Equal(24, report.HourlySamples.Count);
        Assert.Equal(4, report.Targets.Count);
        Assert.All(report.Targets, t => Assert.True(t.Feasible, t.Notes));
        Assert.True(report.BaselineWaterLiters >= 0.0);
        Assert.True(report.PeakSunHours > 4.0);
    }
}
