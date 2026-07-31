# ThermoCore
## 25_SolarAirHeater.md

**Version:** 1.0  
**Status:** Implemented (MVP scaffold)  
**Document Type:** Second-application concept and topology  
**Date:** 2026-07-28  
**Related tasks:** APP2-001 … APP2-005  
**Project:** `ThermoCore.App2.SolarAirHeater`

---

# 1. Purpose

Prove that ThermoCore.Core is reusable beyond AWG without inventing new physics. APP2 is a **forced-air solar air heater**: ambient air is forced through a constant-efficiency solar collector and exhausted.

This is intentionally smaller than AWG (no adsorption, TEC, battery, or control mode machine).

---

# 2. System boundary

| Domain | In scope | Out of scope |
|---|---|---|
| Moist air | Ambient → fan → collector → exhaust | Building zone / duct networks |
| Solar | Constant POA irradiance on collector aperture | Tracking / IAM curves (use Core collector Level-1 η) |
| Electricity | Fan electrical power reported by Core fan | PV / battery / loads |
| Liquid water | — | DHW tanks, heat pumps |

Energy objective for sizing studies: maximize useful collector heat / temperature rise for a given aperture and flow.

---

# 3. Reused Core components

| Role | Component |
|---|---|
| Ambient boundary | `AmbientAirSourceComponent` |
| Forced flow | `PrescribedFlowFanComponent` |
| Solar boundary | `SolarRadiationSourceComponent` |
| Collector | `ConstantEfficiencySolarCollectorComponent` (`Q_u = η · G · A`) |
| Exhaust | `ExhaustAirSinkComponent` |
| Engine | `SimulationEngine` |

**Missing components for this MVP:** none. Later enhancements (optical absorption collector, weather-driven solar) already exist in Core and can replace Level-1 without new APP2 physics.

---

# 4. Topology

```text
ambient-source.outlet → fan.inlet
fan.outlet            → collector.inlet
solar-radiation.outlet → collector.solar
collector.outlet      → exhaust.inlet
```

---

# 5. Code entry points

| Type | Role |
|---|---|
| `SolarAirHeaterConfiguration` | Boundary + collector + fan parameters |
| `SolarAirHeaterGraphBuilder` | Builds `SimulationGraph` |
| `SolarAirHeaterSimulationRunner` | Runs short simulations and reports ΔT / useful heat |

---

# 6. Acceptance (MVP)

1. Graph builds from configuration using only Core components.
2. Short simulation succeeds with positive temperature rise under non-zero irradiance.
3. Solar utilization equals configured collector efficiency for the Level-1 model.
4. AWG assembly is **not** referenced (architecture boundary).

---

# 7. Sizing study (APP2-006)

`SolarAirHeaterSizingRunner` evaluates a Cartesian grid of aperture × mass flow × irradiance.

```bash
dotnet run --project src/ThermoCore.Console -- app2 --size
```

Acceptance: larger aperture at fixed η and G yields higher useful heat; higher flow lowers ΔT at the same Qu (Level-1 collector).

---

**End of Document**
