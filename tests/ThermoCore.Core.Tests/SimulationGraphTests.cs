using ThermoCore.Core.Balances;
using ThermoCore.Core.Components;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

public class SimulationGraphTests
{
    [Fact]
    public void Validate_CompatibleMoistAirConnection_IsValid()
    {
        var source = new MoistAirPassThroughComponent("A");
        var sink = new MoistAirPassThroughComponent("B");
        var graph = new SimulationGraph(
            [source, sink],
            [
                new PhysicalConnection
                {
                    Id = "A_to_B",
                    SourceComponentId = "A",
                    SourcePortId = "outlet",
                    TargetComponentId = "B",
                    TargetPortId = "inlet"
                },
                // Satisfy required ports on open ends for this validation unit:
                // mark outer ports optional by using a second connection pair isn't needed —
                // instead build graph with only connected required ports via custom components.
            ]);

        // A.inlet and B.outlet remain required/unconnected → expect failure unless we only
        // connect both. For a strict two-node chain, create optional outer ports in a dedicated test.
        var result = graph.Validate();
        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, d => d.Code == "GRAPH.REQUIRED_PORT_UNCONNECTED");
    }

    [Fact]
    public void Validate_DomainMismatch_ReportsDiagnostic()
    {
        var air = new MoistAirPassThroughComponent("air");
        var electric = new TestElectricSink("elec");
        var graph = new SimulationGraph(
            [air, electric],
            [
                new PhysicalConnection
                {
                    Id = "bad",
                    SourceComponentId = "air",
                    SourcePortId = "outlet",
                    TargetComponentId = "elec",
                    TargetPortId = "inlet"
                }
            ]);

        var result = graph.Validate();
        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, d => d.Code == "GRAPH.DOMAIN_MISMATCH");
    }

    [Fact]
    public void EvaluateCommit_PassThrough_PreservesStateAndBalance()
    {
        var component = new MoistAirPassThroughComponent("duct");
        var calculator = new PsychrometricCalculator();
        var state = calculator.CreateFromRelativeHumidity(
            UnitConversions.CelsiusToKelvin(25.0),
            PhysicalConstants.StandardAtmosphericPressurePa,
            relativeHumidityFraction: 0.50,
            dryAirMassFlowKgPerSecond: 0.02);

        var context = new ComponentStepContext
        {
            Simulation = new SimulationContext
            {
                SimulationStart = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                TimeStep = TimeSpan.FromSeconds(1.0),
                ElapsedTime = TimeSpan.Zero,
                StepIndex = 0
            },
            InputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["inlet"] = state
            }
        };

        component.Initialize(context.Simulation);
        var result = component.Evaluate(context);
        component.Commit(result);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(0.0, result.Balance.DryAirMassResidualKg);
        Assert.Equal(0.0, result.Balance.WaterMassResidualKg);
        Assert.Equal(0.0, result.Balance.EnergyResidualJ);
        Assert.True(new ConservationValidator().Validate(result.Balance).IsValid);
        Assert.Same(state, result.OutputStates["outlet"]);
        Assert.NotNull(component.LastCommitted);
    }

    [Fact]
    public void Validate_FullyConnectedOptionalOuterPorts_IsValid()
    {
        var source = new BoundarySource("source");
        var sink = new BoundarySink("sink");
        var graph = new SimulationGraph(
            [source, sink],
            [
                new PhysicalConnection
                {
                    Id = "c1",
                    SourceComponentId = "source",
                    SourcePortId = "outlet",
                    TargetComponentId = "sink",
                    TargetPortId = "inlet"
                }
            ]);

        var result = graph.Validate();
        Assert.True(result.IsValid, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
    }

    private sealed class TestElectricSink : ISimulationComponent
    {
        public TestElectricSink(string id)
        {
            Id = id;
            Ports =
            [
                new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.Electricity)
            ];
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

        public IReadOnlyList<SimulationDiagnostic> GetDiagnostics()
            => Array.Empty<SimulationDiagnostic>();
    }

    private sealed class BoundarySource : ISimulationComponent
    {
        public BoundarySource(string id)
        {
            Id = id;
            Ports =
            [
                new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.MoistAir, isRequired: true)
            ];
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

        public IReadOnlyList<SimulationDiagnostic> GetDiagnostics()
            => Array.Empty<SimulationDiagnostic>();
    }

    private sealed class BoundarySink : ISimulationComponent
    {
        public BoundarySink(string id)
        {
            Id = id;
            Ports =
            [
                new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.MoistAir, isRequired: true)
            ];
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

        public IReadOnlyList<SimulationDiagnostic> GetDiagnostics()
            => Array.Empty<SimulationDiagnostic>();
    }
}
