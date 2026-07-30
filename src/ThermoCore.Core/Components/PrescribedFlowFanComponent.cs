using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Prescribed-flow fan: forces dry-air mass flow, applies pressure rise, reports electrical power
/// (docs/03_Components/13_FanAndAirflow.md §8–§9).
/// </summary>
public sealed class PrescribedFlowFanComponent : ISimulationComponent
{
    private readonly IPsychrometricCalculator _calculator;
    private readonly double _dryAirMassFlowKgPerSecond;
    private readonly double _pressureRisePa;
    private readonly double _fanEfficiency;
    private readonly double _driverEfficiency;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public PrescribedFlowFanComponent(
        string id,
        double dryAirMassFlowKgPerSecond,
        double pressureRisePa,
        double fanEfficiency = 0.60,
        double driverEfficiency = 0.90,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.RequirePositive(dryAirMassFlowKgPerSecond, nameof(dryAirMassFlowKgPerSecond));
        FiniteNumber.RequireNonNegative(pressureRisePa, nameof(pressureRisePa));
        FiniteNumber.RequirePositive(fanEfficiency, nameof(fanEfficiency));
        FiniteNumber.RequirePositive(driverEfficiency, nameof(driverEfficiency));
        if (fanEfficiency > 1.0 || driverEfficiency > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(fanEfficiency), "Efficiencies must be in (0, 1].");
        }

        Id = id;
        _dryAirMassFlowKgPerSecond = dryAirMassFlowKgPerSecond;
        _pressureRisePa = pressureRisePa;
        _fanEfficiency = fanEfficiency;
        _driverEfficiency = driverEfficiency;
        _calculator = calculator ?? new PsychrometricCalculator();
        Ports =
        [
            new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.MoistAir),
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.MoistAir)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double LastElectricalPowerW { get; private set; }

    public double LastAirPowerW { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastElectricalPowerW = 0.0;
        LastAirPowerW = 0.0;
    }

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
                        Message = $"Fan '{Id}' requires MoistAirState on 'inlet'.",
                        ComponentId = Id,
                        PortId = "inlet",
                        StepIndex = context.Simulation.StepIndex
                    }
                ],
                Balance = ConservationBalance.Empty
            };
        }

        var outlet = _calculator.CreateFromHumidityRatio(
            inlet.TemperatureK,
            inlet.PressurePa + _pressureRisePa,
            inlet.HumidityRatioKgPerKgDryAir,
            _dryAirMassFlowKgPerSecond);

        var volumetricFlow = outlet.DryAirMassFlowKgPerSecond * outlet.SpecificVolumeM3PerKgDryAir;
        LastAirPowerW = _pressureRisePa * volumetricFlow;
        LastElectricalPowerW = LastAirPowerW / (_fanEfficiency * _driverEfficiency);

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: _dryAirMassFlowKgPerSecond,
            dryAirMassOutputKgPerSecond: outlet.DryAirMassFlowKgPerSecond,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: _dryAirMassFlowKgPerSecond * inlet.HumidityRatioKgPerKgDryAir,
            waterMassOutputKgPerSecond: outlet.WaterVaporMassFlowKgPerSecond,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: outlet.DryAirMassFlowKgPerSecond * outlet.SpecificEnthalpyJPerKgDryAir,
            energyOutputW: outlet.DryAirMassFlowKgPerSecond * outlet.SpecificEnthalpyJPerKgDryAir,
            storedEnergyChangeW: 0.0,
            timeStep: context.Simulation.TimeStep,
            electricalPowerInputW: LastElectricalPowerW,
            electricalPowerOutputW: LastElectricalPowerW);

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
