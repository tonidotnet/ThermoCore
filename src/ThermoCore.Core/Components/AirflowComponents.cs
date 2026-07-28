using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Passive duct with reference-curve pressure loss; moist-air state otherwise unchanged
/// except outlet pressure is reduced by Δp (docs/03_Components/13_FanAndAirflow.md).
/// </summary>
public sealed class DuctPressureLossComponent : ISimulationComponent
{
    private readonly IPsychrometricCalculator _calculator;
    private readonly double _pressureDropRefPa;
    private readonly double _volumetricFlowRefM3PerSecond;
    private readonly double _exponent;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public DuctPressureLossComponent(
        string id,
        double pressureDropRefPa,
        double volumetricFlowRefM3PerSecond,
        double exponent = 2.0,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.RequireNonNegative(pressureDropRefPa, nameof(pressureDropRefPa));
        FiniteNumber.RequirePositive(volumetricFlowRefM3PerSecond, nameof(volumetricFlowRefM3PerSecond));
        FiniteNumber.RequirePositive(exponent, nameof(exponent));

        Id = id;
        _pressureDropRefPa = pressureDropRefPa;
        _volumetricFlowRefM3PerSecond = volumetricFlowRefM3PerSecond;
        _exponent = exponent;
        _calculator = calculator ?? new PsychrometricCalculator();
        Ports =
        [
            new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.MoistAir),
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.MoistAir)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double LastPressureDropPa { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastPressureDropPa = 0.0;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.InputStates.TryGetValue("inlet", out var raw) || raw is not MoistAirState inlet)
        {
            return MissingInlet(context);
        }

        var volumetricFlow = inlet.DryAirMassFlowKgPerSecond * inlet.SpecificVolumeM3PerKgDryAir;
        var ratio = volumetricFlow / _volumetricFlowRefM3PerSecond;
        LastPressureDropPa = _pressureDropRefPa * Math.Pow(Math.Abs(ratio), _exponent);

        var outletPressure = inlet.PressurePa - LastPressureDropPa;
        if (outletPressure <= 0.0)
        {
            return new ComponentStepResult
            {
                Diagnostics =
                [
                    new SimulationDiagnostic
                    {
                        Code = "DUCT.NON_POSITIVE_PRESSURE",
                        Severity = DiagnosticSeverity.Error,
                        Message = $"Duct '{Id}' outlet pressure would be non-positive.",
                        ComponentId = Id,
                        Values = new Dictionary<string, double>(StringComparer.Ordinal)
                        {
                            ["inletPressurePa"] = inlet.PressurePa,
                            ["pressureDropPa"] = LastPressureDropPa
                        }
                    }
                ],
                Balance = ConservationBalance.Empty
            };
        }

        var outlet = _calculator.CreateFromHumidityRatio(
            inlet.TemperatureK,
            outletPressure,
            inlet.HumidityRatioKgPerKgDryAir,
            inlet.DryAirMassFlowKgPerSecond);

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: inlet.DryAirMassFlowKgPerSecond,
            dryAirMassOutputKgPerSecond: outlet.DryAirMassFlowKgPerSecond,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: inlet.WaterVaporMassFlowKgPerSecond,
            waterMassOutputKgPerSecond: outlet.WaterVaporMassFlowKgPerSecond,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: inlet.DryAirMassFlowKgPerSecond * inlet.SpecificEnthalpyJPerKgDryAir,
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

    private ComponentStepResult MissingInlet(ComponentStepContext context)
        => new()
        {
            Diagnostics =
            [
                new SimulationDiagnostic
                {
                    Code = "COMPONENT.MISSING_INLET",
                    Severity = DiagnosticSeverity.Error,
                    Message = $"Duct '{Id}' requires MoistAirState on 'inlet'.",
                    ComponentId = Id,
                    PortId = "inlet",
                    StepIndex = context.Simulation.StepIndex
                }
            ],
            Balance = ConservationBalance.Empty
        };
}

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
