# ThermoCore
## 23_Calibration.md

**Version:** 1.0
**Status:** ReadyForImplementation

# Purpose

This document defines the calibration workflow used to fit the ThermoCore physics models to real prototype measurements.

## Goals

- Estimate unknown parameters.
- Minimize model error.
- Preserve physical constraints.
- Produce reproducible parameter sets.

## Calibration inputs

- Measured weather
- Sensor timestamps
- Air temperatures
- Relative humidity
- Water production
- Electrical power
- Solar irradiance

## Calibration outputs

- Optimized parameter set
- Error metrics (RMSE, MAE, Bias)
- Calibration report
- Validation report

## Workflow

1. Import measurements.
2. Validate data quality.
3. Synchronize timestamps.
4. Run baseline simulation.
5. Compute residuals.
6. Optimize selected parameters.
7. Re-run simulation.
8. Compare before/after.
9. Save calibrated parameter set.

## Acceptance criteria

- Lower total RMSE than baseline.
- No violation of physical limits.
- All calibration metadata stored.
