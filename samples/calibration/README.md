# Calibration / validation samples

Long-format measurement CSV files for simulation-to-measurement comparison (DOC-023 / CAL-002).

## Schema

```text
timestamp_utc,channel_id,value,unit
```

Compatible with DOC-029 `series-long.csv` exports. Channel ids must match simulation result channel ids (for example `ambient-source.outlet.temperature`).

## Smoke dataset

`awg-mvp-ambient-smoke.csv` is a synthetic reference for the default AWG MVP ambient/solar boundary over 3 s at Δt = 1 s (`StartTimeUtc = 2026-01-01T00:00:00Z`).

```bash
dotnet run --project src/ThermoCore.Console -- validate samples/calibration/awg-mvp-ambient-smoke.csv --duration 3 --dt 1
```

Expect near-zero RMSE on ambient and solar channels when using the default configuration without electrical subsystem.
