using ThermoCore.Core.Components;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

public class LoopCancellationAndAirflowTests
{
    private readonly PsychrometricCalculator _calculator = new();

    [Fact]
    public void GraphTopology_HasCycle_DetectsFeedback()
    {
        var a = new MoistAirPassThroughComponent("A");
        var b = new MoistAirPassThroughComponent("B");
        var graph = new SimulationGraph(
            [a, b],
            [
                Connect("A_B", "A", "outlet", "B", "inlet"),
                Connect("B_A", "B", "outlet", "A", "inlet")
            ]);

        Assert.True(GraphTopology.HasCycle(graph));
        Assert.Equal(["A", "B"], GraphTopology.GetCyclicComponentIds(graph));
    }

    [Fact]
    public void SimulationEngine_CycleWithoutLoopDefinition_Fails()
    {
        var a = new MoistAirPassThroughComponent("A");
        var b = new MoistAirPassThroughComponent("B");
        var graph = new SimulationGraph(
            [a, b],
            [
                Connect("A_B", "A", "outlet", "B", "inlet"),
                Connect("B_A", "B", "outlet", "A", "inlet")
            ]);

        var result = new SimulationEngine().Run(new SimulationRequest
        {
            Graph = graph,
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(1),
            TimeStep = TimeSpan.FromSeconds(1)
        });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == "ENGINE.CYCLE_DETECTED");
    }

    [Fact]
    public void SimulationEngine_RecirculationLoop_Converges()
    {
        const double flow = 0.02;
        var fresh = SampleAir(25, 0.40, flow * 0.5);
        var initialRecirc = SampleAir(30, 0.50, flow * 0.5);

        var source = new AmbientAirSourceComponent("fresh", fresh);
        var mixer = new MoistAirMixerComponent("mixer", ["fresh_in", "recirc_in"], _calculator);
        var heater = new SensibleHeaterComponent("heater", heatRateW: 150.0, _calculator);
        var splitter = new MoistAirSplitterComponent("split", [0.5, 0.5], _calculator);
        var sink = new ExhaustAirSinkComponent("exhaust");

        var graph = new SimulationGraph(
            [source, mixer, heater, splitter, sink],
            [
                Connect("fresh_mixer", "fresh", "outlet", "mixer", "fresh_in"),
                Connect("recirc_mixer", "split", "outlet_1", "mixer", "recirc_in"),
                Connect("mixer_heater", "mixer", "outlet", "heater", "inlet"),
                Connect("heater_split", "heater", "outlet", "split", "inlet"),
                Connect("split_exhaust", "split", "outlet_0", "exhaust", "inlet")
            ]);

        var result = new SimulationEngine().Run(new SimulationRequest
        {
            Graph = graph,
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(1),
            TimeStep = TimeSpan.FromSeconds(1),
            ExternalInputs = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["mixer.recirc_in"] = initialRecirc
            },
            Loops =
            [
                new SimulationLoopDefinition
                {
                    Id = "recirc",
                    TearConnectionId = "recirc_mixer",
                    RelaxationFactor = 0.7,
                    MaximumIterations = 50
                }
            ]
        });

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => $"{d.Code}:{d.Message}")));
        Assert.True(result.Steps[0].Committed);
        var heated = Assert.IsType<MoistAirState>(result.Steps[0].PortStates["heater.outlet"]);
        Assert.True(heated.TemperatureK > fresh.TemperatureK);
    }

    [Fact]
    public void SimulationEngine_Cancellation_ThrowsBeforeCommit()
    {
        var source = new AmbientAirSourceComponent("source", SampleAir(25, 0.5, 0.02));
        var sink = new ExhaustAirSinkComponent("sink");
        var graph = new SimulationGraph(
            [source, sink],
            [Connect("c", "source", "outlet", "sink", "inlet")]);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            new SimulationEngine().Run(
                new SimulationRequest
                {
                    Graph = graph,
                    StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                    Duration = TimeSpan.FromSeconds(1),
                    TimeStep = TimeSpan.FromSeconds(1)
                },
                cts.Token));
    }

    [Fact]
    public void Duct_AppliesReferenceCurvePressureDrop()
    {
        var inlet = SampleAir(25, 0.5, 0.02);
        var source = new AmbientAirSourceComponent("source", inlet);
        var duct = new DuctPressureLossComponent(
            "duct",
            pressureDropRefPa: 100.0,
            volumetricFlowRefM3PerSecond: inlet.DryAirMassFlowKgPerSecond * inlet.SpecificVolumeM3PerKgDryAir,
            exponent: 2.0,
            calculator: _calculator);
        var sink = new ExhaustAirSinkComponent("sink");

        var result = new SimulationEngine().Run(new SimulationRequest
        {
            Graph = new SimulationGraph(
                [source, duct, sink],
                [
                    Connect("s_d", "source", "outlet", "duct", "inlet"),
                    Connect("d_k", "duct", "outlet", "sink", "inlet")
                ]),
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(1),
            TimeStep = TimeSpan.FromSeconds(1)
        });

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        var outlet = Assert.IsType<MoistAirState>(result.Steps[0].PortStates["duct.outlet"]);
        Assert.Equal(100.0, duct.LastPressureDropPa, precision: 6);
        Assert.Equal(inlet.PressurePa - 100.0, outlet.PressurePa, precision: 3);
        Assert.Equal(inlet.HumidityRatioKgPerKgDryAir, outlet.HumidityRatioKgPerKgDryAir, precision: 10);
    }

    [Fact]
    public void PrescribedFlowFan_ReportsElectricalPowerAndPressureRise()
    {
        var inlet = SampleAir(25, 0.5, 0.02);
        var source = new AmbientAirSourceComponent("source", inlet);
        var fan = new PrescribedFlowFanComponent(
            "fan",
            dryAirMassFlowKgPerSecond: 0.02,
            pressureRisePa: 200.0,
            fanEfficiency: 0.6,
            driverEfficiency: 0.9,
            calculator: _calculator);
        var sink = new ExhaustAirSinkComponent("sink");

        var result = new SimulationEngine().Run(new SimulationRequest
        {
            Graph = new SimulationGraph(
                [source, fan, sink],
                [
                    Connect("s_f", "source", "outlet", "fan", "inlet"),
                    Connect("f_k", "fan", "outlet", "sink", "inlet")
                ]),
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(1),
            TimeStep = TimeSpan.FromSeconds(1)
        });

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        var outlet = Assert.IsType<MoistAirState>(result.Steps[0].PortStates["fan.outlet"]);
        Assert.Equal(inlet.PressurePa + 200.0, outlet.PressurePa, precision: 3);
        Assert.True(fan.LastElectricalPowerW > fan.LastAirPowerW);
        Assert.True(fan.LastAirPowerW > 0.0);
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
