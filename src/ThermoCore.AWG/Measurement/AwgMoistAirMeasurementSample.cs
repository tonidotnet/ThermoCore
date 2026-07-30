using ThermoCore.AWG.Topology;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.AWG.Measurement;

/// <summary>Sampled moist-air values at a measurement point.</summary>
public sealed record AwgMoistAirMeasurementSample
{
    public required string PointId { get; init; }

    public required string DisplayName { get; init; }

    public required double TemperatureK { get; init; }

    public required double PressurePa { get; init; }

    public required double HumidityRatioKgPerKgDryAir { get; init; }

    public required double RelativeHumidityFraction { get; init; }

    public required double DewPointTemperatureK { get; init; }

    public required double DryAirMassFlowKgPerSecond { get; init; }

    public required double WaterVaporMassFlowKgPerSecond { get; init; }

    public required double SpecificEnthalpyJPerKgDryAir { get; init; }
}
