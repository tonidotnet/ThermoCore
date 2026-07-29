using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Power;

/// <summary>
/// Battery SOC / energy-storage component (docs/03_Components/12_BatteryAndPowerManagement.md §5–§6, PWR-002).
/// Charge and discharge requests are accepted subject to power and SOC limits.
/// </summary>
public sealed class BatteryStorageComponent : ISimulationComponent
{
    private readonly BatteryParameters _parameters;
    private readonly List<SimulationDiagnostic> _diagnostics = [];
    private BatteryState _state;

    public BatteryStorageComponent(
        string id,
        BatteryParameters parameters,
        BatteryState? initialState = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(parameters);
        _parameters = parameters.Validate();
        Id = id;
        _state = initialState ?? BatteryState.Create(
            storedEnergyJ: _parameters.NominalCapacityJ * 0.5
                * (_parameters.MinimumSocFraction + _parameters.MaximumSocFraction),
            nominalCapacityJ: _parameters.NominalCapacityJ,
            batteryTemperatureK: _parameters.InitialTemperatureK);

        if (_state.StateOfChargeFraction < _parameters.MinimumSocFraction
            || _state.StateOfChargeFraction > _parameters.MaximumSocFraction)
        {
            throw new ArgumentException(
                "Initial battery SOC is outside configured operating bounds.",
                nameof(initialState));
        }

        Ports =
        [
            new PhysicalPort("charge", id, PortDirection.Input, PhysicalDomain.Electricity, isRequired: false),
            new PhysicalPort("discharge_request", id, PortDirection.Input, PhysicalDomain.Electricity, isRequired: false),
            new PhysicalPort("discharge", id, PortDirection.Output, PhysicalDomain.Electricity)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public BatteryState State => _state;

    public double LastChargePowerW { get; private set; }

    public double LastDischargePowerW { get; private set; }

    public double LastRejectedChargePowerW { get; private set; }

    public double LastUnservedDischargePowerW { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastChargePowerW = 0.0;
        LastDischargePowerW = 0.0;
        LastRejectedChargePowerW = 0.0;
        LastUnservedDischargePowerW = 0.0;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var diagnostics = new List<SimulationDiagnostic>();
        var dt = context.Simulation.TimeStep.TotalSeconds;
        if (dt <= 0.0)
        {
            return Error(context, "COMPONENT.INVALID_TIMESTEP", "Battery requires a positive timestep.");
        }

        var requestedChargeW = 0.0;
        if (context.InputStates.TryGetValue("charge", out var chargeRaw)
            && chargeRaw is ElectricalPowerState charge)
        {
            FiniteNumber.RequireNonNegative(charge.PowerW, nameof(charge.PowerW));
            requestedChargeW = charge.PowerW;
        }

        var requestedDischargeW = 0.0;
        if (context.InputStates.TryGetValue("discharge_request", out var dischargeRaw)
            && dischargeRaw is ElectricalPowerState dischargeRequest)
        {
            FiniteNumber.RequireNonNegative(dischargeRequest.PowerW, nameof(dischargeRequest.PowerW));
            requestedDischargeW = dischargeRequest.PowerW;
        }

        var chargePowerW = Math.Min(requestedChargeW, _parameters.MaximumChargePowerW);
        if (chargePowerW < requestedChargeW)
        {
            diagnostics.Add(Diagnostic(context, "BATTERY.CHARGE_POWER_LIMITED", DiagnosticSeverity.Information,
                "Charge power was limited to the configured maximum."));
        }

        var dischargePowerW = Math.Min(requestedDischargeW, _parameters.MaximumDischargePowerW);
        if (dischargePowerW < requestedDischargeW)
        {
            diagnostics.Add(Diagnostic(context, "BATTERY.DISCHARGE_POWER_LIMITED", DiagnosticSeverity.Information,
                "Discharge power was limited to the configured maximum."));
        }

        // Energy update: dE/dt = η_ch P_ch - P_dis/η_dis - P_self
        var energyRateW = _parameters.ChargeEfficiencyFraction * chargePowerW
            - dischargePowerW / _parameters.DischargeEfficiencyFraction
            - _parameters.SelfDischargePowerW;

        var proposedEnergyJ = _state.StoredEnergyJ + energyRateW * dt;
        var minEnergyJ = _parameters.MinimumSocFraction * _parameters.NominalCapacityJ;
        var maxEnergyJ = _parameters.MaximumSocFraction * _parameters.NominalCapacityJ;

        // Re-limit charge/discharge so SOC stays in bounds.
        if (proposedEnergyJ > maxEnergyJ)
        {
            var allowedIncreaseJ = Math.Max(0.0, maxEnergyJ - _state.StoredEnergyJ);
            var maxChargeEnergyInJ = allowedIncreaseJ / _parameters.ChargeEfficiencyFraction;
            var maxChargeW = maxChargeEnergyInJ / dt;
            var limitedChargeW = Math.Min(chargePowerW, Math.Max(0.0, maxChargeW));
            LastRejectedChargePowerW = requestedChargeW - limitedChargeW;
            chargePowerW = limitedChargeW;
            diagnostics.Add(Diagnostic(context, "BATTERY.AT_MAXIMUM_SOC", DiagnosticSeverity.Information,
                "Battery reached maximum SOC; excess charge was rejected."));
            proposedEnergyJ = _state.StoredEnergyJ
                + (_parameters.ChargeEfficiencyFraction * chargePowerW
                   - dischargePowerW / _parameters.DischargeEfficiencyFraction
                   - _parameters.SelfDischargePowerW) * dt;
            proposedEnergyJ = Math.Min(proposedEnergyJ, maxEnergyJ);
        }
        else
        {
            LastRejectedChargePowerW = requestedChargeW - chargePowerW;
        }

        if (proposedEnergyJ < minEnergyJ)
        {
            var allowedDecreaseJ = Math.Max(0.0, _state.StoredEnergyJ - minEnergyJ);
            // From E_n - (P_dis/η + P_self)*dt + η P_ch dt >= min
            var maxExtractionJ = allowedDecreaseJ
                + _parameters.ChargeEfficiencyFraction * chargePowerW * dt
                - _parameters.SelfDischargePowerW * dt;
            var maxDischargeW = Math.Max(0.0, maxExtractionJ * _parameters.DischargeEfficiencyFraction / dt);
            var limitedDischargeW = Math.Min(dischargePowerW, maxDischargeW);
            LastUnservedDischargePowerW = requestedDischargeW - limitedDischargeW;
            dischargePowerW = limitedDischargeW;
            diagnostics.Add(Diagnostic(context, "BATTERY.AT_MINIMUM_SOC", DiagnosticSeverity.Warning,
                "Battery reached minimum SOC; discharge request was curtailed."));
            proposedEnergyJ = _state.StoredEnergyJ
                + (_parameters.ChargeEfficiencyFraction * chargePowerW
                   - dischargePowerW / _parameters.DischargeEfficiencyFraction
                   - _parameters.SelfDischargePowerW) * dt;
            proposedEnergyJ = Math.Max(proposedEnergyJ, minEnergyJ);
        }
        else
        {
            LastUnservedDischargePowerW = requestedDischargeW - dischargePowerW;
        }

        proposedEnergyJ = Math.Clamp(proposedEnergyJ, minEnergyJ, maxEnergyJ);

        var proposedState = BatteryState.Create(
            storedEnergyJ: proposedEnergyJ,
            nominalCapacityJ: _parameters.NominalCapacityJ,
            batteryTemperatureK: _state.BatteryTemperatureK,
            cumulativeChargeEnergyJ: _state.CumulativeChargeEnergyJ + chargePowerW * dt,
            cumulativeDischargeEnergyJ: _state.CumulativeDischargeEnergyJ + dischargePowerW * dt);

        LastChargePowerW = chargePowerW;
        LastDischargePowerW = dischargePowerW;

        var storedEnergyChangeW = (proposedState.StoredEnergyJ - _state.StoredEnergyJ) / dt;
        // Electrical: charge is input energy, discharge is output energy; efficiency losses appear as output.
        var chargeLossW = (1.0 - _parameters.ChargeEfficiencyFraction) * chargePowerW;
        var dischargeLossW = dischargePowerW * (1.0 / _parameters.DischargeEfficiencyFraction - 1.0);
        var electricalInputW = chargePowerW;
        var electricalOutputW = dischargePowerW + chargeLossW + dischargeLossW + _parameters.SelfDischargePowerW;

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: 0.0,
            dryAirMassOutputKgPerSecond: 0.0,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: 0.0,
            waterMassOutputKgPerSecond: 0.0,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: electricalInputW,
            energyOutputW: electricalOutputW,
            storedEnergyChangeW: storedEnergyChangeW,
            timeStep: context.Simulation.TimeStep,
            electricalPowerInputW: electricalInputW,
            electricalPowerOutputW: electricalOutputW,
            storedElectricalPowerChangeW: storedEnergyChangeW);

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["discharge"] = new ElectricalPowerState { PowerW = dischargePowerW }
            },
            ProposedInternalState = proposedState,
            Balance = balance,
            Diagnostics = diagnostics
        };
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.ProposedInternalState is BatteryState proposed)
        {
            _state = proposed;
        }

        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;

    private SimulationDiagnostic Diagnostic(
        ComponentStepContext context,
        string code,
        DiagnosticSeverity severity,
        string message)
        => new()
        {
            Code = code,
            Severity = severity,
            Message = message,
            ComponentId = Id,
            StepIndex = context.Simulation.StepIndex,
            SimulationTime = context.Simulation.ElapsedTime
        };

    private ComponentStepResult Error(ComponentStepContext context, string code, string message)
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
                    StepIndex = context.Simulation.StepIndex,
                    SimulationTime = context.Simulation.ElapsedTime
                }
            ],
            Balance = ConservationBalance.Empty
        };
}
