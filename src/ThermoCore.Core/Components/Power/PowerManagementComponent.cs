using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Power;

/// <summary>
/// Prioritized electrical load demand (docs/03_Components/12_BatteryAndPowerManagement.md §7).
/// Lower <see cref="Priority"/> values are served first.
/// </summary>
public sealed record ElectricalLoadDemand
{
    public required string LoadId { get; init; }

    public required double RequestedPowerW { get; init; }

    public required int Priority { get; init; }

    public required bool IsEssential { get; init; }

    public double MinimumAcceptedPowerW { get; init; }

    public ElectricalLoadDemand Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(LoadId);
        FiniteNumber.RequireNonNegative(RequestedPowerW, nameof(RequestedPowerW));
        FiniteNumber.RequireNonNegative(MinimumAcceptedPowerW, nameof(MinimumAcceptedPowerW));
        if (MinimumAcceptedPowerW > RequestedPowerW)
        {
            throw new ArgumentException("Minimum accepted power cannot exceed requested power.");
        }

        return this;
    }
}

/// <summary>
/// Allocates generation and battery discharge to prioritized loads, then charges from surplus
/// and reports PV curtailment (docs/03_Components/12_BatteryAndPowerManagement.md §8–§14 / PWR-005/PWR-006).
/// </summary>
public sealed class PowerManagementComponent : ISimulationComponent
{
    private readonly BatteryParameters _batteryParameters;
    private readonly double _mpptEfficiencyFraction;
    private readonly List<SimulationDiagnostic> _diagnostics = [];
    private readonly BatteryStorageComponent _battery;
    private IReadOnlyList<ElectricalLoadDemand> _loads;

    public PowerManagementComponent(
        string id,
        BatteryParameters batteryParameters,
        IReadOnlyList<ElectricalLoadDemand> loads,
        BatteryState? initialBatteryState = null,
        double mpptEfficiencyFraction = 0.95)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(loads);
        FiniteNumber.RequirePositive(mpptEfficiencyFraction, nameof(mpptEfficiencyFraction));
        if (mpptEfficiencyFraction > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(mpptEfficiencyFraction), "MPPT efficiency must be in (0, 1].");
        }

        Id = id;
        _batteryParameters = batteryParameters.Validate();
        _mpptEfficiencyFraction = mpptEfficiencyFraction;
        _loads = loads.Select(l => l.Validate()).ToArray();
        _battery = new BatteryStorageComponent($"{id}.battery", _batteryParameters, initialBatteryState);

        Ports =
        [
            new PhysicalPort("generation", id, PortDirection.Input, PhysicalDomain.Electricity, isRequired: false),
            new PhysicalPort("bus", id, PortDirection.Output, PhysicalDomain.Electricity),
            // Optional: graph consumers may observe curtailed surplus explicitly (PWR-006).
            new PhysicalPort("curtailed", id, PortDirection.Output, PhysicalDomain.Electricity, isRequired: false)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public BatteryState BatteryState => _battery.State;

    public double LastGeneratedPowerW { get; private set; }

    public double LastServedLoadPowerW { get; private set; }

    public double LastBatteryChargePowerW { get; private set; }

    public double LastBatteryDischargePowerW { get; private set; }

    public double LastCurtailedPowerW { get; private set; }

    public double LastUnservedPowerW { get; private set; }

    public IReadOnlyDictionary<string, double> LastDeliveredLoadPowerW { get; private set; }
        = new Dictionary<string, double>(StringComparer.Ordinal);

    public void SetLoads(IReadOnlyList<ElectricalLoadDemand> loads)
    {
        ArgumentNullException.ThrowIfNull(loads);
        _loads = loads.Select(l => l.Validate()).ToArray();
    }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        _battery.Initialize(context);
        LastGeneratedPowerW = 0.0;
        LastServedLoadPowerW = 0.0;
        LastBatteryChargePowerW = 0.0;
        LastBatteryDischargePowerW = 0.0;
        LastCurtailedPowerW = 0.0;
        LastUnservedPowerW = 0.0;
        LastDeliveredLoadPowerW = new Dictionary<string, double>(StringComparer.Ordinal);
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var diagnostics = new List<SimulationDiagnostic>();
        var dt = context.Simulation.TimeStep.TotalSeconds;
        if (dt <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(context), "Power management requires a positive timestep.");
        }

        var generationGrossW = 0.0;
        if (context.InputStates.TryGetValue("generation", out var genRaw)
            && genRaw is ElectricalPowerState generation)
        {
            FiniteNumber.RequireNonNegative(generation.PowerW, nameof(generation.PowerW));
            generationGrossW = generation.PowerW;
        }

        var generationNetW = generationGrossW * _mpptEfficiencyFraction;
        LastGeneratedPowerW = generationNetW;

        var orderedLoads = _loads
            .OrderBy(l => l.Priority)
            .ThenBy(l => l.LoadId, StringComparer.Ordinal)
            .ToArray();

        var delivered = new Dictionary<string, double>(StringComparer.Ordinal);
        var remainingGenerationW = generationNetW;
        var requestedTotalW = 0.0;

        foreach (var load in orderedLoads)
        {
            requestedTotalW += load.RequestedPowerW;
            var fromGen = Math.Min(load.RequestedPowerW, remainingGenerationW);
            delivered[load.LoadId] = fromGen;
            remainingGenerationW -= fromGen;
        }

        var deficitW = orderedLoads.Sum(l => l.RequestedPowerW - delivered[l.LoadId]);
        var energyBefore = _battery.State.StoredEnergyJ;

        var batteryContext = new ComponentStepContext
        {
            Simulation = context.Simulation,
            SolverIteration = context.SolverIteration,
            InputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["charge"] = new ElectricalPowerState { PowerW = 0.0 },
                ["discharge_request"] = new ElectricalPowerState
                {
                    PowerW = Math.Min(deficitW, _batteryParameters.MaximumDischargePowerW)
                }
            }
        };
        _ = _battery.Evaluate(batteryContext);
        var availableDischargeW = _battery.LastDischargePowerW;

        var remainingDischargeW = availableDischargeW;
        foreach (var load in orderedLoads)
        {
            var stillNeeded = load.RequestedPowerW - delivered[load.LoadId];
            if (stillNeeded <= 0.0 || remainingDischargeW <= 0.0)
            {
                continue;
            }

            var fromBattery = Math.Min(stillNeeded, remainingDischargeW);
            delivered[load.LoadId] += fromBattery;
            remainingDischargeW -= fromBattery;
        }

        var servedLoadW = delivered.Values.Sum();
        var actualDischargeW = availableDischargeW - remainingDischargeW;
        var surplusGenerationW = Math.Max(0.0, generationNetW - (servedLoadW - actualDischargeW));
        var chargeRequestW = Math.Min(surplusGenerationW, _batteryParameters.MaximumChargePowerW);

        batteryContext = batteryContext with
        {
            InputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["charge"] = new ElectricalPowerState { PowerW = chargeRequestW },
                ["discharge_request"] = new ElectricalPowerState { PowerW = actualDischargeW }
            }
        };

        var batteryResult = _battery.Evaluate(batteryContext);
        diagnostics.AddRange(batteryResult.Diagnostics);

        LastBatteryChargePowerW = _battery.LastChargePowerW;
        LastBatteryDischargePowerW = _battery.LastDischargePowerW;
        LastServedLoadPowerW = servedLoadW;
        LastUnservedPowerW = Math.Max(0.0, requestedTotalW - servedLoadW);
        LastCurtailedPowerW = Math.Max(
            0.0,
            generationNetW - (servedLoadW - LastBatteryDischargePowerW) - LastBatteryChargePowerW);
        LastDeliveredLoadPowerW = delivered;

        if (LastCurtailedPowerW > 1e-9)
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "POWER.SOLAR_CURTAILED",
                Severity = DiagnosticSeverity.Information,
                Message = "Surplus generation was curtailed after load service and battery charging.",
                ComponentId = Id,
                Values = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["curtailedPowerW"] = LastCurtailedPowerW
                }
            });
        }

        foreach (var load in orderedLoads)
        {
            var deliveredW = delivered[load.LoadId];
            if (deliveredW + 1e-12 >= load.RequestedPowerW)
            {
                continue;
            }

            diagnostics.Add(new SimulationDiagnostic
            {
                Code = load.IsEssential ? "POWER.ESSENTIAL_LOAD_UNSERVED" : "POWER.LOAD_SHED",
                Severity = load.IsEssential ? DiagnosticSeverity.Critical : DiagnosticSeverity.Information,
                Message = $"Load '{load.LoadId}' received {deliveredW:F3} W of {load.RequestedPowerW:F3} W requested.",
                ComponentId = Id,
                Values = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["requestedW"] = load.RequestedPowerW,
                    ["deliveredW"] = deliveredW
                }
            });
        }

        var proposedEnergy = batteryResult.ProposedInternalState is BatteryState proposed
            ? proposed.StoredEnergyJ
            : energyBefore;
        var storedElectricalChangeW = (proposedEnergy - energyBefore) / dt;
        var mpptLossW = generationGrossW - generationNetW;
        var chargeLossW = (1.0 - _batteryParameters.ChargeEfficiencyFraction) * LastBatteryChargePowerW;
        var dischargeLossW = LastBatteryDischargePowerW
            * (1.0 / _batteryParameters.DischargeEfficiencyFraction - 1.0);

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: 0.0,
            dryAirMassOutputKgPerSecond: 0.0,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: 0.0,
            waterMassOutputKgPerSecond: 0.0,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: generationGrossW,
            energyOutputW: servedLoadW + LastCurtailedPowerW + mpptLossW + chargeLossW + dischargeLossW
                + _batteryParameters.SelfDischargePowerW,
            storedEnergyChangeW: storedElectricalChangeW,
            timeStep: context.Simulation.TimeStep,
            electricalPowerInputW: generationGrossW,
            electricalPowerOutputW: servedLoadW + LastCurtailedPowerW + mpptLossW + chargeLossW + dischargeLossW
                + _batteryParameters.SelfDischargePowerW,
            storedElectricalPowerChangeW: storedElectricalChangeW);

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["bus"] = new ElectricalPowerState { PowerW = servedLoadW },
                ["curtailed"] = new ElectricalPowerState { PowerW = LastCurtailedPowerW }
            },
            ProposedInternalState = batteryResult.ProposedInternalState,
            Balance = balance,
            Diagnostics = diagnostics
        };
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _battery.Commit(new ComponentStepResult
        {
            ProposedInternalState = result.ProposedInternalState,
            Diagnostics = result.Diagnostics,
            Balance = result.Balance
        });
        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;
}
