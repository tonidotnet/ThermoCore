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

# 6. Calibration outputs

- Optimized parameter set (later — CAL-006)
- Error metrics (RMSE, MAE, Bias) — implemented
- Calibration report — MVP console / `MeasurementComparisonReport`
- Validation report — later holdout workflow

---

# 7. Workflow

1. Import measurements.
2. Validate data quality.
3. Synchronize timestamps.
4. Run baseline simulation.
5. Compute residuals.
6. Optimize selected parameters (not yet implemented).
7. Re-run simulation.
8. Compare before/after.
9. Save calibrated parameter set with provenance (requires persistence).

---

# 8. Acceptance criteria

MVP measurement comparison is accepted when:

1. CSV schema is documented and imported;
2. timestamps align to simulation steps;
3. RMSE / MAE / bias are reported per channel;
4. synthetic ambient smoke dataset yields near-zero RMSE on the default MVP boundary.

Full calibration acceptance (later):

- Lower total RMSE than baseline after fitting;
- No violation of physical limits;
- All calibration metadata stored.

---

**End of Document**
