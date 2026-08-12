**Project:** ThermoCore  
**Track:** AWG Cooling Research & Evolution  
**Status:** Proposed / Ready for implementation planning  
**Baseline:** Existing ThermoCore `main` with Core, AWG, API/Web, calibration/optimization and SolarAirHeater app already implemented.

---

# Hybrid Sorbent + Active Cooling Architecture

## Reference variants

### A — Direct TEC

```text
ambient → TEC cooling → water
```

### B — Heating-only control

```text
ambient → solar heating → TEC cooling
```

Heating alone does not increase humidity ratio or dew point and therefore acts as a scientific control.

### C — Sorbent + TEC

```text
ambient adsorption
→ solar regeneration
→ high-dew-point stream
→ TEC condensation
```

### D — Direct compressor

```text
ambient → vapor-compression cooling → water
```

### E — Sorbent + compressor

```text
ambient adsorption
→ solar regeneration
→ high-dew-point stream
→ vapor-compression condensation
```

## Main hypothesis

The sorbent's value is temporal and spatial concentration of water vapor.

The chain to validate is:

```text
higher regeneration-stream humidity ratio
→ higher dew point
→ lower required cooling temperature lift
→ changed cooling COP
→ changed total water/energy performance
```

## Required cycle water accounting

```text
adsorbed
desorbed
condensed
exhausted
retained in sorbent
```

The hybrid is justified only if total-system performance improves under fair solar-resource accounting.

## Software comparison suite (R6-001)

`ThermoCore.AWG/Hybrid/` runs variants A–E through the shared cooling-plant contract
(`HybridComparisonRunner`) with common KPIs (condensed water, exhausted vapor, desorbed
delta, L/kWh_electric, plant COP, dew-point margin). Regeneration streams are approximate
high-dew-point boundary conditions for early comparison; full AWG adsorption cycle coupling
remains available via existing V3 topology.
