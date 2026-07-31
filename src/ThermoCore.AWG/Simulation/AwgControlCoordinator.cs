using ThermoCore.AWG.Control;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Components;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Simulation;

namespace ThermoCore.AWG.Simulation;

/// <summary>
/// Runs the supervisory AWG controller each timestep and applies actuator requests
/// to mutable plant components (collector air coupling, condenser cooling).
/// </summary>
public sealed class AwgControlCoordinator : ISimulationStepHook
{
    private readonly AwgBuiltSystem _built;
    private readonly IAwgController _controller;
    private readonly AwgControlParameters _parameters;
    private readonly DynamicLumpedSolarCollectorComponent _collector;
    private readonly ControllableHeatSourceComponent _condenserCooling;
    private readonly List<AwgDecisionTraceEntry> _decisionTrace = [];
    private AwgControllerState _state;
    private double _targetAirCouplingFraction;

    public AwgControlCoordinator(
        AwgBuiltSystem built,
        IAwgController? controller = null,
        AwgControlParameters? parameters = null,
        AwgOperatingMode initialMode = AwgOperatingMode.Off)
    {
        ArgumentNullException.ThrowIfNull(built);
        _built = built;
        _controller = controller ?? new RuleBasedAwgController();
        _parameters = (parameters ?? RuleBasedAwgController.CreateDefaultParameters()).Validate();
        _state = AwgControllerState.CreateInitial(initialMode);

        _collector = built.Graph.Components.OfType<DynamicLumpedSolarCollectorComponent>()
                .FirstOrDefault(c => c.Id == AwgV3TopologyIds.SolarCollector)
            ?? throw new InvalidOperationException("Solar collector component is required for control.");
        _condenserCooling = built.Graph.Components.OfType<ControllableHeatSourceComponent>()
                .FirstOrDefault(c => c.Id == AwgV3TopologyIds.CondenserCooling)
            ?? throw new InvalidOperationException("Condenser cooling actuator is required for control.");

        // Start in adsorption-friendly plant pose until the first evaluation.
        _targetAirCouplingFraction = 0.0;
        _collector.AirCouplingFraction = 0.0;
        _condenserCooling.Set(0.0, built.Configuration.Condenser.FallbackSurfaceTemperatureK);
    }

    public AwgControllerState CurrentState => _state;

    public AwgControlRequest? LastRequest { get; private set; }

    public IReadOnlyList<AwgDecisionTraceEntry> DecisionTrace => _decisionTrace;

    public void BeforeStep(
        SimulationContext context,
        IReadOnlyDictionary<string, object?> committedPortStates)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(committedPortStates);

        var observation = context.StepIndex == 0
            ? AwgSystemObservationBuilder.CreateSeed(_built, context.SimulationStart)
            : AwgSystemObservationBuilder.CreateFromCommittedState(_built, context, committedPortStates);

        var result = _controller.Evaluate(observation, _state, _parameters, context.TimeStep);
        _state = result.ProposedState;
        LastRequest = result.Request;
        _decisionTrace.AddRange(result.DecisionTrace);

        Apply(result.Request, observation, context.TimeStep);
    }

    private void Apply(
        AwgControlRequest request,
        AwgSystemObservation observation,
        TimeSpan timeStep)
    {
        _targetAirCouplingFraction = request.RegenerationHeatEnabled ? 1.0 : 0.0;
        // Soft-ramp collector coupling over ~3 minutes to keep heat-recovery tears stable.
        const double rampPerSecond = 1.0 / 180.0;
        var step = rampPerSecond * Math.Max(timeStep.TotalSeconds, 1.0);
        var current = _collector.AirCouplingFraction;
        if (current < _targetAirCouplingFraction)
        {
            _collector.AirCouplingFraction = Math.Min(_targetAirCouplingFraction, current + step);
        }
        else if (current > _targetAirCouplingFraction)
        {
            _collector.AirCouplingFraction = Math.Max(_targetAirCouplingFraction, current - step);
        }

        var coolingW = request.CondenserEnabled ? Math.Max(0.0, request.PeltierPowerRequestW) : 0.0;
        // Keep a cold surface available during regeneration even if the dew-point gate
        // has not yet armed CondenserEnabled (humidity rises after desorption starts).
        if (!request.CondenserEnabled
            && request.RequestedMode == AwgOperatingMode.Regeneration
            && observation.AvailableElectricalPowerW > 0.0)
        {
            coolingW = Math.Min(
                _parameters.NominalPeltierPowerRequestW,
                observation.AvailableElectricalPowerW);
        }

        var surfaceK = coolingW > 0.0
            ? Math.Min(
                _built.Configuration.Condenser.FallbackSurfaceTemperatureK,
                Math.Max(
                    255.0,
                    observation.CondenserInletDewPointTemperatureK - _parameters.TargetDewPointApproachK))
            : _built.Configuration.Condenser.FallbackSurfaceTemperatureK;

        _condenserCooling.Set(coolingW, surfaceK);
    }
}
