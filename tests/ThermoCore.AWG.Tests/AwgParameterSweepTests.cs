using ThermoCore.AWG.Calibration;
using ThermoCore.AWG.Optimization;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;

namespace ThermoCore.AWG.Tests;

public class AwgParameterSweepTests
{
    [Fact]
    public void LitersPerDay_ExtrapolatesDuration()
    {
        var litersPerDay = AwgOptimizationObjectives.LitersPerDay(0.5, TimeSpan.FromHours(12));
        Assert.Equal(1.0, litersPerDay, precision: 12);
    }

    [Fact]
    public void Summary_ReportsSolarUtilization_AndBatteryMetricsWhenElectricalEnabled()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: true);
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
        var options = AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
        var run = new AwgSimulationRunner().Run(configuration, initial, options);

        Assert.True(run.EngineResult.Succeeded);
        Assert.NotNull(run.Summary.SolarUtilizationFraction);
        Assert.True(run.Summary.SolarUtilizationFraction is >= 0 and <= 1.5);
        Assert.NotNull(run.Summary.BatteryThroughputFraction);
        Assert.NotNull(run.Summary.BatteryStateOfChargeSwingFraction);
        Assert.Equal(
            run.Summary.SolarUtilizationFraction,
            AwgOptimizationObjectives.SolarUtilizationFraction(run.Summary));
        Assert.Equal(
            run.Summary.BatteryThroughputFraction,
            AwgOptimizationObjectives.BatteryThroughputFraction(run.Summary));
    }

    [Fact]
    public void Sweep_RunsGridAndRanksLitersPerDay()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false);
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
        var options = AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));

        var result = new AwgParameterSweepRunner().Run(
            configuration,
            initial,
            options,
            [
                new AwgParameterSweepAxis
                {
                    ParameterId = AwgCalibratableParameterIds.CondenserBypassFactor,
                    Values = [0.10, 0.20]
                }
            ]);

        Assert.Equal(2, result.Points.Count);
        Assert.All(result.Points, p => Assert.True(p.Succeeded, p.FailureMessage));
        Assert.NotNull(result.BestLitersPerDay);
    }
}
