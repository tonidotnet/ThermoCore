**Project:** ThermoCore  
**Track:** AWG Cooling Research & Evolution  
**Status:** Proposed / Ready for implementation planning  
**Baseline:** Existing ThermoCore `main` with Core, AWG, API/Web, calibration/optimization and SolarAirHeater app already implemented.

---

# Gap Analysis

## Already available

Do not reimplement:

- generic psychrometrics;
- general numerical solver infrastructure;
- graph simulation;
- cyclic loop handling;
- base Peltier physics;
- condenser physics;
- sorbent dynamics;
- weather support;
- calibration framework;
- optimization framework;
- Web/API foundations.

## New gaps

### Cooling orchestration

The physical Peltier component exists, but AWG needs a higher-level cooling-plant selection layer.

### TEC hardware profiles

Need named, provenance-aware profiles:

```text
Manufacturer
Model
Dimensions
Imax
Vmax
Qmax
DeltaTmax
Analytical coefficients and/or map
Source revision
Validity range
```

### Dew-point tracking

Desired control target:

```text
Tsurface,target = Tdewpoint,in - margin
```

rather than minimum possible surface temperature.

### Real thermal resistances

Bench data must identify:

- hot-side thermal resistance;
- cold-side thermal resistance;
- fan power;
- airflow;
- condensation-film effects.

### Commercial demonstrator model

A ready-made Peltier dehumidifier should be representable as an empirical black-box baseline.

### Vapor compression

Need an initial map-based refrigeration model before considering a full refrigerant property solver.

### Fair energy accounting

Solar thermal energy must be included when comparing sorbent hybrid systems against direct electric cooling.
