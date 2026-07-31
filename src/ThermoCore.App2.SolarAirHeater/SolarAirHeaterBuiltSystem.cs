using ThermoCore.Core.Graph;

namespace ThermoCore.App2.SolarAirHeater;

/// <summary>Built solar-air-heater graph and its configuration fingerprint.</summary>
public sealed record SolarAirHeaterBuiltSystem
{
    public required SimulationGraph Graph { get; init; }

    public required SolarAirHeaterConfiguration Configuration { get; init; }

    public required string GraphFingerprint { get; init; }
}
