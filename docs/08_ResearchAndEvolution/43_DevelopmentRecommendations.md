**Project:** ThermoCore  
**Track:** AWG Cooling Research & Evolution  
**Status:** Proposed / Ready for implementation planning  
**Baseline:** Existing ThermoCore `main` with Core, AWG, API/Web, calibration/optimization and SolarAirHeater app already implemented.

---

# Development Recommendations

## R0 — Freeze baseline

Before implementation:

1. run current AWG regression suite;
2. archive/reference baseline result summaries;
3. confirm existing config files remain valid.

## R1 — Cooling metrics

Implement:

```text
L/kWh_electric
L/kWh_solar_primary
L/day/m²
CoolingPlantCOP
AverageDewPointMargin
WaterRecoveryFraction
```

## R2 — TEC characterization

Add:

- manufacturer TEC profile schema;
- dew-point tracking;
- system-level fan/heat-exchanger accounting;
- commercial Peltier black-box profile.

## R3 — Peltier demonstrator validation

Use the existing calibration pipeline and add only missing hardware metadata.

## R4 — Cooling abstraction

Add AWG-level cooling plant selection with backward-compatible thermoelectric adapter.

## R5 — Vapor compression

Add performance-map model and validate one real small DC module.

## R6 — Hybrid comparison

Run:

```text
direct TEC
heating-only control
sorbent + TEC
direct compressor
sorbent + compressor
```

with common metrics.

## R7 — Absorption feasibility

Research-only map scaffold delivered in R7-001; keep out of production plant selection (COOL-008)
until TEC/compressor baselines and a real device dataset justify more.

## First implementation issues

```text
R1-001 Add water/energy comparison KPIs
R1-002 Add cooling COP and dew-point-margin result channels
R2-001 Add manufacturer TEC profile model
R2-002 Add dew-point-tracking TEC controller
R3-001 Add prototype hardware metadata and CSV profile
R4-001 Add backward-compatible cooling technology selector
R5-001 Add vapor-compression performance-map contract
```
