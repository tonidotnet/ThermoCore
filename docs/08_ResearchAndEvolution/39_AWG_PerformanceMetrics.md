**Project:** ThermoCore  
**Track:** AWG Cooling Research & Evolution  
**Status:** Proposed / Ready for implementation planning  
**Baseline:** Existing ThermoCore `main` with Core, AWG, API/Web, calibration/optimization and SolarAirHeater app already implemented.

---

# AWG Performance Metrics

## Required KPIs

```text
L/day
Wh_electric/L
L/kWh_electric
L/kWh_solar_primary
L/day/m² solar aperture
WaterRecoveryFraction
DesorptionCaptureFraction
BareCoolingDeviceCOP
CoolingPlantCOP
AverageTemperatureLift
AverageDewPointMargin
```

## Solar accounting

Track separately:

```text
Incident PV solar energy
Incident thermal-collector solar energy
PV electrical output
Thermal energy transferred to process
Curtailed PV
Recovered internal heat
```

Recovered internal heat must not be counted as new solar input.

## Comparison table

| Variant | L/day | Wh_e/L | L/kWh_e | L/kWh_solar | L/day/m² | recovery |
|---|---:|---:|---:|---:|---:|---:|

Every report must state which energy denominator it uses.
