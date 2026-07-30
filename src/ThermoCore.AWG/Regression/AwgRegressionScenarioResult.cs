using ThermoCore.AWG.Simulation;

namespace ThermoCore.AWG.Regression;

/// <summary>Outcome of one regression scenario execution.</summary>
public sealed record AwgRegressionScenarioResult
{
    public required AwgRegressionScenario Scenario { get; init; }

    public required AwgSimulationRunResult Run { get; init; }

    public required bool Passed { get; init; }

    public required IReadOnlyList<string> Failures { get; init; }
}
