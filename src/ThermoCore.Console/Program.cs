using ThermoCore.Core.Components;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;

Console.WriteLine("ThermoCore Console");

var calculator = new PsychrometricCalculator();
var ambient = calculator.CreateFromRelativeHumidity(
    UnitConversions.CelsiusToKelvin(25.0),
    PhysicalConstants.StandardAtmosphericPressurePa,
    relativeHumidityFraction: 0.50,
    dryAirMassFlowKgPerSecond: 0.02);

var graph = new SimulationGraph(
    [
        new AmbientAirSourceComponent("source", ambient),
        new SensibleHeaterComponent("heater", heatRateW: 200.0, calculator),
        new ExhaustAirSinkComponent("sink")
    ],
    [
        new PhysicalConnection
        {
            Id = "s_h",
            SourceComponentId = "source",
            SourcePortId = "outlet",
            TargetComponentId = "heater",
            TargetPortId = "inlet"
        },
        new PhysicalConnection
        {
            Id = "h_k",
            SourceComponentId = "heater",
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

var outlet = (MoistAirState)result.Steps[0].PortStates["heater.outlet"]!;
Console.WriteLine($"Engine succeeded: {result.Succeeded}");
Console.WriteLine(
    $"Heater outlet: T={UnitConversions.KelvinToCelsius(outlet.TemperatureK):F2} °C, " +
    $"W={outlet.HumidityRatioKgPerKgDryAir:F6} kg/kg, " +
    $"h={outlet.SpecificEnthalpyJPerKgDryAir:F0} J/kg");
