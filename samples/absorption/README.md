# Absorption research samples (R7-001 / COOL-008)

Research-only performance-map screen — **not** a production AWG cooling plant.

| File | Role |
|---|---|
| `generic-solar-thermal-screen.r7-001.json` | Synthetic H2O/LiBr-style feasibility grid (8 points) |

```csharp
var map = AbsorptionPerformanceMapSerializer.LoadFromFile(
    "samples/absorption/generic-solar-thermal-screen.r7-001.json");
var result = new AbsorptionCoolingResearchModel(map).Evaluate(tg, ts, te);
// result.Diagnostics contains ABSORPTION.RESEARCH_ONLY
```

`CoolingTechnology.AbsorptionResearch` remains rejected by `CoolingPlantFactory` (COOL-008).
