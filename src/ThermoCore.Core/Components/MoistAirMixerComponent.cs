using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Adiabatic moist-air mixer (docs/02_Mathematics/04_MathematicalModel.md §37).
/// </summary>
public sealed class MoistAirMixerComponent : ISimulationComponent
{
    private readonly IPsychrometricCalculator _calculator;
    private readonly IReadOnlyList<string> _inletPortIds;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public MoistAirMixerComponent(
        string id,
        IReadOnlyList<string> inletPortIds,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(inletPortIds);
        if (inletPortIds.Count < 2)
        {
            throw new ArgumentException("A mixer requires at least two inlet ports.", nameof(inletPortIds));
        }

        Id = id;
        _calculator = calculator ?? new PsychrometricCalculator();
        _inletPortIds = inletPortIds.ToArray();

        var ports = new List<IPhysicalPort>(_inletPortIds.Count + 1);
        foreach (var inletId in _inletPortIds)
        {
            ports.Add(new PhysicalPort(inletId, id, PortDirection.Input, PhysicalDomain.MoistAir));
        }

        ports.Add(new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.MoistAir));
        Ports = ports;
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public void Initialize(SimulationContext context) => _diagnostics.Clear();

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var inlets = new List<MoistAirState>(_inletPortIds.Count);
        foreach (var inletId in _inletPortIds)
        {
            if (!context.InputStates.TryGetValue(inletId, out var raw) || raw is not MoistAirState state)
            {
                return MissingInlet(context, inletId);
            }

            inlets.Add(state);
        }

        var dryAirOut = inlets.Sum(s => s.DryAirMassFlowKgPerSecond);
        if (dryAirOut <= 0.0)
        {
            return Error(context, "COMPONENT.ZERO_FLOW", "Mixer dry-air outlet flow must be positive.");
        }

        var vaporOut = inlets.Sum(s => s.WaterVaporMassFlowKgPerSecond);
        var humidityRatioOut = inlets.Sum(s => s.DryAirMassFlowKgPerSecond * s.HumidityRatioKgPerKgDryAir) / dryAirOut;
        var enthalpyOut = inlets.Sum(s => s.DryAirMassFlowKgPerSecond * s.SpecificEnthalpyJPerKgDryAir) / dryAirOut;
        var pressureOut = inlets.Sum(s => s.DryAirMassFlowKgPerSecond * s.PressurePa) / dryAirOut;
        var temperatureOut = _calculator.CalculateTemperatureKFromEnthalpy(enthalpyOut, humidityRatioOut);

        var outlet = _calculator.CreateFromHumidityRatio(
            temperatureOut,
            pressureOut,
            humidityRatioOut,
            dryAirOut);

        // Rebuild may slightly adjust enthalpy; enforce mixed enthalpy for conservation reporting.
        var energyIn = inlets.Sum(s => s.DryAirMassFlowKgPerSecond * s.SpecificEnthalpyJPerKgDryAir);
        var energyOut = dryAirOut * outlet.SpecificEnthalpyJPerKgDryAir;

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: dryAirOut,
            dryAirMassOutputKgPerSecond: outlet.DryAirMassFlowKgPerSecond,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: vaporOut,
            waterMassOutputKgPerSecond: outlet.WaterVaporMassFlowKgPerSecond,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: energyIn,
            energyOutputW: energyOut,
            storedEnergyChangeW: 0.0,
            timeStep: context.Simulation.TimeStep);

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["outlet"] = outlet
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

    private ComponentStepResult MissingInlet(ComponentStepContext context, string portId)
        => Error(context, "COMPONENT.MISSING_INLET", $"Mixer '{Id}' requires MoistAirState on '{portId}'.", portId);

    private ComponentStepResult Error(
        ComponentStepContext context,
        string code,
        string message,
        string? portId = null)
        => new()
        {
            Diagnostics =
            [
                new SimulationDiagnostic
                {
                    Code = code,
                    Severity = DiagnosticSeverity.Error,
                    Message = message,
                    ComponentId = Id,
                    PortId = portId,
                    StepIndex = context.Simulation.StepIndex,
                    SimulationTime = context.Simulation.ElapsedTime
                }
            ],
            Balance = ConservationBalance.Empty
        };
}
