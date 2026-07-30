using ThermoCore.AWG.Topology;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.AWG.Simulation;

/// <summary>Result of an AWG-hosted simulation run.</summary>
public sealed record AwgSimulationRunResult
{
    public required AwgBuiltSystem BuiltSystem { get; init; }

    public required AwgSimulationOptions Options { get; init; }

    public required SimulationRunResult EngineResult { get; init; }

    public required AwgRunSummary Summary { get; init; }

    public required AwgSystemBalanceReport BalanceReport { get; init; }
}
