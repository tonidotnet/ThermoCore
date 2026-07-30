using ThermoCore.Core.Components;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;
using Xunit;

namespace ThermoCore.Core.Tests;

public class WaterTankComponentTests
{
    [Fact]
    public void WaterTank_AccumulatesInletMass()
    {
        var tank = new WaterTankComponent(
            "tank",
            capacityKg: 10.0,
            initialStoredMassKg: 1.0,
            initialTemperatureK: UnitConversions.CelsiusToKelvin(20.0));

        var inlet = new LiquidWaterState
        {
            MassFlowKgPerSecond = 0.05,
            TemperatureK = UnitConversions.CelsiusToKelvin(10.0)
        };

        var context = new ComponentStepContext
        {
            Simulation = new SimulationContext
            {
                SimulationStart = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                TimeStep = TimeSpan.FromSeconds(2),
                ElapsedTime = TimeSpan.Zero,
                StepIndex = 0
            },
            InputStates = new Dictionary<string, object?>(StringComparer.Ordinal) { ["inlet"] = inlet }
        };

        tank.Initialize(context.Simulation);
        var result = tank.Evaluate(context);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity >= DiagnosticSeverity.Error);
        Assert.Equal(0.0, result.Balance.WaterMassResidualKg, precision: 12);
        tank.Commit(result);

        Assert.Equal(1.1, tank.StoredMassKg, precision: 10);
        Assert.False(tank.LastOverflowActive);
        Assert.Equal(0.11, tank.LevelFraction, precision: 10);
    }

    [Fact]
    public void WaterTank_OverflowsWhenCapacityExceeded()
    {
        var tank = new WaterTankComponent(
            "tank",
            capacityKg: 1.0,
            initialStoredMassKg: 0.95,
            initialTemperatureK: UnitConversions.CelsiusToKelvin(20.0));

        var inlet = new LiquidWaterState
        {
            MassFlowKgPerSecond = 0.1,
            TemperatureK = UnitConversions.CelsiusToKelvin(15.0)
        };

        var context = new ComponentStepContext
        {
            Simulation = new SimulationContext
            {
                SimulationStart = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                TimeStep = TimeSpan.FromSeconds(1),
                ElapsedTime = TimeSpan.Zero,
                StepIndex = 0
            },
            InputStates = new Dictionary<string, object?>(StringComparer.Ordinal) { ["inlet"] = inlet }
        };

        tank.Initialize(context.Simulation);
        var result = tank.Evaluate(context);
        tank.Commit(result);

        Assert.True(tank.LastOverflowActive);
        Assert.Equal(1.0, tank.StoredMassKg, precision: 10);
        Assert.True(tank.LastOverflowMassFlowKgPerSecond > 0.0);
        Assert.Contains(result.Diagnostics, d => d.Code == "TANK.OVERFLOW");
        var overflow = Assert.IsType<LiquidWaterState>(result.OutputStates["overflow"]);
        Assert.Equal(tank.LastOverflowMassFlowKgPerSecond, overflow.MassFlowKgPerSecond);
    }
}
