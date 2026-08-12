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

## Holdout

```bash
dotnet run --project src/ThermoCore.Console -- holdout samples/calibration/awg-mvp-ambient-smoke.csv \
  --duration 3 --dt 1 --train-fraction 0.67
```

Physical prototype campaign checklist: `PROTOTYPE_CAMPAIGN.md`.

## Prototype wide CSV + hardware metadata (R3-001 / VAL-001…003)

Commercial Peltier / prototype campaigns use a **wide** CSV plus a provenance JSON document.
This extends the existing calibration pipeline; it does **not** replace long-format import.

| Artifact | Role |
|---|---|
| `prototype-campaign.r3-001.json` | Hardware identity, sensor calibration IDs, validation level |
| `prototype-commercial-peltier-wide.csv` | Wide measurement rows (DOC-040 fields) |

```csharp
var package = PrototypeWideCsvImporter.ImportPackageFromFiles(
    "samples/calibration/prototype-campaign.r3-001.json");
var longFormat = PrototypeMeasurementBridge.ToMeasurementDataset(package);
// longFormat plugs into existing validate / holdout / calibrate runners
```

Validation levels: `benchValidated` | `integratedValidated` | `outdoorValidated`.

## Synthetic multi-regime campaign

`awg-mvp-campaign-synthetic.csv` is a **software stand-in** (three ambient/solar regimes, condenser outlet temperature). Not field data.

```bash
dotnet run --project src/ThermoCore.Console -- write-campaign samples/calibration/awg-mvp-campaign-synthetic.csv
dotnet run --project src/ThermoCore.Console -- holdout samples/calibration/awg-mvp-campaign-synthetic.csv \
  --duration 8 --dt 1 --train-fraction 0.7 --params condenser.bypassFactor
```
