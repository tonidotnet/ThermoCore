using ThermoCore.AWG.Measurement;
using ThermoCore.AWG.Regression;

namespace ThermoCore.AWG.Tests;

public class AwgFullFlowStationReportTests
{
    [Fact]
    public void FullFlowDemo_ReportsStationsAlongProcessTrain()
    {
        var scenario = AwgRegressionScenarioCatalog.CreateFullAwgFlowDemoScenario();
        var result = new AwgRegressionScenarioRunner().Run(scenario);
        Assert.True(result.Passed, string.Join("; ", result.Failures));

        var report = AwgFullFlowStationReportBuilder.Build(result.Run);
        Assert.True(report.HeatRecoveryEnabled);
        Assert.Contains(report.Stations, s => s.StationId == "T1");
        Assert.Contains(report.Stations, s => s.StationId == "T2");
        Assert.Contains(report.Stations, s => s.StationId == "T3");
        Assert.Contains(report.Stations, s => s.StationId == "T4");
        Assert.Contains(report.Stations, s => s.StationId == "T5");
        Assert.Contains(report.Stations, s => s.StationId == "TEX");
        Assert.All(report.Stations, s => Assert.True(s.DryAirMassFlowKgPerSecond > 0.0));
        Assert.Equal(0.02, result.Run.BuiltSystem.Configuration.Fan.DryAirMassFlowKgPerSecond, precision: 12);
    }
}
