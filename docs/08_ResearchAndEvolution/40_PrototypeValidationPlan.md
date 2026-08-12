**Project:** ThermoCore  
**Track:** AWG Cooling Research & Evolution  
**Status:** Implemented (R3-001 wide CSV + metadata)  
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

Encoded as `PrototypeValidationLevel` on `PrototypeCampaignDocument` (VAL-003).

## Implementation (R3-001)

| Type | Role |
|---|---|
| `PrototypeCampaignDocument` | Hardware identity, sensor calibration IDs, validation level, CSV path |
| `PrototypeWideCsvImporter` | Import DOC-040 wide CSV |
| `PrototypeMeasurementBridge` | Wide → existing long-format `MeasurementDataset` |
| Sample | `samples/calibration/prototype-campaign.r3-001.json` |

Long-format `MeasurementCsvImporter` remains the simulation comparison path; the wide schema is an adapter only.
