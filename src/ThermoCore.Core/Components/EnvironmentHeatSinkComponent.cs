using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Boundary heat sink that terminates a heat stream (GEN-004).
/// </summary>
public sealed class EnvironmentHeatSinkComponent : ISimulationComponent
{
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public EnvironmentHeatSinkComponent(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
        Ports =
        [
            new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.Heat)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double LastHeatFlowW { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastHeatFlowW = 0.0;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.InputStates.TryGetValue("inlet", out var raw) || raw is not HeatFlowState inlet)
        {
            return new ComponentStepResult
            {
                Diagnostics =
                [
                    new SimulationDiagnostic
                    {
                        Code = "COMPONENT.MISSING_INLET",
                        Severity = DiagnosticSeverity.Error,
                        Message = $"Component '{Id}' requires a HeatFlowState on port 'inlet'.",
                        ComponentId = Id,
                        PortId = "inlet",
                        StepIndex = context.Simulation.StepIndex
                    }
                ],
                Balance = ConservationBalance.Empty
            };
        }

        FiniteNumber.Require(inlet.HeatFlowW, nameof(inlet.HeatFlowW));
        LastHeatFlowW = inlet.HeatFlowW;
        return new ComponentStepResult { Balance = ConservationBalance.Empty };
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;
}
