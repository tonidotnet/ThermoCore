using ThermoCore.Api.Contracts;

namespace ThermoCore.Api.Services;

public interface ISimulationJobStore
{
    CreateSimulationResponse Enqueue(CreateSimulationRequest request, string? idempotencyKey = null);

    SimulationJob? Get(string simulationId);

    IReadOnlyList<SimulationJob> List();

    bool TryCancel(string simulationId, out string? conflictReason);
}
