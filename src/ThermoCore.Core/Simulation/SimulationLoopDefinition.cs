namespace ThermoCore.Core.Simulation;

/// <summary>
/// Describes a single feedback tear used by the fixed-point loop solver
/// (docs/04_Simulation/16_SimulationEngine.md §10–§11).
/// </summary>
public sealed record SimulationLoopDefinition
{
    public required string Id { get; init; }

    /// <summary>Connection id of the feedback edge to tear.</summary>
    public required string TearConnectionId { get; init; }

    public double RelaxationFactor { get; init; } = 0.5;

    public int MaximumIterations { get; init; } = 100;
}
