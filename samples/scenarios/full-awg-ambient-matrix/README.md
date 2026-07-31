# Full AWG ambient matrix (T × RH)

Controlled Full AWG V3 (Adsorption ↔ Regeneration), electrical subsystem, **no heat recovery** (HR tear is unstable with collector gating). Fixed process air **0.02 kg/s**, silica **2 kg** (regenerated start), G **950 W/m²**, battery SOC **90%**, duration **2 h**.

| Axis | Values |
|---|---|
| Inlet temperature | 20, 25, 30, 35 °C |
| Relative humidity | 30, 35, 40, 45, 50, 60 % |

**24 scenarios** (4 × 6). Supervisory `RuleBasedAwgController` is enabled.

```bash
dotnet run --project src/ThermoCore.Console -- full-flow-ambient-matrix
dotnet run --project src/ThermoCore.Console -- regress --dir samples/scenarios/full-awg-ambient-matrix
```

## Summary visualization

- [`SUMMARY.md`](SUMMARY.md) — táblázat + heatmap  
- [`summary-table.svg`](summary-table.svg) — összefoglaló L/nap táblázat  
- [`results-heatmap-liters-per-day.svg`](results-heatmap-liters-per-day.svg)  
- [`results.csv`](results.csv)
