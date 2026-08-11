# R0-001 — AWG regression baseline

Pre-research machine-readable baseline for ThermoCore AWG regressions.

## Identifiers

| Field | Value |
|---|---|
| Task | `R0-001` |
| Captured (UTC) | `2026-08-11T22:02:57.9555547+00:00` |
| Git commit | `8138b5359f591ea80c05822e36972432b8a86aab` |

## Suites

- **doc-022-default** → `doc-022-default-baseline.json` 
  (10/10 passed) — DOC-022 / APP-006 default AWG regression scenarios (CreateDefaultScenarios).
- **dry-sunny-matrix** → `dry-sunny-matrix-baseline.json` 
  (30/30 passed) — Dry-sunny T×silica matrix (CreateDrySunnyMatrixScenarios).

## Reproduce

```bash
dotnet build ThermoCore.slnx -nologo
dotnet run --project src/ThermoCore.Console -- capture-baseline --suite all
dotnet run --project src/ThermoCore.Console -- regress
dotnet run --project src/ThermoCore.Console -- regress --dir samples/scenarios/dry-sunny-matrix
dotnet test tests/ThermoCore.AWG.Tests --filter FullyQualifiedName~AwgRegressionAndPvRearAirTests
```

Later research PRs should compare scenario fingerprints, residuals, and water/tank metrics against these JSON files.
