# ThermoCore
## 23_Calibration.md

**Version:** 1.1  
**Status:** Implemented  
**Document Type:** Calibration and measurement-comparison specification  
**Applies To:** ThermoCore.Core.Calibration, ThermoCore.AWG.Calibration  
**Related tasks:** CAL-001 to CAL-005

---

# 1. Purpose

This document defines the calibration workflow used to fit ThermoCore physics models to prototype measurements, and the MVP measurement-import / comparison path already implemented.

## Goals

- Estimate unknown parameters.
- Minimize model error.
- Preserve physical constraints.
- Produce reproducible parameter sets.
- Compare simulations to measurement CSV without inventing physics in tooling.

---

# 2. Calibration inputs

- Measured weather
- Sensor timestamps
- Air temperatures
- Relative humidity
- Water production
- Electrical power
- Solar irradiance

Channel identifiers shall match simulation result channel ids (DOC-029), for example:

```text
ambient-source.outlet.temperature
ambient-source.outlet.relativeHumidity
solar-radiation.outlet.irradiance
```

---

# 3. Measurement CSV schema (CAL-002)

Long format (compatible with DOC-029 `series-long.csv`):

```text
timestamp_utc,channel_id,value,unit
```

Rules:

- `timestamp_utc` is ISO-8601 / round-trip UTC;
- `channel_id` is the simulation result channel id;
- `value` is SI (or the unit declared in `unit`);
- blank rows are ignored;
- invalid rows produce warnings and are skipped.

Sample dataset:

```text
samples/calibration/awg-mvp-ambient-smoke.csv
```

---

# 4. Import and alignment (CAL-003 / CAL-004)

1. Import CSV via `MeasurementCsvImporter`.
2. Run baseline simulation.
3. Align each measurement sample to the nearest simulation step within ±½ Δt.
4. Reject unmatched samples (counted per channel).
5. Warn on unit mismatches.

Core types:

```text
ThermoCore.Core.Calibration.MeasurementCsvImporter
ThermoCore.Core.Calibration.SimulationMeasurementComparer
```

---

# 5. Error metrics (CAL-005)

Per channel, for aligned pairs \((y_i, \hat{y}_i)\):

```text
Bias = mean(ŷ − y)
MAE  = mean(|ŷ − y|)
RMSE = sqrt(mean((ŷ − y)²))
```

Overall RMSE is the root-mean-square of per-channel RMSE values.

AWG entry point:

```text
ThermoCore.AWG.Calibration.AwgMeasurementValidationRunner
```

Console:

```bash
dotnet run --project src/ThermoCore.Console -- validate samples/calibration/awg-mvp-ambient-smoke.csv --duration 3 --dt 1 --max-rmse 1e-6
```

---

# 6. Parameter fitting (CAL-006)

Algorithm: bounded coordinate descent with golden-section search on each scalar parameter.

Default AWG calibratable ids:

```text
condenser.bypassFactor
condenser.drainageEfficiency
silicaGel.referenceMassTransferCoefficientPerSecond
solarCollector.overallLossCoefficientWPerM2K
heatRecovery.effectivenessFraction   (when HR enabled)
```

Console:

```bash
dotnet run --project src/ThermoCore.Console -- calibrate samples/calibration/awg-mvp-ambient-smoke.csv \
  --duration 3 --dt 1 --params condenser.bypassFactor --db samples/results/calibration.db
```

# 7. Calibration outputs

- Optimized parameter set — implemented
- Error metrics (RMSE, MAE, Bias) — implemented
- Calibration report — MVP console / `MeasurementComparisonReport`
- Provenance in SQLite via `ThermoCore.Persistence` (CAL-007 MVP)
- Holdout validation report — implemented (`AwgHoldoutValidationRunner`, console `holdout`)

---

# 8. Workflow

1. Import measurements.
2. Validate data quality.
3. Synchronize timestamps.
4. Run baseline simulation.
5. Compute residuals.
6. Optimize selected parameters (bounded coordinate descent).
7. Re-run simulation.
8. Compare before/after.
9. Save calibrated parameter set with provenance (`--db`).

---

# 9. Acceptance criteria

MVP measurement comparison and fitting are accepted when:

1. CSV schema is documented and imported;
2. timestamps align to simulation steps;
3. RMSE / MAE / bias are reported per channel;
4. synthetic ambient smoke dataset yields near-zero RMSE on the default MVP boundary;
5. bounded fitting can reduce RMSE versus a wrong initial parameter on synthetic condenser data;
6. calibration provenance can be stored in SQLite.

Holdout (M5 workflow):

```bash
dotnet run --project src/ThermoCore.Console -- holdout samples/calibration/awg-mvp-ambient-smoke.csv \
  --duration 3 --dt 1 --train-fraction 0.67
```

Chronological split uses distinct timestamps (`MeasurementDatasetSplitter`). Prototype field campaign steps: `samples/calibration/PROTOTYPE_CAMPAIGN.md`. Model limits: `26_ModelLimitations.md`.

Later:

- Broader multi-parameter campaigns with physical-limit audits against real prototype CSV.

---

**End of Document**
