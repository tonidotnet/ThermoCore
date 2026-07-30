using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

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
