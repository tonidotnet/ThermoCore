using ThermoCore.Core.Components;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

public class AcyclicEngineAndComponentTests
{
    private readonly PsychrometricCalculator _calculator = new();

    [Fact]
    public void GraphTopology_OrdersDependentsAfterSources_Deterministically()
    {
        var a = new AmbientAirSourceComponent("A", SampleAir(25, 0.5, 0.01));
        var b = new MoistAirPassThroughComponent("B");
        var c = new ExhaustAirSinkComponent("C");

        // Register in reverse dependency order.
        var graph = new SimulationGraph(
            [c, b, a],
            [
                Connect("A_B", "A", "outlet", "B", "inlet"),
                Connect("B_C", "B", "outlet", "C", "inlet")
            ]);

        var order = GraphTopology.OrderComponentIds(graph);
        Assert.Equal(["A", "B", "C"], order);
    }

    [Fact]
    public void GraphTopology_Cycle_Throws()
    {
        var a = new MoistAirPassThroughComponent("A");
        var b = new MoistAirPassThroughComponent("B");
        var graph = new SimulationGraph(
            [a, b],
            [
                Connect("A_B", "A", "outlet", "B", "inlet"),
                Connect("B_A", "B", "outlet", "A", "inlet")
            ]);

        Assert.Throws<SimulationGraphException>(() => GraphTopology.OrderComponentIds(graph));
    }

    [Fact]
    public void AcyclicEngine_SourceHeaterSink_CommitsAndHeats()
    {
        var inlet = SampleAir(temperatureC: 25.0, rh: 0.50, flow: 0.02);
        var source = new AmbientAirSourceComponent("source", inlet);
        var heater = new SensibleHeaterComponent("heater", heatRateW: 200.0, _calculator);
        var sink = new ExhaustAirSinkComponent("sink");

        var graph = new SimulationGraph(
            [source, heater, sink],
            [
                Connect("s_h", "source", "outlet", "heater", "inlet"),
                Connect("h_k", "heater", "outlet", "sink", "inlet")
            ]);

        var engine = new AcyclicSimulationEngine();
        var result = engine.Run(new SimulationRequest
        {
            Graph = graph,
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(2),
            TimeStep = TimeSpan.FromSeconds(1)
        });

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(2, result.Steps.Count);
        Assert.All(result.Steps, s => Assert.True(s.Committed));

        var outlet = Assert.IsType<MoistAirState>(result.Steps[^1].PortStates["heater.outlet"]);
        Assert.Equal(inlet.HumidityRatioKgPerKgDryAir, outlet.HumidityRatioKgPerKgDryAir, precision: 12);
        Assert.True(outlet.TemperatureK > inlet.TemperatureK);
        Assert.True(outlet.SpecificEnthalpyJPerKgDryAir > inlet.SpecificEnthalpyJPerKgDryAir);
        Assert.Equal(inlet.DewPointTemperatureK, outlet.DewPointTemperatureK, precision: 4);
    }

    [Fact]
    public void Mixer_ConservesMassAndEnergy()
    {
        var a = SampleAir(20, 0.40, 0.01);
        var b = SampleAir(30, 0.60, 0.03);
        var sourceA = new AmbientAirSourceComponent("A", a);
        var sourceB = new AmbientAirSourceComponent("B", b);
        var mixer = new MoistAirMixerComponent("mixer", ["inlet_a", "inlet_b"], _calculator);
        var sink = new ExhaustAirSinkComponent("sink");

        var graph = new SimulationGraph(
            [sourceA, sourceB, mixer, sink],
            [
                Connect("A_m", "A", "outlet", "mixer", "inlet_a"),
                Connect("B_m", "B", "outlet", "mixer", "inlet_b"),
                Connect("m_s", "mixer", "outlet", "sink", "inlet")
            ]);

        var result = new AcyclicSimulationEngine().Run(new SimulationRequest
        {
            Graph = graph,
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(1),
            TimeStep = TimeSpan.FromSeconds(1)
        });

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        var mixed = Assert.IsType<MoistAirState>(result.Steps[0].PortStates["mixer.outlet"]);
        Assert.Equal(0.04, mixed.DryAirMassFlowKgPerSecond, precision: 12);
        Assert.Equal(
            (a.DryAirMassFlowKgPerSecond * a.HumidityRatioKgPerKgDryAir
             + b.DryAirMassFlowKgPerSecond * b.HumidityRatioKgPerKgDryAir) / 0.04,
            mixed.HumidityRatioKgPerKgDryAir,
            precision: 8);
    }

    [Fact]
    public void Splitter_PreservesStateAndSplitsFlow()
    {
        var inlet = SampleAir(25, 0.5, 0.02);
        var source = new AmbientAirSourceComponent("source", inlet);
        var splitter = new MoistAirSplitterComponent("split", [0.25, 0.75], _calculator);
        var sink0 = new ExhaustAirSinkComponent("sink0");
        var sink1 = new ExhaustAirSinkComponent("sink1");

        var graph = new SimulationGraph(
            [source, splitter, sink0, sink1],
            [
                Connect("s_sp", "source", "outlet", "split", "inlet"),
                Connect("sp_0", "split", "outlet_0", "sink0", "inlet"),
                Connect("sp_1", "split", "outlet_1", "sink1", "inlet")
            ]);

        var result = new AcyclicSimulationEngine().Run(new SimulationRequest
        {
            Graph = graph,
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(1),
            TimeStep = TimeSpan.FromSeconds(1)
        });

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        var o0 = Assert.IsType<MoistAirState>(result.Steps[0].PortStates["split.outlet_0"]);
        var o1 = Assert.IsType<MoistAirState>(result.Steps[0].PortStates["split.outlet_1"]);
        Assert.Equal(0.005, o0.DryAirMassFlowKgPerSecond, precision: 12);
        Assert.Equal(0.015, o1.DryAirMassFlowKgPerSecond, precision: 12);
        Assert.Equal(inlet.HumidityRatioKgPerKgDryAir, o0.HumidityRatioKgPerKgDryAir, precision: 12);
        Assert.Equal(inlet.TemperatureK, o1.TemperatureK, precision: 8);
    }

    [Fact]
    public void AcyclicEngine_MissingInlet_DoesNotCommit()
    {
        var heater = new SensibleHeaterComponent("heater", 100.0);
        var sink = new ExhaustAirSinkComponent("sink");
        // No source connected to heater inlet — graph validation fails on required port.
        var graph = new SimulationGraph(
            [heater, sink],
            [Connect("h_s", "heater", "outlet", "sink", "inlet")]);

        var result = new AcyclicSimulationEngine().Run(new SimulationRequest
        {
            Graph = graph,
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(1),
            TimeStep = TimeSpan.FromSeconds(1)
        });

        Assert.False(result.Succeeded);
        Assert.Empty(result.Steps);
        Assert.Contains(result.Diagnostics, d => d.Code == "GRAPH.REQUIRED_PORT_UNCONNECTED");
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
}
