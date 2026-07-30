using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;

namespace ThermoCore.Core.Graph;

public interface ISimulationComponent
{
    string Id { get; }

    IReadOnlyList<IPhysicalPort> Ports { get; }

    void Initialize(SimulationContext context);

    ComponentStepResult Evaluate(ComponentStepContext context);

    void Commit(ComponentStepResult result);

    IReadOnlyList<SimulationDiagnostic> GetDiagnostics();
}
