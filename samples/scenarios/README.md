# AWG regression scenarios (APP-006 / DOC-022)

Canonical Level-5 scenario definitions used by:

```bash
dotnet run --project src/ThermoCore.Console -- regress
dotnet run --project src/ThermoCore.Console -- regress --dir samples/scenarios
```

Each JSON file maps to `AwgRegressionScenario`. Built-in catalog defaults match these files (including `dry-cool-day.json`).

## Dry sunny matrix pack

Temperature × silica-gel mass under dry sunny conditions (30 scenarios):

```bash
dotnet run --project src/ThermoCore.Console -- regress --dir samples/scenarios/dry-sunny-matrix
```

See `dry-sunny-matrix/README.md` and result diagrams in `dry-sunny-matrix/RESULTS.md`.

## Full AWG flow pack

Same air rate / dry-sunny matrix with **heat recovery + electrical** and station T/RH/W report:

```bash
dotnet run --project src/ThermoCore.Console -- full-flow
dotnet run --project src/ThermoCore.Console -- regress --dir samples/scenarios/full-awg-flow
```

See `full-awg-flow/README.md`, `FLOW.md`, and SVG visuals (`flow-stations.svg`, `results-heatmap.svg`).

## Full AWG ambient matrix (T × RH)

Inlet **20–35 °C** × RH **30–60%** (24 cases) with supervisory controller + summary table visualization:

```bash
dotnet run --project src/ThermoCore.Console -- full-flow-ambient-matrix
```

See `full-awg-ambient-matrix/SUMMARY.md` and `summary-table.svg`.

## Full AWG silica / Peltier sweeps (35 °C / 50% RH)

```bash
dotnet run --project src/ThermoCore.Console -- full-flow-silica-matrix
dotnet run --project src/ThermoCore.Console -- full-flow-peltier-matrix
```

See `full-awg-silica-matrix/SUMMARY.md` and `full-awg-peltier-matrix/SUMMARY.md`.
