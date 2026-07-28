using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;

namespace ThermoCore.Core.Components;

/// <summary>
/// Steady moist-air pass-through used to exercise Evaluate/Commit and conservation reporting.
/// </summary>
public sealed class MoistAirPassThroughComponent : ISimulationComponent
{
    private readonly IPhysicalPort _inlet;
    private readonly IPhysicalPort _outlet;
    private ComponentStepResult? _lastCommitted;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public MoistAirPassThroughComponent(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
        _inlet = new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.MoistAir);
        _outlet = new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.MoistAir);
        Ports = [_inlet, _outlet];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public void Initialize(SimulationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _diagnostics.Clear();
        _lastCommitted = null;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.InputStates.TryGetValue(_inlet.Id, out var raw) || raw is not MoistAirState inlet)
        {
            var diagnostic = new SimulationDiagnostic
            {
                Code = "COMPONENT.MISSING_INLET",
                Severity = DiagnosticSeverity.Error,
                Message = $"Component '{Id}' requires a MoistAirState on port '{_inlet.Id}'.",
                ComponentId = Id,
                PortId = _inlet.Id,
                StepIndex = context.Simulation.StepIndex,
                SimulationTime = context.Simulation.ElapsedTime,
                SolverIteration = context.SolverIteration
            };

            return new ComponentStepResult
            {
                Diagnostics = [diagnostic],
                Balance = ConservationBalance.Empty
            };
        }

        var dt = context.Simulation.TimeStep;
        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: inlet.DryAirMassFlowKgPerSecond,
            dryAirMassOutputKgPerSecond: inlet.DryAirMassFlowKgPerSecond,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: inlet.WaterVaporMassFlowKgPerSecond,
            waterMassOutputKgPerSecond: inlet.WaterVaporMassFlowKgPerSecond,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: inlet.DryAirMassFlowKgPerSecond * inlet.SpecificEnthalpyJPerKgDryAir,
            energyOutputW: inlet.DryAirMassFlowKgPerSecond * inlet.SpecificEnthalpyJPerKgDryAir,
            storedEnergyChangeW: 0.0,
            timeStep: dt);

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [_outlet.Id] = inlet
            },
            ProposedInternalState = null,
            Balance = balance,
            Diagnostics = Array.Empty<SimulationDiagnostic>()
        };
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _lastCommitted = result;
        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;

    public ComponentStepResult? LastCommitted => _lastCommitted;
}
