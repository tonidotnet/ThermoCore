using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Stateful liquid-water tank with capacity, temperature mixing, and overflow
/// (docs/01_Architecture/03_PhysicalArchitecture.md §38 / AWG-014).
/// </summary>
public sealed class WaterTankComponent : ISimulationComponent
{
    private readonly double _capacityKg;
    private readonly List<SimulationDiagnostic> _diagnostics = [];
    private double _storedMassKg;
    private double _temperatureK;

    public WaterTankComponent(
        string id,
        double capacityKg,
        double initialStoredMassKg = 0.0,
        double initialTemperatureK = 298.15)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.RequirePositive(capacityKg, nameof(capacityKg));
        FiniteNumber.RequireNonNegative(initialStoredMassKg, nameof(initialStoredMassKg));
        FiniteNumber.RequirePositive(initialTemperatureK, nameof(initialTemperatureK));
        if (initialStoredMassKg > capacityKg)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialStoredMassKg),
                "Initial stored mass cannot exceed tank capacity.");
        }

        Id = id;
        _capacityKg = capacityKg;
        _storedMassKg = initialStoredMassKg;
        _temperatureK = initialTemperatureK;
        Ports =
        [
            new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.LiquidWater),
            new PhysicalPort("overflow", id, PortDirection.Output, PhysicalDomain.LiquidWater, isRequired: false)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double CapacityKg => _capacityKg;

    public double StoredMassKg => _storedMassKg;

    public double TemperatureK => _temperatureK;

    public double LevelFraction => _capacityKg <= 0.0 ? 0.0 : _storedMassKg / _capacityKg;

    public double LastInletMassFlowKgPerSecond { get; private set; }

    public double LastOverflowMassFlowKgPerSecond { get; private set; }

    public bool LastOverflowActive { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastInletMassFlowKgPerSecond = 0.0;
        LastOverflowMassFlowKgPerSecond = 0.0;
        LastOverflowActive = false;
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
        FiniteNumber.RequirePositive(inlet.TemperatureK, nameof(inlet.TemperatureK));

        var dt = context.Simulation.TimeStep.TotalSeconds;
        var inletMassKg = inlet.MassFlowKgPerSecond * dt;
        var cp = ReferenceThermophysicalProperties.LiquidWaterSpecificHeatJPerKgK;

        var provisionalMassKg = _storedMassKg + inletMassKg;
        var overflowMassKg = Math.Max(0.0, provisionalMassKg - _capacityKg);
        var storedMassKg = provisionalMassKg - overflowMassKg;
        var overflowFlow = dt > 0.0 ? overflowMassKg / dt : 0.0;

        double nextTemperatureK;
        if (storedMassKg <= 0.0)
        {
            nextTemperatureK = inlet.TemperatureK;
            storedMassKg = 0.0;
        }
        else if (_storedMassKg <= 0.0)
        {
            nextTemperatureK = inlet.TemperatureK;
        }
        else
        {
            var retainedMassKg = Math.Min(_storedMassKg, storedMassKg);
            var addedMassKg = Math.Max(0.0, storedMassKg - retainedMassKg);
            var energyJ = retainedMassKg * cp * _temperatureK + addedMassKg * cp * inlet.TemperatureK;
            nextTemperatureK = energyJ / (storedMassKg * cp);
        }

        LastInletMassFlowKgPerSecond = inlet.MassFlowKgPerSecond;
        LastOverflowMassFlowKgPerSecond = overflowFlow;
        LastOverflowActive = overflowFlow > 0.0;

        var diagnostics = new List<SimulationDiagnostic>();
        if (LastOverflowActive)
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "TANK.OVERFLOW",
                Severity = DiagnosticSeverity.Warning,
                Message = $"Water tank '{Id}' overflowed during the timestep.",
                ComponentId = Id,
                StepIndex = context.Simulation.StepIndex,
                Values = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["overflowKg"] = overflowMassKg,
                    ["capacityKg"] = _capacityKg,
                    ["levelFraction"] = storedMassKg / _capacityKg
                }
            });
        }

        var storageChangeKgPerSecond = dt > 0.0 ? (storedMassKg - _storedMassKg) / dt : 0.0;
        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: 0.0,
            dryAirMassOutputKgPerSecond: 0.0,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: inlet.MassFlowKgPerSecond,
            waterMassOutputKgPerSecond: overflowFlow,
            waterMassStorageChangeKgPerSecond: storageChangeKgPerSecond,
            energyInputW: inlet.MassFlowKgPerSecond * cp * inlet.TemperatureK,
            energyOutputW: overflowFlow * cp * nextTemperatureK,
            storedEnergyChangeW: dt > 0.0
                ? ((storedMassKg * cp * nextTemperatureK) - (_storedMassKg * cp * _temperatureK)) / dt
                : 0.0,
            timeStep: context.Simulation.TimeStep);

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["overflow"] = new LiquidWaterState
                {
                    MassFlowKgPerSecond = overflowFlow,
                    TemperatureK = nextTemperatureK
                }
            },
            ProposedInternalState = new WaterTankInternalState(storedMassKg, nextTemperatureK),
            Balance = balance,
            Diagnostics = diagnostics
        };
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.ProposedInternalState is WaterTankInternalState state)
        {
            _storedMassKg = state.StoredMassKg;
            _temperatureK = state.TemperatureK;
        }

        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;

    private sealed record WaterTankInternalState(double StoredMassKg, double TemperatureK);
}
