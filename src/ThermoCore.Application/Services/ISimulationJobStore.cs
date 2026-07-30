using ThermoCore.Api.Contracts;

namespace ThermoCore.Api.Services;

public interface ISimulationJobStore
{
    CreateSimulationResponse Enqueue(CreateSimulationRequest request);

    SimulationJob? Get(string simulationId);

    bool TryCancel(string simulationId, out string? conflictReason);
}
