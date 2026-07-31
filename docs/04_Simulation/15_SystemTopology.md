# ThermoCore
## 15_SystemTopology.md

**Version:** 1.1  
**Status:** Implemented  
**Document Type:** AWG V3 system topology specification  
**Applies To:** ThermoCore.AWG  
**Internal units:** SI  
**Related tasks:** AWG-002, DOC-015A, AWG-003+

---

# 1. Purpose

This document defines the complete system topology of the ThermoCore.AWG V3 reference application.

It describes:

- component graph;
- airflow paths;
- thermal paths;
- electrical paths;
- water paths;
- control connections;
- external boundaries;
- recirculation;
- measurement points;
- graph-construction rules.

The topology shall connect reusable components without embedding AWG-specific assumptions into ThermoCore.Core.

# 2. External system boundaries

Inputs:

```text
Ambient moist air
Solar radiation
Ambient temperature
Ambient pressure
Wind speed
Initial battery energy
Initial silica-gel loading
Initial component temperatures
```

Outputs:

```text
Exhaust moist air
Collected liquid water
Rejected heat
Curtailed solar power
Diagnostics
Simulation results
```

# 3. Primary airflow path

```text
Ambient Air Source
    ↓
Inlet Filter
    ↓
Peltier Hot-Side Air Heat Exchanger
    ↓
PV Rear-Air Channel
    ↓
Solar Air Collector
    ↓
Silica-Gel Bed
    ↓
Peltier-Cooled Condenser
    ↓
Heat-Recovery Hot Side
    ↓
Recirculation Splitter
    ├── Exhaust Sink
    └── Recirculation Return
```

Recirculated air returns through a mixer upstream of the selected preheating path.

# 4. Recommended mixer location

Recommended MVP:

```text
Ambient Air Source
        ┐
        ├── Fresh/Recirculated Air Mixer
Recirculation Return
        ┘
                ↓
Peltier Hot-Side Air Heat Exchanger
```

Alternative mixer positions may be studied later, but each topology variant shall have a distinct configuration identifier.

# 5. Physical-domain graphs

The AWG contains five connected graphs:

```text
Moist-air graph
Thermal graph
Electrical graph
Liquid-water graph
Control graph
```

They share component identities but shall use typed ports.

# 6. Moist-air graph

Nodes and connections:

```text
AmbientSource.MoistAirOut
    → FreshAirMixer.FreshIn

RecirculationSplitter.RecirculationOut
    → FreshAirMixer.RecirculationIn

FreshAirMixer.Out
    → PeltierHotSideHeatExchanger.AirIn

PeltierHotSideHeatExchanger.AirOut
    → SolarPanelRearChannel.AirIn

SolarPanelRearChannel.AirOut
    → SolarCollector.AirIn

SolarCollector.AirOut
    → SilicaGelBed.AirIn

SilicaGelBed.AirOut
    → Condenser.AirIn

Condenser.AirOut
    → HeatRecovery.HotIn

HeatRecovery.HotOut
    → RecirculationSplitter.In

RecirculationSplitter.ExhaustOut
    → ExhaustSink.In
```

The cold side of the heat recovery exchanger receives fresh or mixed inlet air according to configuration.

# 7. Thermal graph

Relevant heat paths:

```text
Solar radiation → Solar panel
Solar radiation → Solar collector
Peltier cold side ← Condenser heat load
Peltier hot side → Hot-side heat exchanger
PV panel → Rear airflow
Solar collector absorber → Process airflow
Silica-gel adsorption → Bed and airflow
Hot exhaust → Heat recovery → Cold inlet stream
All components → Ambient heat sink
```

# 8. Electrical graph

```text
SolarPanel.ElectricalOut
    → MPPT
    → DC Bus

Battery
    ↔ DC Bus

DC Bus
    ├── Controller load
    ├── Fan load
    ├── Peltier driver load
    └── Auxiliary loads
```

The power manager allocates accepted power. Components consume only delivered power.

# 9. Liquid-water graph

```text
Condenser.LiquidWaterOut
    → Drainage
    → WaterTank
```

Optional loss paths:

```text
Retained film
Carryover
Drain leakage
Tank overflow
```

Every loss path shall be explicit.

# 10. Control graph

```text
Sensors / Simulation observations
    → AWG Controller
    → Fan control
    → Peltier control
    → Recirculation splitter control
    → Heat-recovery bypass
    → Operating-mode request
```

Control signals are not physical energy or mass flows.

# 11. Component identifiers

Recommended stable IDs:

```text
ambient-source
fresh-air-mixer
peltier-hot-side-hx
pv-panel
pv-rear-channel
solar-collector
silica-gel-bed
condenser
heat-recovery
recirculation-splitter
exhaust-sink
water-tank
battery
power-manager
process-fan
awg-controller
```

# 12. Configuration model

```csharp
public sealed record AwgV3TopologyConfiguration
{
    public required bool EnableRecirculation { get; init; }

    public required bool EnableHeatRecovery { get; init; }

    public required bool EnablePvRearAirChannel { get; init; }

    public required double InitialRecirculationFraction { get; init; }

    public required string HeatRecoveryColdSideSource { get; init; }

    public required IReadOnlyDictionary<string, string> ComponentModelSelections { get; init; }
}
```

# 13. Graph-builder interface

```csharp
public interface IAwgSystemGraphBuilder
{
    SimulationGraph Build(
        AwgSystemConfiguration configuration,
        AwgInitialState initialState);
}
```

# 14. Validation rules

The graph builder shall reject:

- missing required component;
- duplicate component ID;
- incompatible port domains;
- missing required connection;
- multiple sources driving a single non-mixer inlet;
- liquid-water output without sink;
- electrical load without power source;
- recirculation enabled without loop solver;
- heat recovery enabled without both streams;
- impossible control connection;
- component model missing required parameters.

# 15. Optional topology variants

Supported later:

```text
No silica gel, direct condensation only
Dual silica-gel beds
Parallel adsorption and regeneration beds
Two-stage Peltier
External thermal storage
No battery
Two fans
Latent heat recovery
Closed-loop laboratory test mode
```

Each variant shall have a unique topology ID and validation rules.

# 16. Dual-bed extension

A future dual-bed configuration may allow one bed to adsorb while the other regenerates.

Additional requirements:

- switching valves;
- two bed states;
- crossover prevention;
- synchronized control;
- duplicated pressure-drop paths;
- separate thermal states.

This is outside MVP but must not require redesign of Core graph abstractions.

# 17. Measurement points

Recommended virtual or real sensor locations:

```text
MP-01 Ambient inlet
MP-02 Mixed inlet
MP-03 After Peltier hot side
MP-04 After PV rear channel
MP-05 Solar collector outlet
MP-06 Silica-gel outlet
MP-07 Condenser inlet
MP-08 Condenser outlet
MP-09 Heat-recovery hot outlet
MP-10 Exhaust
MP-11 Recirculation return
MP-12 Peltier hot ceramic
MP-13 Peltier cold ceramic
MP-14 Condenser surface
MP-15 Battery
MP-16 Water tank
```

# 18. Result channels

For each moist-air measurement point:

```text
Temperature
Pressure
Humidity ratio
Relative humidity
Dew point
Dry-air mass flow
Water-vapor mass flow
Enthalpy
```

For components:

```text
Power
Heat flow
Pressure drop
Stored energy
Stored water
Diagnostics
Residuals
```

# 19. Execution ordering

For an acyclic approximation:

1. environment and weather;
2. control request based on previous committed state;
3. power generation and allocation;
4. fan/airflow provisional state;
5. Peltier;
6. upstream heat exchangers;
7. solar collector;
8. silica gel;
9. condenser;
10. heat recovery;
11. splitter and mixer;
12. balance validation;
13. commit.

With recirculation, the moist-air loop requires iteration before commit.

# 20. Recirculation loop variables

Suggested loop convergence variables:

```text
Mixed inlet temperature
Mixed inlet humidity ratio
Recirculated dry-air mass flow
Recirculated enthalpy flow
Fan operating flow
```

# 21. Initial state

Required initial values:

```text
Battery stored energy
Silica-gel water loading
Silica-gel temperature
Peltier hot and cold temperatures
Solar collector absorber temperature
PV panel temperature
Water-tank content
Controller state
Recirculation state
```

No hidden default shall be used for critical physical states.

# 22. System-level balances

Water:

\[
m_{water,ambient}
+
m_{water,initial\ storage}
=
m_{water,exhaust}
+
m_{water,collected}
+
m_{water,final\ storage}
+
R_w
\]

Energy:

\[
E_{solar}
+
E_{initial\ storage}
=
E_{electrical\ losses}
+
E_{thermal\ exhaust}
+
E_{ambient\ losses}
+
E_{final\ storage}
+
R_E
\]

Dry air:

\[
m_{da,fresh}=m_{da,exhaust}
\]

for a steady inventory over the evaluated interval.

# 23. Topology metadata

Every simulation result shall store:

```text
Topology ID
Topology version
Component model IDs
Parameter-set IDs
Enabled optional paths
Graph hash or equivalent reproducibility identifier
```

# 24. Required tests

- complete graph builds;
- every required port connected;
- invalid domain connection rejected;
- recirculation disabled topology;
- recirculation enabled topology;
- heat recovery bypass;
- no-battery variant;
- missing sink rejection;
- duplicate ID rejection;
- full system dry-air path;
- water path to tank;
- electrical load allocation path;
- deterministic graph construction.

# 25. Acceptance criteria

The topology is accepted when:

1. every physical transfer has a typed connection;
2. every external boundary is explicit;
3. no component directly mutates another component;
4. recirculation uses the cyclic solver;
5. system metadata is sufficient for reproducibility;
6. optional variants do not require changes to ThermoCore.Core.

# 26. Core component type mapping (V3 MVP)

| Topology ID | Recommended Core type | Notes |
|---|---|---|
| `ambient-source` | `AmbientAirSourceComponent` | Boundary moist-air inlet |
| `fresh-air-mixer` | `MoistAirMixerComponent` | Fresh + recirculation |
| `peltier-hot-side-hx` | Pass-through or dedicated HX when available; MVP may use `SensibleHeaterComponent` / Peltier air ports | Hot-side rejection into process air |
| `pv-panel` | `DynamicElectrothermalSolarPanelComponent` or `TemperatureCorrectedSolarPanelComponent` | Electrical + thermal |
| `pv-rear-channel` | Rear-air ports on dynamic PV model | Optional via configuration |
| `solar-collector` | `DynamicLumpedSolarCollectorComponent` | Absorber + process air |
| `silica-gel-bed` | `SilicaGelBedComponent` | Adsorption / desorption |
| `condenser` | `CondenserComponent` | Liquid water + moist air |
| `heat-recovery` | `SensibleHeatRecoveryComponent` | Optional bypass |
| `recirculation-splitter` | `MoistAirSplitterComponent` | Exhaust vs recirculation |
| `exhaust-sink` | `ExhaustAirSinkComponent` | Moist-air boundary |
| `water-tank` | `LiquidWaterSinkComponent` (MVP inventory) | Explicit water path |
| `battery` | `BatteryStorageComponent` | Electrical storage |
| `power-manager` | `PowerManagementComponent` | Allocation |
| `process-fan` | `CurveBasedFanComponent` or `PrescribedFlowFanComponent` | Airflow driver |
| `awg-controller` | AWG control module (not a physical component) | Issues control requests |

Stable topology ID: `awg-v3-mvp`.

# 27. Port and connection conventions

- Moist-air ports use `inlet` / `outlet` unless a multi-port component requires named sides (`hotIn`, `hotOut`, `coldIn`, `coldOut`, `freshIn`, `recirculationIn`, `exhaustOut`).
- Electrical ports use `electricalIn` / `electricalOut` or domain-specific names already defined by the Core component.
- Liquid-water ports use `liquidOut` / `inlet` consistent with `CondenserComponent` and sinks.
- Connection IDs shall be deterministic: `{sourceId}.{sourcePort}->{targetId}.{targetPort}`.
- Graph hash input shall include topology ID, version, component model selections and sorted connection IDs.

# 28. Graph-builder coding order

1. Configuration and initial-state records (`AwgV3TopologyConfiguration`, `AwgSystemConfiguration`, `AwgInitialState`).
2. Component factory selecting Core implementations from model IDs.
3. Connection builder for moist-air, thermal, electrical and liquid-water graphs.
4. Validation rules from §14.
5. Acyclic build path with recirculation disabled.
6. Cyclic build path with recirculation enabled (requires GRAPH cyclic solver).
7. Topology metadata on every `SimulationResult`.

Recommended AWG layout:

```text
ThermoCore.AWG/
  Topology/
    AwgV3TopologyIds.cs
    AwgSystemConfiguration.cs
    AwgInitialState.cs
    AwgV3TopologyConfiguration.cs
    IAwgSystemGraphBuilder.cs
    AwgV3SystemGraphBuilder.cs
    AwgTopologyValidation.cs
```

---

**End of Document**
