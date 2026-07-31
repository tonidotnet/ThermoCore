using ThermoCore.Core.Simulation;

namespace ThermoCore.App2.SolarAirHeater;

/// <summary>Result of a solar air heater MVP simulation.</summary>
public sealed record SolarAirHeaterRunResult
{
    public required SolarAirHeaterBuiltSystem BuiltSystem { get; init; }

    public required SimulationRunResult EngineResult { get; init; }

    public required double ExhaustTemperatureK { get; init; }

    public required double TemperatureRiseK { get; init; }

    public required double UsefulHeatW { get; init; }

    public required double IncidentSolarPowerW { get; init; }

    public double SolarUtilizationFraction
        => IncidentSolarPowerW > 1e-12 ? UsefulHeatW / IncidentSolarPowerW : 0.0;
}
