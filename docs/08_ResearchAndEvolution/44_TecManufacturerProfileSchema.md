**Project:** ThermoCore  
**Track:** AWG Cooling Research & Evolution  
**Status:** Implemented (R2-001 / COOL-003)  
**Baseline:** Analytical Peltier model (`AnalyticalPeltierParameters`) already on `main`.

---

# TEC Manufacturer Profile Schema

## Purpose

Provide a provenance-aware hardware profile that can populate the existing analytical Peltier model **without hard-coding a manufacturer inside the physics component**.

## Types

| Type | Role |
|---|---|
| `TecManufacturerProfile` | Schema + validation + mapping |
| `TecParameterModelType` | `analyticalSteadyState` / `constantCop` / `performanceMap` |
| `TecEvidenceLevel` | `provisionalEngineering` … `calibrated` |
| `TecAnalyticalCoefficientSet` | Optional explicit α, R, K |
| `TecManufacturerProfileCatalog` | Built-in generic reference profiles |
| `TecManufacturerProfileSerializer` | Version-compatible JSON |

## Minimum fields (COOL-003)

```text
Manufacturer
Model
Dimensions (length/width/height mm)
Imax / Vmax / Qmax / DeltaTmax
Parameter/model type
Source identifier + revision
Validity range (T_cold,min / T_hot,max + hot-side reference)
Evidence level
Schema version
```

## Mapping rules

1. Existing `AnalyticalPeltierParameters.CreateProvisionalEngineeringDefaults()` remains valid and unchanged for callers that do not use profiles.
2. `profile.ToAnalyticalPeltierParameters()` builds analytical parameters:
   - Prefer `analyticalCoefficients` when present;
   - otherwise estimate α, R, K from datasheet ratings (Lineykin-style / `08_Peltier.md` §60) and record assumptions in `fittingMethod`.
3. Thermal interface resistances / dynamics default from provisional engineering defaults unless overridden by the caller via `thermalBoundaryDefaults`.

## Reference profile

```text
ProfileId: generic-tec1-12706
Manufacturer: Generic
Model: TEC1-12706
Evidence: provisionalEngineering
Sample JSON: samples/tec-profiles/generic-tec1-12706.json
```

This is **not** a manufacturer commitment. Replace with a datasheet-backed profile before predictive design.

## Serialization

- `schemaVersion` currently `"1.0"`
- camelCase JSON, string enums
- unknown members skipped (forward-compatible)
- unsupported `schemaVersion` rejected on validate/load
- invalid numeric / empty identity fields rejected

## Non-goals (later tasks)

- Wiring the profile into the AWG V3 graph / cooling plant (R4)
- Dew-point-tracking TEC controller (R2-002)
- Commercial black-box dehumidifier map (R3)
