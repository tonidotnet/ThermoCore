# ThermoCore
## 14_ControlSystem.md

**Version:** 1.1  
**Status:** Implemented  
**Document Type:** AWG control architecture and state-machine specification  
**Applies To:** ThermoCore.AWG  
**Internal units:** SI  
**Primary implementation language:** C#
**Related tasks:** AWG-001, DOC-014A, AWG-008+

---

# 1. Purpose

This document defines the supervisory control system for ThermoCore.AWG.

The controller coordinates:

- adsorption;
- regeneration;
- condensation;
- recirculation;
- Peltier power;
- fan operating points;
- battery constraints;
- thermal protection;
- water-tank limits;
- fault handling;
- safe shutdown.

The controller shall not duplicate component physics. It shall select operating modes and issue control requests based on measured or simulated state.

# 2. Architectural placement

Recommended namespace:

```csharp
ThermoCore.AWG.Control
```

The controller may depend on ThermoCore.Core abstractions and AWG component contracts.

It shall not depend on:

- Blazor;
- ASP.NET Core;
- WPF;
- database entities;
- UI events.

# 3. Control principles

The controller shall:

1. use explicit operating states;
2. separate requested control from actual delivered component behavior;
3. obey component diagnostics and physical limits;
4. preserve deterministic execution;
5. record every mode transition;
6. fail safe on missing critical data;
7. avoid oscillating rapidly between modes;
8. expose the reason for each control decision.

# 4. Operating states

```csharp
public enum AwgOperatingMode
{
    Off,
    Startup,
    Adsorption,
    Regeneration,
    Condensation,
    HeatRecovery,
    Recirculation,
    Standby,
    ControlledShutdown,
    Fault
}
```

A concrete implementation may combine some transient states, but the logical behavior shall remain traceable.

# 5. State meanings

## Off

No active loads. Passive thermal conduction and self-discharge may continue in the simulation.

## Startup

Validate configuration, sensors, airflow availability, battery state and component readiness.

## Adsorption

Ambient or conditioned air passes through the silica-gel bed to increase adsorbent loading.

## Regeneration

Heated air lowers equilibrium loading and releases stored water vapor.

## Condensation

The condenser and Peltier are enabled to convert part of the humid air stream into liquid water.

## HeatRecovery

Heat exchanger bypass and routing are selected to recover useful sensible heat.

## Recirculation

A configured fraction of post-condenser or exhaust air is mixed back into the inlet path.

## Standby

The system waits while preserving essential monitoring.

## ControlledShutdown

Active loads are ramped down in a safe order.

## Fault

Protection state entered after a critical diagnostic or invalid system state.

# 6. State transition inputs

The controller may use:

```text
Ambient temperature
Ambient relative humidity
Solar irradiance
Battery SOC
Available electrical power
Silica-gel loading
Silica-gel temperature
Condenser surface temperature
Inlet and outlet dew points
Peltier hot-side temperature
Peltier cold-side temperature
Airflow
Pressure drop
Water-tank level
Component diagnostics
Simulation time
Previous control state
```

# 7. Control request model

```csharp
public sealed record AwgControlRequest
{
    public required AwgOperatingMode RequestedMode { get; init; }

    public required double FanControlFraction { get; init; }

    public required double PeltierPowerRequestW { get; init; }

    public required double RecirculationFraction { get; init; }

    public required bool HeatRecoveryBypassOpen { get; init; }

    public required bool AdsorptionBedEnabled { get; init; }

    public required bool RegenerationHeatEnabled { get; init; }

    public required bool CondenserEnabled { get; init; }

    public required string ReasonCode { get; init; }
}
```

# 8. Controller state

```csharp
public sealed record AwgControllerState
{
    public required AwgOperatingMode CurrentMode { get; init; }

    public required TimeSpan TimeInCurrentMode { get; init; }

    public required DateTimeOffset? LastModeChangeUtc { get; init; }

    public required int ConsecutiveFaultCount { get; init; }

    public required bool IsLatchedFault { get; init; }

    public required string LastTransitionReasonCode { get; init; }
}
```

Simulation-time-only implementations may omit real timestamps and use simulation time instead.

# 9. Adsorption entry conditions

Adsorption may start when:

- bed loading is below adsorption target;
- ambient vapor pressure is sufficient;
- inlet conditions are inside calibrated range;
- minimum airflow is available;
- battery can supply essential fan power;
- no critical fault exists.

A simple driving-force condition:

\[
X_{eq,ads}(T_{bed},p_v) - X > \Delta X_{min}
\]

# 10. Adsorption exit conditions

Exit adsorption when any condition is met:

- loading target reached;
- adsorption rate falls below threshold;
- maximum adsorption duration reached;
- bed temperature exceeds limit;
- battery reaches reserve SOC;
- airflow is insufficient;
- regeneration opportunity has higher configured priority.

# 11. Regeneration entry conditions

Regeneration may start when:

- bed loading exceeds regeneration threshold;
- available solar or recovered heat is sufficient;
- airflow can carry desorbed vapor;
- condenser path is available;
- battery or PV can support fan and control loads;
- no thermal-protection lockout exists.

Thermal availability shall be evaluated from actual energy supply, not from collector temperature alone.

# 12. Regeneration exit conditions

Exit regeneration when:

- bed reaches regenerated target loading;
- water-transfer rate falls below threshold;
- available heat becomes insufficient;
- bed or collector temperature exceeds limit;
- condenser cannot process the released vapor;
- battery reaches reserve limit;
- maximum regeneration duration reached.

# 13. Condensation control

The condenser shall be enabled only if:

\[
T_{surface} < T_{dp,in} - \Delta T_{margin}
\]

and available cooling power is positive.

The controller may request a target Peltier power or target surface temperature, but actual cooling remains the responsibility of the Peltier and condenser models.

# 14. Peltier control strategies

Supported strategies:

```text
FixedPower
MaximumAvailablePower
TargetColdSideTemperature
TargetDewPointApproach
MinimumWhPerLiter
ThermalProtectionLimited
```

Recommended MVP strategy:

```text
TargetDewPointApproach with electrical and hot-side limits
```

Target:

\[
T_{surface,target}
=
T_{dp,in}
-
\Delta T_{approach,target}
\]

COOL-004 / R2-002 additions:

```text
Configurable TargetDewPointApproachK (margin)
MinimumPeltierPowerRequestW / MaximumPeltierPowerRequestW
Optional MaximumPeltierCurrentA × TecOperatingVoltageV electrical cap
PeltierPowerRampLimitWPerSecond (anti-chatter; +∞ disables)
MinimumCondenserSurfaceTemperatureK
HoldPowerFractionWhenAtOrBelowTarget
Diagnostics: CTRL.PELTIER_POWER_SATURATED, CTRL.PELTIER_TARGET_UNREACHABLE,
             CTRL.PELTIER_RAMP_LIMIT, CTRL.PELTIER_HOT_SIDE_LIMIT
```

Higher inlet dew point reduces the required temperature lift and therefore the proportional drive
for the same observed surface temperature (optimize water/energy, not minimum cold temperature).

# 15. Fan control strategies

Supported modes:

```text
FixedControlFraction
FixedDryAirMassFlow
FixedVolumetricFlow
PressureControlled
OptimizationControlled
```

Minimum safe airflow shall take precedence over water-production optimization.

# 16. Recirculation control

The controller may vary:

\[
0 \le r \le r_{max}
\]

Recirculation should increase only when it improves one or more selected objectives without causing:

- excessive inlet temperature;
- reduced desorption driving force;
- unstable loop convergence;
- oxygen or contamination concerns in future real hardware;
- excessive humidity accumulation;
- fan operating-point failure.

# 17. Battery protection

Required thresholds:

```text
Critical minimum SOC
Reserve SOC
Normal operating SOC
Maximum SOC
```

At reserve SOC:

- disable optional loads;
- derate Peltier;
- maintain minimum safe airflow;
- preserve controller operation.

At critical minimum SOC:

- enter controlled shutdown or fault.

# 18. Thermal protection

Critical limits include:

```text
Peltier hot side
Solar collector absorber
Battery temperature
Silica-gel bed
Condenser minimum temperature
Electronics enclosure
```

Protection order:

1. reduce Peltier power;
2. increase safe hot-side airflow if available;
3. bypass heat recovery if it worsens hot-side rejection;
4. disable regeneration heat;
5. enter controlled shutdown;
6. latch fault if temperature remains unsafe.

# 19. Water-tank protection

When tank level reaches configured capacity:

- disable condensation or divert drainage;
- preserve safe airflow;
- report `WaterTankFull`;
- prevent untracked liquid-water loss.

# 20. Anti-chatter rules

Every state transition should support:

- minimum dwell time;
- separate entry and exit thresholds;
- transition debounce count;
- optional cooldown time.

Example hysteresis:

```text
Enter regeneration at loading >= 0.20 kg/kg
Exit regeneration at loading <= 0.08 kg/kg
```

# 21. Fault model

```csharp
public enum AwgFaultCode
{
    None,
    ConfigurationInvalid,
    CriticalSensorUnavailable,
    FanOperatingPointUnavailable,
    PeltierHotSideOverTemperature,
    BatteryBelowCriticalSoc,
    WaterTankFull,
    SimulationNonConvergent,
    EnergyBalanceInvalid,
    WaterBalanceInvalid,
    ComponentCriticalDiagnostic
}
```

# 22. Fault latching

Faults may be:

```text
Transient
Recoverable
Latched
```

Latched faults require explicit reset after the triggering condition is cleared.

Simulation mode may support automatic reset only when configured.

# 23. Decision sequence per timestep

1. Read committed system state.
2. Read current environment and weather.
3. Aggregate critical diagnostics.
4. Apply safety protections.
5. Determine available electrical and thermal resources.
6. Evaluate current mode exit conditions.
7. Evaluate candidate entry conditions.
8. Apply dwell and hysteresis rules.
9. Select requested mode.
10. Calculate fan, Peltier, bypass and recirculation requests.
11. Return proposed controller state and control request.
12. Commit only after the simulation step is accepted.

# 24. Controller interface

```csharp
public interface IAwgController
{
    AwgControlStepResult Evaluate(
        AwgSystemObservation observation,
        AwgControllerState currentState,
        AwgControlParameters parameters,
        TimeSpan timeStep);
}
```

# 25. Result model

```csharp
public sealed record AwgControlStepResult
{
    public required AwgControlRequest Request { get; init; }

    public required AwgControllerState ProposedState { get; init; }

    public required IReadOnlyCollection<SimulationDiagnostic> Diagnostics { get; init; }

    public required IReadOnlyCollection<AwgDecisionTraceEntry> DecisionTrace { get; init; }
}
```

# 26. Decision trace

Every transition or protection action shall record:

```text
Input values used
Thresholds
Previous mode
Requested mode
Reason code
Active limiting constraint
```

# 27. Optimization objectives

The controller may later optimize:

```text
Maximum liters/day
Minimum Wh/liter
Maximum water recovery fraction
Minimum battery cycling
Minimum thermal stress
Maximum use of current solar surplus
```

MVP control shall remain rule-based and deterministic.

# 28. Required unit tests

- startup validation;
- adsorption entry and exit;
- regeneration entry and exit;
- condensation dew-point margin;
- battery derating;
- thermal protection;
- minimum dwell time;
- hysteresis;
- recirculation bounds;
- water-tank full;
- transient and latched faults;
- deterministic decision trace.

# 29. Integration tests

- controller with battery and Peltier;
- controller with silica-gel state;
- controller with fan operating-point failure;
- 24-hour weather-driven mode sequence;
- recirculation loop convergence failure;
- controlled shutdown;
- balance-invalid system response.

# 30. Acceptance criteria

The control system is accepted when:

1. all critical limits override optimization;
2. state transitions are deterministic;
3. no mode changes without a recorded reason;
4. requested power never exceeds reported availability without explicit unserved demand;
5. controller logic contains no duplicated component equations;
6. anti-chatter behavior is tested;
7. failure states are explicit and auditable.

# 31. Observation and parameter contracts

These contracts complete the controller interface in §24. Implementations shall live under `ThermoCore.AWG.Control` and must not embed component physics.

```csharp
public sealed record AwgSystemObservation
{
    public required DateTimeOffset SimulationTimeUtc { get; init; }

    public required double AmbientTemperatureK { get; init; }

    public required double AmbientRelativeHumidityFraction { get; init; }

    public required double AmbientVaporPressurePa { get; init; }

    public required double SolarIrradianceWPerSquareMeter { get; init; }

    public required double BatteryStateOfChargeFraction { get; init; }

    public required double AvailableElectricalPowerW { get; init; }

    public required double SilicaGelLoadingKgPerKg { get; init; }

    public required double SilicaGelTemperatureK { get; init; }

    public required double CondenserSurfaceTemperatureK { get; init; }

    public required double InletDewPointTemperatureK { get; init; }

    public required double CondenserInletDewPointTemperatureK { get; init; }

    public required double PeltierHotSideTemperatureK { get; init; }

    public required double PeltierColdSideTemperatureK { get; init; }

    public required double ProcessDryAirMassFlowKgPerSecond { get; init; }

    public required double WaterTankLevelFraction { get; init; }

    public required bool FanOperatingPointValid { get; init; }

    public required IReadOnlyCollection<SimulationDiagnostic> ComponentDiagnostics { get; init; }
}

public sealed record AwgControlParameters
{
    public required double AdsorptionTargetLoadingKgPerKg { get; init; }

    public required double RegenerationEntryLoadingKgPerKg { get; init; }

    public required double RegenerationExitLoadingKgPerKg { get; init; }

    public required double MinimumAdsorptionDrivingForceKgPerKg { get; init; }

    public required double CondensationDewPointMarginK { get; init; }

    public required double TargetDewPointApproachK { get; init; }

    public required double MaximumRecirculationFraction { get; init; }

    public required double ReserveBatterySocFraction { get; init; }

    public required double CriticalBatterySocFraction { get; init; }

    public required TimeSpan MinimumModeDwell { get; init; }

    public required double PeltierHotSideLimitK { get; init; }

    public required double SilicaGelTemperatureLimitK { get; init; }

    public required double CollectorAbsorberTemperatureLimitK { get; init; }

    public required double MinimumSafeDryAirMassFlowKgPerSecond { get; init; }
}

public sealed record AwgDecisionTraceEntry
{
    public required string ReasonCode { get; init; }

    public required string PreviousMode { get; init; }

    public required string RequestedMode { get; init; }

    public required string ActiveLimitingConstraint { get; init; }

    public required IReadOnlyDictionary<string, double> ScalarInputs { get; init; }
}
```

# 32. Implementation mapping

| Control concern | Consumes / observes | Issues request to |
|---|---|---|
| Adsorption / regeneration mode | `SilicaGelBedComponent` loading and temperature | Bed enable + regeneration heat enable flags |
| Condensation enable | `CondenserComponent` surface and inlet dew point | `CondenserEnabled`, Peltier power request |
| Peltier power | `AnalyticalPeltierComponent` / `ConstantCopPeltierComponent` temperatures and available bus power | `PeltierPowerRequestW` |
| Fan airflow | `CurveBasedFanComponent` / `PrescribedFlowFanComponent` operating point | `FanControlFraction` |
| Recirculation | `MoistAirMixerComponent` + `MoistAirSplitterComponent` | `RecirculationFraction` |
| Heat-recovery bypass | `SensibleHeatRecoveryComponent` | `HeatRecoveryBypassOpen` |
| Battery protection | `BatteryStorageComponent`, `PowerManagementComponent` | Load derating via power request limits |
| Water-tank full | Liquid-water tank / sink inventory | Disable condensation / drainage diversion |

Recommended AWG layout:

```text
ThermoCore.AWG/
  Control/
    IAwgController.cs
    RuleBasedAwgController.cs
    AwgOperatingMode.cs
    AwgControlRequest.cs
    AwgControllerState.cs
    AwgSystemObservation.cs
    AwgControlParameters.cs
    AwgControlStepResult.cs
    AwgFaultCode.cs
```

# 33. MVP coding order

1. Enums, records and `IAwgController` contracts (no physics).
2. Pure rule-based controller with injectable thresholds.
3. Unit tests from §28 using synthetic observations.
4. Wire controller into AWG graph builder after AWG-003.
5. Integration tests from §29 after topology assembly.

Controller evaluation shall be deterministic for identical observation, state and parameters.

---

**End of Document**
