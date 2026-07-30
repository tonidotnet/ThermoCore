using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;

namespace ThermoCore.Core.Components;

/// <summary>
/// Deprecated filename alias. Prefer <c>SimulationComponentTemplate.cs</c>.
/// </summary>
public sealed class ComponentTemplate : ISimulationComponent
{
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public ComponentTemplate(string id)
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
        throw new NotImplementedException($"{Id} is a template and is not executable.");
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;
}
