using ThermoCore.Core.Components;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;
using Xunit;

namespace ThermoCore.Core.Tests;

/// <summary>
/// Integration-test template using an acyclic moist-air graph.
/// </summary>
public sealed class IntegrationTestTemplate
{
    [Fact]
    public void Example_graph_runs_successfully()
    {
        var calculator = new PsychrometricCalculator();
        var ambient = calculator.CreateFromRelativeHumidity(
            UnitConversions.CelsiusToKelvin(25.0),
            PhysicalConstants.StandardAtmosphericPressurePa,
            relativeHumidityFraction: 0.50,
            dryAirMassFlowKgPerSecond: 0.02);

        var graph = new SimulationGraph(
            [
                new AmbientAirSourceComponent("source", ambient),
                new ExhaustAirSinkComponent("sink")
            ],
            [
                new PhysicalConnection
                {
                    Id = "source.outlet->sink.inlet",
                    SourceComponentId = "source",
                    SourcePortId = "outlet",
                    TargetComponentId = "sink",
                    TargetPortId = "inlet"
                }
            ]);

        var result = new AcyclicSimulationEngine().Run(new SimulationRequest
        {
            Graph = graph,
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(1),
            TimeStep = TimeSpan.FromSeconds(1)
        });

        Assert.True(result.Succeeded);
    }
}
