using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

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
