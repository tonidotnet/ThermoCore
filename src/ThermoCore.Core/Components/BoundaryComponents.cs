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

/// <summary>
/// Boundary electrical sink that terminates a power stream.
/// </summary>
public sealed class ElectricalLoadSinkComponent : ISimulationComponent
{
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public ElectricalLoadSinkComponent(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
        Ports =
        [
            new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.Electricity)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double LastPowerW { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastPowerW = 0.0;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.InputStates.TryGetValue("inlet", out var raw) || raw is not ElectricalPowerState inlet)
        {
            return new ComponentStepResult
            {
                Diagnostics =
                [
                    new SimulationDiagnostic
                    {
                        Code = "COMPONENT.MISSING_INLET",
                        Severity = DiagnosticSeverity.Error,
                        Message = $"Component '{Id}' requires an ElectricalPowerState on port 'inlet'.",
                        ComponentId = Id,
                        PortId = "inlet",
                        StepIndex = context.Simulation.StepIndex
                    }
                ],
                Balance = ConservationBalance.Empty
            };
        }

        FiniteNumber.RequireNonNegative(inlet.PowerW, nameof(inlet.PowerW));
        LastPowerW = inlet.PowerW;
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
