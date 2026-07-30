using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Boundary prescribed electrical power source (GEN-003).
/// </summary>
public sealed class PrescribedElectricalSourceComponent : ISimulationComponent
{
    private readonly ElectricalPowerState _state;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public PrescribedElectricalSourceComponent(string id, double powerW)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.RequireNonNegative(powerW, nameof(powerW));
        Id = id;
        _state = new ElectricalPowerState { PowerW = powerW };
        Ports =
        [
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.Electricity)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double LastPowerW => _state.PowerW;

    public void Initialize(SimulationContext context) => _diagnostics.Clear();

    public ComponentStepResult Evaluate(ComponentStepContext context)
        => new()
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["outlet"] = _state
            },
            Balance = ConservationBalance.Empty
        };

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;
}
