namespace ThermoCore.Core.Simulation;

/// <summary>
/// Compatibility wrapper around <see cref="SimulationEngine"/> for acyclic runs.
/// </summary>
public sealed class AcyclicSimulationEngine : ISimulationEngine
{
    private readonly SimulationEngine _engine;

    public AcyclicSimulationEngine()
    {
        _engine = new SimulationEngine();
    }

    public SimulationRunResult Run(
        SimulationRequest request,
        CancellationToken cancellationToken = default)
        => _engine.Run(request, cancellationToken);
}
