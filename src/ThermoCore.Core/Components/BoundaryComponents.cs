using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;

namespace ThermoCore.Core.Components;

/// <summary>
/// Boundary moist-air source with a configured immutable outlet state.
/// </summary>
public sealed class AmbientAirSourceComponent : ISimulationComponent
{
    private readonly MoistAirState _outletState;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public AmbientAirSourceComponent(string id, MoistAirState outletState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(outletState);
        Id = id;
        _outletState = outletState;
        Ports =
        [
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.MoistAir)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public void Initialize(SimulationContext context) => _diagnostics.Clear();

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var dt = context.Simulation.TimeStep;
        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: _outletState.DryAirMassFlowKgPerSecond,
            dryAirMassOutputKgPerSecond: _outletState.DryAirMassFlowKgPerSecond,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: _outletState.WaterVaporMassFlowKgPerSecond,
            waterMassOutputKgPerSecond: _outletState.WaterVaporMassFlowKgPerSecond,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: _outletState.DryAirMassFlowKgPerSecond * _outletState.SpecificEnthalpyJPerKgDryAir,
            energyOutputW: _outletState.DryAirMassFlowKgPerSecond * _outletState.SpecificEnthalpyJPerKgDryAir,
            storedEnergyChangeW: 0.0,
            timeStep: dt);

        // Boundary source: external environment supplies the stream (input = output, residual 0).
        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["outlet"] = _outletState
            },
            Balance = balance
        };
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;
}

/// <summary>
/// Boundary moist-air sink that terminates a stream.
/// </summary>
public sealed class ExhaustAirSinkComponent : ISimulationComponent
{
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public ExhaustAirSinkComponent(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
        Ports =
        [
            new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.MoistAir)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public void Initialize(SimulationContext context) => _diagnostics.Clear();

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.InputStates.TryGetValue("inlet", out var raw) || raw is not MoistAirState inlet)
        {
            return MissingInlet(context);
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

        return new ComponentStepResult { Balance = balance };
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;

    private ComponentStepResult MissingInlet(ComponentStepContext context)
        => new()
        {
            Diagnostics =
            [
                new SimulationDiagnostic
                {
                    Code = "COMPONENT.MISSING_INLET",
                    Severity = DiagnosticSeverity.Error,
                    Message = $"Component '{Id}' requires a MoistAirState on port 'inlet'.",
                    ComponentId = Id,
                    PortId = "inlet",
                    StepIndex = context.Simulation.StepIndex,
                    SimulationTime = context.Simulation.ElapsedTime
                }
            ],
            Balance = ConservationBalance.Empty
        };
}
