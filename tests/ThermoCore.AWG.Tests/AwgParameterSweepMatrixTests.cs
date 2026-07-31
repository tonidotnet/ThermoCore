using ThermoCore.AWG.Regression;

namespace ThermoCore.AWG.Tests;

public class AwgParameterSweepMatrixTests
{
    [Fact]
    public void SilicaMassMatrix_At35C50Rh_HasFivePoints_AndAllPass()
    {
        var scenarios = AwgRegressionScenarioCatalog.CreateFullAwgFlowSilicaMassMatrixScenarios();
        Assert.Equal(5, scenarios.Count);
        Assert.All(scenarios, s =>
        {
            Assert.Equal(35.0, s.AmbientTemperatureC);
            Assert.Equal(0.50, s.RelativeHumidityFraction);
            Assert.True(s.EnableController);
            Assert.Equal(120.0, s.NominalPeltierPowerRequestW);
        });

        var report = new AwgSweepRunner().Run(
            title: "test silica",
            parameterName: "Silica mass",
            parameterUnit: "kg",
            boundarySummary: "35C/50%RH",
            consoleCommand: "full-flow-silica-matrix",
            scenarios: scenarios,
            parameterSelector: s => s.SilicaGelDryAdsorbentMassKg ?? 0.0);

        Assert.Equal(5, report.Points.Count);
        Assert.All(report.Points, p => Assert.True(p.Passed, $"{p.ScenarioId}: {p.FailureMessage}"));
        Assert.NotNull(report.BestLitersPerDay);
    }

    [Fact]
    public void PeltierPowerMatrix_At35C50Rh_HasFivePoints_AndAllPass()
    {
        var scenarios = AwgRegressionScenarioCatalog.CreateFullAwgFlowPeltierPowerMatrixScenarios();
        Assert.Equal(5, scenarios.Count);
        Assert.All(scenarios, s =>
        {
            Assert.Equal(35.0, s.AmbientTemperatureC);
            Assert.Equal(0.50, s.RelativeHumidityFraction);
            Assert.True(s.EnableController);
            Assert.Equal(2.0, s.SilicaGelDryAdsorbentMassKg);
        });

        var report = new AwgSweepRunner().Run(
            title: "test peltier",
            parameterName: "Peltier power",
            parameterUnit: "W",
            boundarySummary: "35C/50%RH",
            consoleCommand: "full-flow-peltier-matrix",
            scenarios: scenarios,
            parameterSelector: s => s.NominalPeltierPowerRequestW ?? 0.0);

        Assert.Equal(5, report.Points.Count);
        Assert.All(report.Points, p => Assert.True(p.Passed, $"{p.ScenarioId}: {p.FailureMessage}"));
        Assert.NotNull(report.BestLitersPerDay);
    }
}
