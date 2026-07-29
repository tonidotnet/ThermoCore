using ThermoCore.Core.Components.Thermoelectric;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

public class AnalyticalPeltierTests
{
    private static AnalyticalPeltierParameters DefaultParameters()
        => AnalyticalPeltierParameters.CreateProvisionalEngineeringDefaults();

    [Fact]
    public void DisabledModule_LeavesOnlyPassiveConduction()
    {
        var peltier = Create(
            requestedElectricalPowerW: 0.0,
            coldC: 10.0,
            hotC: 40.0);

        var result = Evaluate(peltier);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error);
        Assert.Equal(0.0, peltier.LastCurrentA, precision: 12);
        Assert.Equal(0.0, peltier.LastElectricalPowerW, precision: 12);

        var deltaT = UnitConversions.CelsiusToKelvin(40.0) - UnitConversions.CelsiusToKelvin(10.0);
        var expected = -DefaultParameters().ThermalConductanceWPerK * deltaT;
        Assert.Equal(expected, peltier.LastColdSideHeatW, precision: 10);
        Assert.Equal(expected, peltier.LastHotSideHeatW, precision: 10);
        Assert.Contains(result.Diagnostics, d => d.Code == "PELTIER.OFF_STATE_CONDUCTION");
        Assert.Equal(0.0, result.Balance.EnergyResidualJ, precision: 8);
    }

    [Fact]
    public void ZeroTemperatureDifference_SatisfiesEnergyIdentity()
    {
        var parameters = DefaultParameters();
        var peltier = new AnalyticalPeltierComponent(
            id: "tec",
            parameters: parameters,
            coldSideTemperatureK: UnitConversions.CelsiusToKelvin(25.0),
            hotSideTemperatureK: UnitConversions.CelsiusToKelvin(25.0),
            requestedElectricalPowerW: 20.0);

        var result = Evaluate(peltier);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error);

        var current = peltier.LastCurrentA;
        var expectedQc = parameters.SeebeckCoefficientVPerK * current * UnitConversions.CelsiusToKelvin(25.0)
            - 0.5 * current * current * parameters.ElectricalResistanceOhm;
        Assert.Equal(expectedQc, peltier.LastColdSideHeatW, precision: 8);
        Assert.Equal(peltier.LastColdSideHeatW + peltier.LastElectricalPowerW, peltier.LastHotSideHeatW, precision: 8);
        Assert.Equal(20.0, peltier.LastElectricalPowerW, precision: 8);
        Assert.Equal(0.0, result.Balance.EnergyResidualJ, precision: 8);
    }

    [Fact]
    public void IncreasedHotSideTemperature_ReducesCooling()
    {
        var coolHot = Create(requestedElectricalPowerW: 25.0, coldC: 10.0, hotC: 30.0);
        var hotHot = Create(requestedElectricalPowerW: 25.0, coldC: 10.0, hotC: 55.0);

        Evaluate(coolHot);
        Evaluate(hotHot);

        Assert.True(hotHot.LastColdSideHeatW < coolHot.LastColdSideHeatW);
    }

    [Fact]
    public void PowerRequestSolver_ReproducesElectricalPower()
    {
        var parameters = DefaultParameters();
        const double requestedPowerW = 18.0;
        var coldK = UnitConversions.CelsiusToKelvin(12.0);
        var hotK = UnitConversions.CelsiusToKelvin(35.0);
        var deltaT = hotK - coldK;

        var solvedCurrent = AnalyticalPeltierComponent.SolveCurrentFromElectricalPower(
            parameters.SeebeckCoefficientVPerK,
            parameters.ElectricalResistanceOhm,
            deltaT,
            requestedPowerW);

        var voltage = parameters.SeebeckCoefficientVPerK * deltaT
            + solvedCurrent * parameters.ElectricalResistanceOhm;
        Assert.Equal(requestedPowerW, voltage * solvedCurrent, precision: 8);

        var peltier = new AnalyticalPeltierComponent(
            "tec",
            parameters,
            coldK,
            hotK,
            requestedElectricalPowerW: requestedPowerW);
        Evaluate(peltier);
        Assert.Equal(requestedPowerW, peltier.LastElectricalPowerW, precision: 8);
        Assert.Equal(solvedCurrent, peltier.LastCurrentA, precision: 8);
    }

    [Fact]
    public void CurrentLimit_IsEnforced()
    {
        var parameters = DefaultParameters() with { MaximumCurrentA = 1.0 };
        var peltier = new AnalyticalPeltierComponent(
            "tec",
            parameters,
            UnitConversions.CelsiusToKelvin(10.0),
            UnitConversions.CelsiusToKelvin(25.0),
            requestedElectricalPowerW: 50.0);

        var result = Evaluate(peltier);
        Assert.Equal(1.0, Math.Abs(peltier.LastCurrentA), precision: 10);
        Assert.Contains(result.Diagnostics, d => d.Code == "PELTIER.CURRENT_LIMIT");
    }

    [Fact]
    public void FixedCurrentMode_UsesGoverningEquations()
    {
        var parameters = DefaultParameters();
        const double currentA = 2.5;
        var coldK = UnitConversions.CelsiusToKelvin(15.0);
        var hotK = UnitConversions.CelsiusToKelvin(40.0);
        var deltaT = hotK - coldK;

        var peltier = new AnalyticalPeltierComponent(
            "tec",
            parameters,
            coldK,
            hotK,
            requestedCurrentA: currentA);

        var result = Evaluate(peltier);
        var expectedQc = parameters.SeebeckCoefficientVPerK * currentA * coldK
            - 0.5 * currentA * currentA * parameters.ElectricalResistanceOhm
            - parameters.ThermalConductanceWPerK * deltaT;
        var expectedPe = currentA
            * (parameters.SeebeckCoefficientVPerK * deltaT + currentA * parameters.ElectricalResistanceOhm);

        Assert.Equal(currentA, peltier.LastCurrentA, precision: 12);
        Assert.Equal(expectedQc, peltier.LastColdSideHeatW, precision: 8);
        Assert.Equal(expectedPe, peltier.LastElectricalPowerW, precision: 8);
        Assert.Equal(expectedQc + expectedPe, peltier.LastHotSideHeatW, precision: 8);
        Assert.Equal(0.0, result.Balance.EnergyResidualJ, precision: 8);
    }

    [Fact]
    public void BoundaryPorts_OverrideConfiguredTemperatures()
    {
        var parameters = DefaultParameters();
        var peltier = new AnalyticalPeltierComponent(
            "tec",
            parameters,
            coldSideTemperatureK: UnitConversions.CelsiusToKelvin(20.0),
            hotSideTemperatureK: UnitConversions.CelsiusToKelvin(20.0),
            requestedElectricalPowerW: 15.0);

        var context = new ComponentStepContext
        {
            Simulation = new SimulationContext
            {
                SimulationStart = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                TimeStep = TimeSpan.FromSeconds(1),
                ElapsedTime = TimeSpan.Zero,
                StepIndex = 0
            },
            InputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["cold_boundary"] = new HeatFlowState
                {
                    HeatFlowW = 0.0,
                    TemperatureK = UnitConversions.CelsiusToKelvin(5.0)
                },
                ["hot_boundary"] = new HeatFlowState
                {
                    HeatFlowW = 0.0,
                    TemperatureK = UnitConversions.CelsiusToKelvin(45.0)
                }
            }
        };

        peltier.Initialize(context.Simulation);
        var result = peltier.Evaluate(context);
        peltier.Commit(result);

        var cold = Assert.IsType<HeatFlowState>(result.OutputStates["cold_heat"]);
        var hot = Assert.IsType<HeatFlowState>(result.OutputStates["hot_heat"]);
        Assert.Equal(UnitConversions.CelsiusToKelvin(5.0), cold.TemperatureK, precision: 12);
        Assert.Equal(UnitConversions.CelsiusToKelvin(45.0), hot.TemperatureK, precision: 12);
    }

    [Fact]
    public void Deterministic_RepeatedEvaluationMatches()
    {
        var peltier = Create(22.0, 8.0, 38.0);
        var first = Evaluate(peltier);
        var second = Evaluate(peltier);

        Assert.Equal(first.Balance.EnergyResidualJ, second.Balance.EnergyResidualJ, precision: 12);
        Assert.Equal(peltier.LastColdSideHeatW, EvaluateAndReadCold(Create(22.0, 8.0, 38.0)), precision: 12);
    }

    private static AnalyticalPeltierComponent Create(double requestedElectricalPowerW, double coldC, double hotC)
        => new(
            "tec",
            DefaultParameters(),
            UnitConversions.CelsiusToKelvin(coldC),
            UnitConversions.CelsiusToKelvin(hotC),
            requestedElectricalPowerW: requestedElectricalPowerW);

    private static ComponentStepResult Evaluate(AnalyticalPeltierComponent peltier)
    {
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
        var result = peltier.Evaluate(context);
        peltier.Commit(result);
        return result;
    }

    private static double EvaluateAndReadCold(AnalyticalPeltierComponent peltier)
    {
        Evaluate(peltier);
        return peltier.LastColdSideHeatW;
    }
}
