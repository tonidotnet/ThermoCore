using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Components;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.Tests;

public class AwgCoolingMetricsTests
{
    [Fact]
    public void BareCoolingDeviceCop_MatchesConstantCopTecBaseline()
    {
        // TEC baseline from ConstantCopPeltier: COP=1.2, Pe=50 → Qc=60.
        Assert.Equal(1.2, AwgCoolingMetricsCalculator.BareCoolingDeviceCop(60.0, 50.0));
        Assert.Equal(1.2, AwgCoolingMetricsCalculator.BareCoolingDeviceCop(60.0 * 10.0, 50.0 * 10.0));
    }

    [Fact]
    public void BareCoolingDeviceCop_ReturnsNullWhenElectricalMissingOrZero()
    {
        Assert.Null(AwgCoolingMetricsCalculator.BareCoolingDeviceCop(60.0, 0.0));
        Assert.Null(AwgCoolingMetricsCalculator.BareCoolingDeviceCop(60.0, null));
        Assert.Null(AwgCoolingMetricsCalculator.BareCoolingDeviceCop(null, 50.0));
    }

    [Fact]
    public void ConstantCopPeltier_ExposesConfiguredCop_AndSatisfiesQcOverPe()
    {
        var peltier = new ConstantCopPeltierComponent(
            id: "tec",
            coolingCop: 1.2,
            electricalPowerW: 50.0,
            coldSideTemperatureK: UnitConversions.CelsiusToKelvin(10.0),
            hotSideTemperatureK: UnitConversions.CelsiusToKelvin(40.0));

        var context = new ComponentStepContext
        {
            Simulation = new SimulationContext
            {
                SimulationStart = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                TimeStep = TimeSpan.FromSeconds(1),
                ElapsedTime = TimeSpan.Zero,
                StepIndex = 0
            }
        };
        peltier.Initialize(context.Simulation);
        var step = peltier.Evaluate(context);
        peltier.Commit(step);

        Assert.DoesNotContain(step.Diagnostics, d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error);
        Assert.Equal(1.2, peltier.CoolingCop);
        Assert.Equal(60.0, peltier.LastColdSideHeatW, precision: 10);
        Assert.Equal(50.0, peltier.LastElectricalPowerW, precision: 10);
        Assert.Equal(
            1.2,
            AwgCoolingMetricsCalculator.BareCoolingDeviceCop(
                peltier.LastColdSideHeatW,
                peltier.LastElectricalPowerW));
    }

    [Fact]
    public void AwgRun_ReportsProxyDeviceCopNearOne_AndPlantChannels()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: true);
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
        var options = AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1));
        var run = new AwgSimulationRunner().Run(configuration, initial, options);

        Assert.True(run.EngineResult.Succeeded);
        Assert.NotNull(run.Summary.CoolingPlantThermalInputJ);
        Assert.NotNull(run.Summary.CoolingPlantElectricalEnergyJ);
        Assert.True(run.Summary.CoolingPlantElectricalEnergyJ > 0.0);

        // ControllableHeatSource path: Pe proxy = Qc request → bare COP ≈ 1.
        Assert.NotNull(run.Summary.BareCoolingDeviceCOP);
        Assert.Equal(1.0, run.Summary.BareCoolingDeviceCOP!.Value, precision: 6);

        Assert.NotNull(run.Summary.CoolingPlantCOP);
        // Plant COP includes fan → must be ≤ bare device COP when fan > 0.
        Assert.True(run.Summary.CoolingPlantCOP <= run.Summary.BareCoolingDeviceCOP + 1e-9);

        Assert.NotNull(run.Summary.AverageTemperatureLiftK);
        Assert.NotNull(run.Summary.AverageDewPointMarginK);

        var collected = AwgResultExporter.Collect(run);
        Assert.True(collected.Summary.ScalarMetrics.ContainsKey("kpi.bareCoolingDeviceCOP"));
        Assert.True(collected.Summary.ScalarMetrics.ContainsKey("kpi.coolingPlantCOP"));
        Assert.True(collected.Summary.ScalarMetrics.ContainsKey("energy.coolingPlant.electricalJ"));
        Assert.True(collected.Summary.ScalarMetrics.ContainsKey("kpi.averageDewPointMarginK"));
    }
}
