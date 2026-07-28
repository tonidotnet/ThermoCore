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

/// <summary>
/// Ideal moist-air splitter (docs/02_Mathematics/04_MathematicalModel.md §38).
/// </summary>
public sealed class MoistAirSplitterComponent : ISimulationComponent
{
    private readonly IPsychrometricCalculator _calculator;
    private readonly IReadOnlyList<double> _fractions;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public MoistAirSplitterComponent(
        string id,
        IReadOnlyList<double> outletFractions,
        IPsychrometricCalculator? calculator = null,
        double fractionTolerance = 1e-9)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(outletFractions);
        if (outletFractions.Count < 2)
        {
            throw new ArgumentException("A splitter requires at least two outlets.", nameof(outletFractions));
        }

        FiniteNumber.RequirePositive(fractionTolerance, nameof(fractionTolerance));
        if (outletFractions.Any(f => f < 0.0 || f > 1.0))
        {
            throw new ArgumentOutOfRangeException(nameof(outletFractions), "Split fractions must be in [0, 1].");
        }

        var sum = outletFractions.Sum();
        if (Math.Abs(sum - 1.0) > fractionTolerance)
        {
            throw new ArgumentException("Split fractions must sum to 1.0.", nameof(outletFractions));
        }

        Id = id;
        _calculator = calculator ?? new PsychrometricCalculator();
        _fractions = outletFractions.ToArray();

        var ports = new List<IPhysicalPort>(_fractions.Count + 1)
        {
            new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.MoistAir)
        };

        for (var i = 0; i < _fractions.Count; i++)
        {
            ports.Add(new PhysicalPort($"outlet_{i}", id, PortDirection.Output, PhysicalDomain.MoistAir));
        }

        Ports = ports;
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public void Initialize(SimulationContext context) => _diagnostics.Clear();

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.InputStates.TryGetValue("inlet", out var raw) || raw is not MoistAirState inlet)
        {
            return new ComponentStepResult
            {
                Diagnostics =
                [
                    new SimulationDiagnostic
                    {
                        Code = "COMPONENT.MISSING_INLET",
                        Severity = DiagnosticSeverity.Error,
                        Message = $"Splitter '{Id}' requires MoistAirState on 'inlet'.",
                        ComponentId = Id,
                        PortId = "inlet",
                        StepIndex = context.Simulation.StepIndex,
                        SimulationTime = context.Simulation.ElapsedTime
                    }
                ],
                Balance = ConservationBalance.Empty
            };
        }

        var outputs = new Dictionary<string, object?>(StringComparer.Ordinal);
        var dryAirOutTotal = 0.0;
        var vaporOutTotal = 0.0;
        var energyOutTotal = 0.0;

        for (var i = 0; i < _fractions.Count; i++)
        {
            var fraction = _fractions[i];
            var dryAirFlow = fraction * inlet.DryAirMassFlowKgPerSecond;
            var outlet = _calculator.CreateFromHumidityRatio(
                inlet.TemperatureK,
                inlet.PressurePa,
                inlet.HumidityRatioKgPerKgDryAir,
                dryAirFlow);

            outputs[$"outlet_{i}"] = outlet;
            dryAirOutTotal += outlet.DryAirMassFlowKgPerSecond;
            vaporOutTotal += outlet.WaterVaporMassFlowKgPerSecond;
            energyOutTotal += outlet.DryAirMassFlowKgPerSecond * outlet.SpecificEnthalpyJPerKgDryAir;
        }

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: inlet.DryAirMassFlowKgPerSecond,
            dryAirMassOutputKgPerSecond: dryAirOutTotal,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: inlet.WaterVaporMassFlowKgPerSecond,
            waterMassOutputKgPerSecond: vaporOutTotal,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: inlet.DryAirMassFlowKgPerSecond * inlet.SpecificEnthalpyJPerKgDryAir,
            energyOutputW: energyOutTotal,
            storedEnergyChangeW: 0.0,
            timeStep: context.Simulation.TimeStep);

        return new ComponentStepResult
        {
            OutputStates = outputs,
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
/// Constant-heat-rate sensible heater: humidity ratio conserved, enthalpy increases by Q/m_da.
/// </summary>
public sealed class SensibleHeaterComponent : ISimulationComponent
{
    private readonly IPsychrometricCalculator _calculator;
    private readonly double _heatRateW;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public SensibleHeaterComponent(
        string id,
        double heatRateW,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.Require(heatRateW, nameof(heatRateW));
        Id = id;
        _heatRateW = heatRateW;
        _calculator = calculator ?? new PsychrometricCalculator();
        Ports =
        [
            new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.MoistAir),
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.MoistAir)
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
            return new ComponentStepResult
            {
                Diagnostics =
                [
                    new SimulationDiagnostic
                    {
                        Code = "COMPONENT.MISSING_INLET",
                        Severity = DiagnosticSeverity.Error,
                        Message = $"Heater '{Id}' requires MoistAirState on 'inlet'.",
                        ComponentId = Id,
                        PortId = "inlet",
                        StepIndex = context.Simulation.StepIndex,
                        SimulationTime = context.Simulation.ElapsedTime
                    }
                ],
                Balance = ConservationBalance.Empty
            };
        }

        if (inlet.DryAirMassFlowKgPerSecond <= 0.0)
        {
            return new ComponentStepResult
            {
                Diagnostics =
                [
                    new SimulationDiagnostic
                    {
                        Code = "COMPONENT.ZERO_FLOW",
                        Severity = DiagnosticSeverity.Error,
                        Message = $"Heater '{Id}' requires positive dry-air mass flow.",
                        ComponentId = Id,
                        PortId = "inlet",
                        StepIndex = context.Simulation.StepIndex,
                        SimulationTime = context.Simulation.ElapsedTime
                    }
                ],
                Balance = ConservationBalance.Empty
            };
        }

        var enthalpyOut = inlet.SpecificEnthalpyJPerKgDryAir
            + _heatRateW / inlet.DryAirMassFlowKgPerSecond;
        var temperatureOut = _calculator.CalculateTemperatureKFromEnthalpy(
            enthalpyOut,
            inlet.HumidityRatioKgPerKgDryAir);

        var outlet = _calculator.CreateFromHumidityRatio(
            temperatureOut,
            inlet.PressurePa,
            inlet.HumidityRatioKgPerKgDryAir,
            inlet.DryAirMassFlowKgPerSecond);

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: inlet.DryAirMassFlowKgPerSecond,
            dryAirMassOutputKgPerSecond: outlet.DryAirMassFlowKgPerSecond,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: inlet.WaterVaporMassFlowKgPerSecond,
            waterMassOutputKgPerSecond: outlet.WaterVaporMassFlowKgPerSecond,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: inlet.DryAirMassFlowKgPerSecond * inlet.SpecificEnthalpyJPerKgDryAir + _heatRateW,
            energyOutputW: outlet.DryAirMassFlowKgPerSecond * outlet.SpecificEnthalpyJPerKgDryAir,
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
}
