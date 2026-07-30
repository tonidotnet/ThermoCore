using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

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
