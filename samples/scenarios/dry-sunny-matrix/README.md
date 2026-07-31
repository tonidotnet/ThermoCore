# Dry sunny scenario matrix

AWG regression pack: **dry air + strong sunshine**, ample battery, temperature × silica-gel mass.

| Axis | Values |
|---|---|
| Ambient temperature | 10, 15, 20, 25, 30, 35 °C |
| Relative humidity | 30% |
| Solar irradiance | 950 W/m² |
| Battery SOC | 90% |
| Silica gel dry mass | 1, 2, 3, 4, 5 kg |

**30 scenarios** (6 × 5). Kept out of the default Level-5 pack so `regress` stays fast.

```bash
dotnet run --project src/ThermoCore.Console -- regress --dir samples/scenarios/dry-sunny-matrix
```

## Results diagram

Simulated matrix charts and table:

- [`RESULTS.md`](RESULTS.md) — heatmaps + Mermaid L/day curves + full table  
- [`results-heatmap.svg`](results-heatmap.svg) — L/day heatmap (temperature × silica)  
- [`results-heatmap-water-kg.svg`](results-heatmap-water-kg.svg) — 30 s collected water (kg)  
- [`results.csv`](results.csv) — raw metrics  

Schema field: `silicaGelDryAdsorbentMassKg` (see `AwgRegressionScenario`).  
Catalog factory: `AwgRegressionScenarioCatalog.CreateDrySunnyMatrixScenarios()`.
