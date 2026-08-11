# R2-001 — TEC Manufacturer Profile Schema

Requirements:
COOL-003.

Add a provenance-aware model/profile for TEC hardware.

Minimum fields:

```text
Manufacturer
Model
Dimensions
Imax
Vmax
Qmax
DeltaTmax
Parameter/model type
Source identifier/revision
Validity range
Evidence level
```

Integrate with the existing analytical Peltier model without hard-coding a specific manufacturer.

Acceptance:
- existing generic parameters still work;
- at least one generic reference profile test exists;
- serialization is version-compatible;
- invalid profiles are rejected.
