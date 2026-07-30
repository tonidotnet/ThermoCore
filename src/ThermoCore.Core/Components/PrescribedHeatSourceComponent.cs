using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Boundary prescribed heat source for optional thermal ports (external regeneration heat, etc.).
/// </summary>
public sealed class PrescribedHeatSourceComponent : ISimulationComponent
{
    private readonly HeatFlowState _state;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public PrescribedHeatSourceComponent(string id, double heatFlowW, double temperatureK)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.Require(heatFlowW, nameof(heatFlowW));
        FiniteNumber.RequirePositive(temperatureK, nameof(temperatureK));
        Id = id;
        _state = new HeatFlowState { HeatFlowW = heatFlowW, TemperatureK = temperatureK };
        Ports =
        [
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.Heat)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

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
