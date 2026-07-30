using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.Core.Results;

public sealed record SimulationRunMetadata
{
    public required string ResultFormatVersion { get; init; }

    public required DateTimeOffset StartTimeUtc { get; init; }

    public required TimeSpan Duration { get; init; }

    public required TimeSpan TimeStep { get; init; }

    public required int CapturedStepCount { get; init; }

    public required int TotalStepCount { get; init; }

    public required ResultCapturePolicy CapturePolicy { get; init; }
}
