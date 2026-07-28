using ThermoCore.Core.Balances;
using ThermoCore.Core.Components;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

public class PhysicalComponentTests
{
    private readonly PsychrometricCalculator _calculator = new();

    [Fact]
    public void ConstantCopPeltier_SatisfiesEnergyIdentity()
    {
        var peltier = new ConstantCopPeltierComponent(
            id: "tec",
            coolingCop: 1.2,
            electricalPowerW: 50.0,
            coldSideTemperatureK: UnitConversions.CelsiusToKelvin(10.0),
            hotSideTemperatureK: UnitConversions.CelsiusToKelvin(40.0));

        var result = EvaluateStandalone(peltier);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error);
        Assert.Equal(60.0, peltier.LastColdSideHeatW, precision: 10);
        Assert.Equal(110.0, peltier.LastHotSideHeatW, precision: 10);
        Assert.Equal(0.0, result.Balance.EnergyResidualJ, precision: 8);
        Assert.Equal(0.0, result.Balance.ElectricalEnergyResidualJ, precision: 8);
    }

    [Fact]
    public void SolarCollector_HeatsAirAtConstantHumidity()
    {
        var inlet = SampleAir(25, 0.5, 0.02);
        var source = new AmbientAirSourceComponent("air", inlet);
        var sun = new SolarRadiationSourceComponent("sun", irradianceWPerM2: 800.0);
        var collector = new ConstantEfficiencySolarCollectorComponent(
            "collector",
            efficiency: 0.5,
            apertureAreaM2: 1.0,
            calculator: _calculator);
        var sink = new ExhaustAirSinkComponent("sink");

        var result = new SimulationEngine().Run(new SimulationRequest
        {
            Graph = new SimulationGraph(
                [source, sun, collector, sink],
                [
                    Connect("a_c", "air", "outlet", "collector", "inlet"),
                    Connect("s_c", "sun", "outlet", "collector", "solar"),
                    Connect("c_k", "collector", "outlet", "sink", "inlet")
                ]),
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(1),
            TimeStep = TimeSpan.FromSeconds(1)
        });

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        var outlet = Assert.IsType<MoistAirState>(result.Steps[0].PortStates["collector.outlet"]);
        Assert.Equal(400.0, collector.LastUsefulHeatW, precision: 8);
        Assert.True(outlet.TemperatureK > inlet.TemperatureK);
        Assert.Equal(inlet.HumidityRatioKgPerKgDryAir, outlet.HumidityRatioKgPerKgDryAir, precision: 12);
    }

    [Fact]
    public void SolarPanel_ProducesExpectedElectricalPower()
    {
        var sun = new SolarRadiationSourceComponent("sun", 1000.0);
        var panel = new ConstantEfficiencySolarPanelComponent("pv", efficiency: 0.2, areaM2: 1.5);
        var load = new ElectricalLoadSinkComponent("load");

        var result = new SimulationEngine().Run(new SimulationRequest
        {
            Graph = new SimulationGraph(
                [sun, panel, load],
                [
                    Connect("s_p", "sun", "outlet", "pv", "solar"),
                    Connect("p_l", "pv", "electrical", "load", "inlet")
                ]),
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(1),
            TimeStep = TimeSpan.FromSeconds(1)
        });

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(300.0, panel.LastElectricalPowerW, precision: 8);
        var power = Assert.IsType<ElectricalPowerState>(result.Steps[0].PortStates["pv.electrical"]);
        Assert.Equal(300.0, power.PowerW, precision: 8);
    }

    [Fact]
    public void Condenser_ProducesWaterWhenSurfaceBelowDewPoint()
    {
        var inlet = SampleAir(30, 0.80, 0.02);
        var source = new AmbientAirSourceComponent("air", inlet);
        var condenser = new CondenserComponent(
            id: "cond",
            bypassFactor: 0.1,
            drainageEfficiency: 0.95,
            fallbackSurfaceTemperatureK: UnitConversions.CelsiusToKelvin(5.0),
            fallbackAvailableCoolingPowerW: 2000.0,
            calculator: _calculator);
        var sink = new ExhaustAirSinkComponent("sink");
        var drain = new LiquidWaterSinkComponent("drain");

        var result = new SimulationEngine().Run(new SimulationRequest
        {
            Graph = new SimulationGraph(
                [source, condenser, sink, drain],
                [
                    Connect("a_c", "air", "outlet", "cond", "inlet"),
                    Connect("c_s", "cond", "outlet", "sink", "inlet"),
                    Connect("c_d", "cond", "liquid_out", "drain", "inlet")
                ]),
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(1),
            TimeStep = TimeSpan.FromSeconds(1)
        });

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.True(condenser.LastCondensedWaterRateKgPerSecond > 0.0);
        Assert.True(condenser.LastCollectedWaterRateKgPerSecond > 0.0);
        Assert.True(condenser.LastCollectedWaterRateKgPerSecond
            <= condenser.LastCondensedWaterRateKgPerSecond + 1e-12);

        var outlet = Assert.IsType<MoistAirState>(result.Steps[0].PortStates["cond.outlet"]);
        Assert.True(outlet.HumidityRatioKgPerKgDryAir < inlet.HumidityRatioKgPerKgDryAir);
        Assert.True(outlet.TemperatureK < inlet.TemperatureK);
    }

    [Fact]
    public void Condenser_NoCondensationAboveDewPoint()
    {
        var inlet = SampleAir(20, 0.40, 0.02);
        var condenser = new CondenserComponent(
            id: "cond",
            bypassFactor: 0.2,
            drainageEfficiency: 1.0,
            fallbackSurfaceTemperatureK: UnitConversions.CelsiusToKelvin(25.0),
            fallbackAvailableCoolingPowerW: 500.0,
            calculator: _calculator);

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
                ["inlet"] = inlet
            }
        };

        condenser.Initialize(context.Simulation);
        var result = condenser.Evaluate(context);
        Assert.Equal(0.0, condenser.LastCondensedWaterRateKgPerSecond, precision: 12);
        Assert.Contains(result.Diagnostics, d => d.Code == "CONDENSER.NO_CONDENSATION");
        var outlet = Assert.IsType<MoistAirState>(result.OutputStates["outlet"]);
        Assert.Equal(inlet.HumidityRatioKgPerKgDryAir, outlet.HumidityRatioKgPerKgDryAir, precision: 12);
    }

    [Fact]
    public void Condenser_PowerLimit_ReducesCondensation()
    {
        var inlet = SampleAir(35, 0.90, 0.03);
        var unlimited = EvaluateCondenser(inlet, availableCoolingW: 5000.0);
        var limited = EvaluateCondenser(inlet, availableCoolingW: 50.0);

        Assert.True(limited.LastCondensedWaterRateKgPerSecond < unlimited.LastCondensedWaterRateKgPerSecond);
        Assert.Contains(limited.EvaluateDiagnostics, d => d.Code == "CONDENSER.COOLING_POWER_LIMITED");
    }

    private (double LastCondensedWaterRateKgPerSecond, IReadOnlyList<ThermoCore.Core.Diagnostics.SimulationDiagnostic> EvaluateDiagnostics)
        EvaluateCondenser(MoistAirState inlet, double availableCoolingW)
    {
        var condenser = new CondenserComponent(
            "cond",
            bypassFactor: 0.05,
            drainageEfficiency: 1.0,
            fallbackSurfaceTemperatureK: UnitConversions.CelsiusToKelvin(5.0),
            fallbackAvailableCoolingPowerW: availableCoolingW,
            calculator: _calculator);

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
                ["inlet"] = inlet
            }
        };

        condenser.Initialize(context.Simulation);
        var result = condenser.Evaluate(context);
        return (condenser.LastCondensedWaterRateKgPerSecond, result.Diagnostics);
    }

    private ComponentStepResult EvaluateStandalone(ISimulationComponent component)
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
        component.Initialize(context.Simulation);
        return component.Evaluate(context);
    }

    private MoistAirState SampleAir(double temperatureC, double rh, double flow)
        => _calculator.CreateFromRelativeHumidity(
            UnitConversions.CelsiusToKelvin(temperatureC),
            PhysicalConstants.StandardAtmosphericPressurePa,
            rh,
            flow);

    private static PhysicalConnection Connect(
        string id,
        string sourceComponent,
        string sourcePort,
        string targetComponent,
        string targetPort)
        => new()
        {
            Id = id,
            SourceComponentId = sourceComponent,
            SourcePortId = sourcePort,
            TargetComponentId = targetComponent,
            TargetPortId = targetPort
        };

    private sealed class ElectricalLoadSinkComponent : ISimulationComponent
    {
        public ElectricalLoadSinkComponent(string id)
        {
            Id = id;
            Ports = [new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.Electricity)];
        }

        public string Id { get; }

        public IReadOnlyList<IPhysicalPort> Ports { get; }

        public void Initialize(SimulationContext context)
        {
        }

        public ComponentStepResult Evaluate(ComponentStepContext context)
            => new() { Balance = ConservationBalance.Empty };

        public void Commit(ComponentStepResult result)
        {
        }

        public IReadOnlyList<ThermoCore.Core.Diagnostics.SimulationDiagnostic> GetDiagnostics()
            => Array.Empty<ThermoCore.Core.Diagnostics.SimulationDiagnostic>();
    }

    private sealed class LiquidWaterSinkComponent : ISimulationComponent
    {
        public LiquidWaterSinkComponent(string id)
        {
            Id = id;
            Ports = [new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.LiquidWater)];
        }

        public string Id { get; }

        public IReadOnlyList<IPhysicalPort> Ports { get; }

        public void Initialize(SimulationContext context)
        {
        }

        public ComponentStepResult Evaluate(ComponentStepContext context)
            => new() { Balance = ConservationBalance.Empty };

        public void Commit(ComponentStepResult result)
        {
        }

        public IReadOnlyList<ThermoCore.Core.Diagnostics.SimulationDiagnostic> GetDiagnostics()
            => Array.Empty<ThermoCore.Core.Diagnostics.SimulationDiagnostic>();
    }
}
