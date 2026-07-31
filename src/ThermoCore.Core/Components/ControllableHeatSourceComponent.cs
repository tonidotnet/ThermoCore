using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Mutable prescribed heat source for supervisory actuators (e.g. condenser cooling request).
/// </summary>
public sealed class ControllableHeatSourceComponent : ISimulationComponent
{
    private readonly List<SimulationDiagnostic> _diagnostics = [];
    private double _heatFlowW;
    private double _temperatureK;

    public ControllableHeatSourceComponent(string id, double heatFlowW, double temperatureK)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.Require(heatFlowW, nameof(heatFlowW));
        FiniteNumber.RequirePositive(temperatureK, nameof(temperatureK));
        Id = id;
        _heatFlowW = heatFlowW;
        _temperatureK = temperatureK;
        Ports =
        [
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.Heat)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double HeatFlowW => _heatFlowW;

    public double TemperatureK => _temperatureK;

    public void Set(double heatFlowW, double temperatureK)
    {
        FiniteNumber.Require(heatFlowW, nameof(heatFlowW));
        FiniteNumber.RequirePositive(temperatureK, nameof(temperatureK));
        _heatFlowW = heatFlowW;
        _temperatureK = temperatureK;
    }

    public void Initialize(SimulationContext context) => _diagnostics.Clear();

    public ComponentStepResult Evaluate(ComponentStepContext context)
        => new()
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["outlet"] = new HeatFlowState
                {
                    HeatFlowW = _heatFlowW,
                    TemperatureK = _temperatureK
                }
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
