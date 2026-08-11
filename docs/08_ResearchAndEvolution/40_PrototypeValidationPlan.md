**Project:** ThermoCore  
**Track:** AWG Cooling Research & Evolution  
**Status:** Proposed / Ready for implementation planning  
**Baseline:** Existing ThermoCore `main` with Core, AWG, API/Web, calibration/optimization and SolarAirHeater app already implemented.

---

# Prototype Validation Plan

## Phase 1 — Commercial Peltier baseline

Run a ready-made Peltier dehumidifier intact.

Measure:

```text
Inlet T/RH
Outlet T/RH
Electrical power
Collected water mass
Duration
```

Optional:

```text
Airflow
Cold-surface temperature
Hot-side temperature
```

## Phase 2 — Controlled high-dew-point inlet

Measure behavior at several inlet dew points.

## Phase 3 — A/B/C comparison

```text
A direct ambient → Peltier
B solar-heated ambient → Peltier
C sorbent regeneration → Peltier
```

## Phase 4 — Optimized bare TEC

Only after commercial-unit data exists.

## Phase 5 — Compressor comparison

Repeat selected cases using a small complete vapor-compression module.

## Suggested CSV fields

```text
timestampUtc
testId
variantId
ambientTemperatureC
ambientRhPercent
inletTemperatureC
inletRhPercent
outletTemperatureC
outletRhPercent
coldSurfaceTemperatureC
hotSideTemperatureC
airflowM3PerHour
voltageV
currentA
powerW
solarIrradianceWPerM2
waterMassG
sorbentMassG
notes
```

## Validation levels

```text
BenchValidated
IntegratedValidated
OutdoorValidated
```
