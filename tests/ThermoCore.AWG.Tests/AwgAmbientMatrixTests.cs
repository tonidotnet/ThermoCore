using ThermoCore.AWG.Regression;

namespace ThermoCore.AWG.Tests;

public class AwgAmbientMatrixTests
{
    [Fact]
    public void AmbientMatrix_Has24Points_AndAllPass()
    {
        var scenarios = AwgRegressionScenarioCatalog.CreateFullAwgFlowAmbientMatrixScenarios();
        Assert.Equal(24, scenarios.Count);
        Assert.Contains(scenarios, s => s.AmbientTemperatureC == 20 && s.RelativeHumidityFraction == 0.30);
        Assert.Contains(scenarios, s => s.AmbientTemperatureC == 35 && s.RelativeHumidityFraction == 0.60);
        Assert.All(scenarios, s =>
        {
            Assert.False(s.EnableHeatRecovery);
            Assert.True(s.EnableElectricalSubsystem);
            Assert.True(s.EnableController);
            Assert.Equal(2.0, s.SilicaGelDryAdsorbentMassKg);
            Assert.Equal(0.02, s.InitialSilicaGelLoadingKgPerKg);
        });

        var report = new AwgAmbientMatrixRunner().Run(scenarios);
        Assert.Equal(24, report.Points.Count);
        Assert.All(report.Points, p => Assert.True(p.Passed, $"{p.ScenarioId}: {p.FailureMessage}"));
        Assert.NotNull(report.BestLitersPerDay);

        // Higher ambient RH at fixed T should not reduce harvested water once the controller cycles.
        var at35 = report.Points.Where(p => p.AmbientTemperatureC == 35).OrderBy(p => p.RelativeHumidityPercent).ToArray();
        Assert.True(at35.Length >= 2);
        Assert.True(
            at35[^1].CollectedWaterKg + 1e-9 >= at35[0].CollectedWaterKg,
            $"Expected RH60 water >= RH30 at 35 °C, got {at35[0].CollectedWaterKg} vs {at35[^1].CollectedWaterKg}");
    }
}
