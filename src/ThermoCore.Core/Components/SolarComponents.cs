using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Fidelity Level 1 constant-efficiency solar collector:
/// Q_useful = η · G_poa · A (docs/03_Components/06_SolarCollector.md §60).
/// </summary>
public sealed class ConstantEfficiencySolarCollectorComponent : ISimulationComponent
{
    private readonly IPsychrometricCalculator _calculator;
    private readonly double _efficiency;
    private readonly double _apertureAreaM2;
    private readonly double _fallbackIrradianceWPerM2;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public ConstantEfficiencySolarCollectorComponent(
        string id,
        double efficiency,
        double apertureAreaM2,
        double fallbackIrradianceWPerM2 = 0.0,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.Require(efficiency, nameof(efficiency));
        if (efficiency is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(efficiency), "Collector efficiency must be in [0, 1].");
        }

        FiniteNumber.RequirePositive(apertureAreaM2, nameof(apertureAreaM2));
        FiniteNumber.RequireNonNegative(fallbackIrradianceWPerM2, nameof(fallbackIrradianceWPerM2));

        Id = id;
        _efficiency = efficiency;
        _apertureAreaM2 = apertureAreaM2;
        _fallbackIrradianceWPerM2 = fallbackIrradianceWPerM2;
        _calculator = calculator ?? new PsychrometricCalculator();
        Ports =
        [
            new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.MoistAir),
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.MoistAir),
            new PhysicalPort("solar", id, PortDirection.Input, PhysicalDomain.SolarRadiation, isRequired: false)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double LastUsefulHeatW { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastUsefulHeatW = 0.0;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.InputStates.TryGetValue("inlet", out var raw) || raw is not MoistAirState inlet)
        {
            return Missing("COMPONENT.MISSING_INLET", "Collector requires MoistAirState on 'inlet'.", "inlet", context);
        }

        if (inlet.DryAirMassFlowKgPerSecond <= 0.0)
        {
            return Missing("COMPONENT.ZERO_FLOW", "Collector requires positive dry-air mass flow.", "inlet", context);
        }

        var irradiance = _fallbackIrradianceWPerM2;
        if (context.InputStates.TryGetValue("solar", out var solarRaw)
            && solarRaw is SolarIrradianceState solar)
        {
            FiniteNumber.RequireNonNegative(solar.IrradianceWPerM2, nameof(solar.IrradianceWPerM2));
            irradiance = solar.IrradianceWPerM2;
        }

        LastUsefulHeatW = _efficiency * irradiance * _apertureAreaM2;
        var enthalpyOut = inlet.SpecificEnthalpyJPerKgDryAir
            + LastUsefulHeatW / inlet.DryAirMassFlowKgPerSecond;
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
            energyInputW: inlet.DryAirMassFlowKgPerSecond * inlet.SpecificEnthalpyJPerKgDryAir + LastUsefulHeatW,
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

    private ComponentStepResult Missing(
        string code,
        string message,
        string portId,
        ComponentStepContext context)
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
                    StepIndex = context.Simulation.StepIndex
                }
            ],
            Balance = ConservationBalance.Empty
        };
}

/// <summary>
/// Fidelity Level 1 constant-efficiency PV:
/// P_out = η · G_poa · A (docs/03_Components/07_SolarPanel.md §52).
/// </summary>
public sealed class ConstantEfficiencySolarPanelComponent : ISimulationComponent
{
    private readonly double _efficiency;
    private readonly double _areaM2;
    private readonly double _fallbackIrradianceWPerM2;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public ConstantEfficiencySolarPanelComponent(
        string id,
        double efficiency,
        double areaM2,
        double fallbackIrradianceWPerM2 = 0.0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.Require(efficiency, nameof(efficiency));
        if (efficiency is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(efficiency), "PV efficiency must be in [0, 1].");
        }

        FiniteNumber.RequirePositive(areaM2, nameof(areaM2));
        FiniteNumber.RequireNonNegative(fallbackIrradianceWPerM2, nameof(fallbackIrradianceWPerM2));

        Id = id;
        _efficiency = efficiency;
        _areaM2 = areaM2;
        _fallbackIrradianceWPerM2 = fallbackIrradianceWPerM2;
        Ports =
        [
            new PhysicalPort("solar", id, PortDirection.Input, PhysicalDomain.SolarRadiation, isRequired: false),
            new PhysicalPort("electrical", id, PortDirection.Output, PhysicalDomain.Electricity)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double LastElectricalPowerW { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastElectricalPowerW = 0.0;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var irradiance = _fallbackIrradianceWPerM2;
        if (context.InputStates.TryGetValue("solar", out var solarRaw)
            && solarRaw is SolarIrradianceState solar)
        {
            FiniteNumber.RequireNonNegative(solar.IrradianceWPerM2, nameof(solar.IrradianceWPerM2));
            irradiance = solar.IrradianceWPerM2;
        }

        LastElectricalPowerW = _efficiency * irradiance * _areaM2;

        // Level-1 bookkeeping: exported DC power is reported on the electrical port.
        // Electrical residual uses "power entering component" convention, so generation is
        // tracked via total energy terms rather than ElectricalEnergyOutput alone.
        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: 0.0,
            dryAirMassOutputKgPerSecond: 0.0,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: 0.0,
            waterMassOutputKgPerSecond: 0.0,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: LastElectricalPowerW,
            energyOutputW: LastElectricalPowerW,
            storedEnergyChangeW: 0.0,
            timeStep: context.Simulation.TimeStep);

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["electrical"] = new ElectricalPowerState { PowerW = LastElectricalPowerW }
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

/// <summary>Boundary solar irradiance source.</summary>
public sealed class SolarRadiationSourceComponent : ISimulationComponent
{
    private readonly SolarIrradianceState _state;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public SolarRadiationSourceComponent(string id, double irradianceWPerM2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.RequireNonNegative(irradianceWPerM2, nameof(irradianceWPerM2));
        Id = id;
        _state = new SolarIrradianceState { IrradianceWPerM2 = irradianceWPerM2 };
        Ports =
        [
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.SolarRadiation)
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
