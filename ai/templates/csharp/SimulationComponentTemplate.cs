using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;

namespace ThermoCore.Core.Components;

/// <summary>
/// Template for a new simulation component. Rename the type and replace stub logic.
/// </summary>
public sealed class SimulationComponentTemplate : ISimulationComponent
{
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public SimulationComponentTemplate(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
        Ports =
        [
            new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.MoistAir),
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.MoistAir)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public void Initialize(SimulationContext context) => _diagnostics.Clear();

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // TODO: Read typed inlet state, compute outlets, build ConservationBalance.
        throw new NotImplementedException($"{Id} is a template and is not executable.");
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
        // TODO: Apply ProposedInternalState when the component stores dynamic state.
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;
}
