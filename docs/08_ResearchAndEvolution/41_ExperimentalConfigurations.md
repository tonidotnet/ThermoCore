**Project:** ThermoCore  
**Track:** AWG Cooling Research & Evolution  
**Status:** Proposed / Ready for implementation planning  
**Baseline:** Existing ThermoCore `main` with Core, AWG, API/Web, calibration/optimization and SolarAirHeater app already implemented.

---

# Experimental Configurations

```text
EXP-A01 Commercial Peltier / ambient baseline
EXP-A02 Commercial Peltier / controlled high dew point
EXP-B01 Solar heating only / Peltier
EXP-C01 Sorbent regeneration / commercial Peltier
EXP-C02 Sorbent regeneration / characterized bare TEC
EXP-D01 Direct vapor compression
EXP-D02 Sorbent regeneration / vapor compression
```

## Suggested environmental matrix

```text
20 / 25 / 30 / 35 °C
30 / 40 / 50 / 60 % RH
```

A smaller matrix is acceptable for early bench work.

## Hardware metadata

Every run shall store:

```text
Manufacturer
Model
Asset/serial ID
Modifications
Power supply
Fan configuration
Heat exchanger configuration
Sensor calibration IDs
```
