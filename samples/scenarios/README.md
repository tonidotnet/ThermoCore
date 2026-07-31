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

See `dry-sunny-matrix/README.md`.
