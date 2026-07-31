# ThermoCore
## 12_BatteryAndPowerManagement.md

**Version:** 1.1  
**Status:** Implemented  
**Document Type:** Engineering and implementation specification  
**Internal units:** SI  
**Primary implementation language:** C#

---


# 1. Purpose

This document defines battery state, photovoltaic power allocation and load management for ThermoCore.AWG.

The subsystem shall distinguish electrical generation, converter losses, battery charging, battery discharging, delivered load, curtailed generation and unserved demand.

# 2. Electrical topology

```text
Solar panel
   ↓
MPPT / DC-DC
   ↓
DC bus
   ├── Controller and sensors
   ├── Fans
   ├── Peltier driver
   └── Battery charge/discharge path
```

# 3. State

```csharp
public sealed record BatteryState
{
    public required double StoredEnergyJ { get; init; }
    public required double StateOfChargeFraction { get; init; }
    public required double BatteryTemperatureK { get; init; }
    public required double CumulativeChargeEnergyJ { get; init; }
    public required double CumulativeDischargeEnergyJ { get; init; }
}
```

# 4. Parameters

```csharp
public sealed record BatteryParameters
{
    public required double NominalCapacityJ { get; init; }
    public required double MinimumSocFraction { get; init; }
    public required double MaximumSocFraction { get; init; }
    public required double ChargeEfficiencyFraction { get; init; }
    public required double DischargeEfficiencyFraction { get; init; }
    public required double MaximumChargePowerW { get; init; }
    public required double MaximumDischargePowerW { get; init; }
    public required double SelfDischargePowerW { get; init; }
    public required double ConverterEfficiencyFraction { get; init; }
}
```

# 5. Battery balance

\[
\frac{dE_{bat}}{dt}
=
\eta_{ch}P_{ch}
-
\frac{P_{dis}}{\eta_{dis}}
-
P_{self}
\]

Discrete update:

\[
E_{n+1}
=
E_n+
\left(
\eta_{ch}P_{ch}
-
P_{dis}/\eta_{dis}
-
P_{self}
\right)\Delta t
\]

# 6. SOC

\[
SOC=E_{bat}/E_{capacity}
\]

Operational bounds:

\[
SOC_{min}\le SOC\le SOC_{max}
\]

Any rejected charging energy or unserved discharge request shall be explicit.

# 7. Load demand

```csharp
public sealed record ElectricalLoadDemand
{
    public required string LoadId { get; init; }
    public required double RequestedPowerW { get; init; }
    public required int Priority { get; init; }
    public required bool IsEssential { get; init; }
    public required double MinimumAcceptedPowerW { get; init; }
}
```

# 8. Allocation order

Recommended default:

1. controller and protection;
2. minimum safe airflow;
3. sensors and valves;
4. Peltier;
5. optional or auxiliary loads.

The ordering shall be configuration-driven, not hard-coded inside generic Core infrastructure.

# 9. Generation allocation

Let:

\[
P_{gen,net}=P_{pv}\eta_{mppt}
\]

First supply loads from current generation. Surplus may charge the battery. Deficit may be supplied by battery subject to SOC and discharge limits.

# 10. Curtailment

\[
P_{curtailed}
=
\max(0,P_{gen,net}-P_{load,served}-P_{charge,input})
\]

Curtailment shall be reported.

# 11. Unserved load

\[
P_{unserved}
=
P_{requested}-P_{served}
\]

Essential-load failure shall produce a critical diagnostic. Non-essential load shedding may be a normal operating event.

# 12. Peltier derating

The power manager may return an accepted Peltier power below request. It shall not directly calculate Peltier cooling. The Peltier component evaluates cooling from accepted electrical power.

# 13. Result

```csharp
public sealed record PowerManagementStepResult
{
    public required BatteryState ProposedBatteryState { get; init; }
    public required double GeneratedPowerW { get; init; }
    public required double ServedLoadPowerW { get; init; }
    public required double BatteryChargePowerW { get; init; }
    public required double BatteryDischargePowerW { get; init; }
    public required double CurtailedPowerW { get; init; }
    public required double UnservedPowerW { get; init; }
    public required IReadOnlyDictionary<string, double> DeliveredLoadPowerW { get; init; }
    public required ConservationBalance Balance { get; init; }
    public required IReadOnlyCollection<SimulationDiagnostic> Diagnostics { get; init; }
}
```

# 14. Power-allocation sequence

1. Validate generation, battery and load demand.
2. Reserve essential load.
3. Serve remaining loads by priority from generation.
4. Use allowed battery discharge for remaining demand.
5. Shed or derate unserved loads.
6. Charge battery from surplus generation.
7. Curtail remaining surplus.
8. Update stored energy.
9. Validate SOC and electrical balance.
10. Return proposed state.

# 15. Diagnostics

```text
BatteryAtMinimumSoc
BatteryAtMaximumSoc
ChargePowerLimited
DischargePowerLimited
EssentialLoadUnserved
LoadShed
PeltierDerated
SolarPowerCurtailed
ConverterLossSignificant
ElectricalBalanceResidualExceeded
```

# 16. Required tests

- PV-only operation;
- battery-only operation;
- simultaneous generation and charge;
- charge and discharge limits;
- minimum and maximum SOC;
- efficiency losses;
- load priorities;
- essential-load failure;
- Peltier derating;
- curtailment;
- timestep energy balance;
- deterministic execution.

# 17. Acceptance criteria

- SOC remains within configured limits;
- no electrical power is created;
- converter and battery losses are explicit;
- curtailed and unserved power are explicit;
- generic power management contains no psychrometric or AWG physics.

---

**End of Document**
