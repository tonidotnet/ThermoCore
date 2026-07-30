using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Boundary liquid-water sink that terminates a condensate or drain stream (GEN-006).
/// </summary>
public sealed class LiquidWaterSinkComponent : ISimulationComponent
{
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public LiquidWaterSinkComponent(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
        Ports =
        [
            new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.LiquidWater)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double LastMassFlowKgPerSecond { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastMassFlowKgPerSecond = 0.0;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.InputStates.TryGetValue("inlet", out var raw) || raw is not LiquidWaterState inlet)
        {
            return new ComponentStepResult
            {
                Diagnostics =
                [
                    new SimulationDiagnostic
                    {
                        Code = "COMPONENT.MISSING_INLET",
                        Severity = DiagnosticSeverity.Error,
                        Message = $"Component '{Id}' requires a LiquidWaterState on port 'inlet'.",
                        ComponentId = Id,
                        PortId = "inlet",
                        StepIndex = context.Simulation.StepIndex
                    }
                ],
                Balance = ConservationBalance.Empty
            };
        }

        FiniteNumber.RequireNonNegative(inlet.MassFlowKgPerSecond, nameof(inlet.MassFlowKgPerSecond));
        LastMassFlowKgPerSecond = inlet.MassFlowKgPerSecond;
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
