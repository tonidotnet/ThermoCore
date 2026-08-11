# R1-001 — Add Water/Energy Comparison KPIs

Requirements:
KPI-001, KPI-002, KPI-003, KPI-004.

Add result/summary support for:

```text
L/kWh_electric
L/kWh_solar_primary
L/day/m² solar aperture
WaterRecoveryFraction
DesorptionCaptureFraction where applicable
```

Rules:
- do not double-count recovered internal heat;
- preserve old summary fields;
- define zero-denominator behavior explicitly;
- add tests;
- expose through Console/API/Web only where the existing result architecture naturally supports it.

Acceptance:
- old regressions unchanged except additive fields;
- unit tests cover denominators and edge cases;
- metrics are documented.
