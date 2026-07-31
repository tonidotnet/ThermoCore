# Full AWG process-flow pack

Same dry-sunny boundaries as `dry-sunny-matrix` (**ṁ = 0.02 kg/s**, 30% RH, G = 950 W/m², battery SOC 90%, silica 1–5 kg, T = 10–35 °C), but with the **full AWG V3 train**:

- heat recovery **on**
- electrical subsystem **on**
- process air: ambient → HR cold → fan → Peltier hot → collector → silica → condenser → HR hot → exhaust
- solar radiation → collector absorber

## Station report (demo)

Generate / refresh:

```bash
dotnet run --project src/ThermoCore.Console -- full-flow
```

Outputs:

- [`FLOW.md`](FLOW.md) — diagram + T/RH/W table  
- [`flow-stations.svg`](flow-stations.svg) — process train with station T/RH/W  
- [`flow-station-bars.svg`](flow-station-bars.svg) — temperature bars along the path  
- [`RESULTS.md`](RESULTS.md) — matrix heatmaps + charts  
- [`results-heatmap.svg`](results-heatmap.svg) — L/day vs T × silica (full flow)  
- [`stations.csv`](stations.csv) / [`results.csv`](results.csv)  
- `full-awg-flow-demo.json` + `full-awg-T*C-silica*kg.json` — scenario pack  

## Regress the matrix

```bash
dotnet run --project src/ThermoCore.Console -- regress --dir samples/scenarios/full-awg-flow
```
