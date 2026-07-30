using ThermoCore.AWG.Configuration;

namespace ThermoCore.Api.Contracts;

/// <summary>Creates an AWG simulation job from a console-compatible configuration document.</summary>
public sealed record CreateSimulationRequest
{
    public required AwgConfigurationDocument Configuration { get; init; }

    public double DurationSeconds { get; init; } = 60.0;

    public double TimeStepSeconds { get; init; } = 1.0;

    public DateTimeOffset? StartTimeUtc { get; init; }
}
