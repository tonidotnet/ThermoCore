**Project:** ThermoCore  
**Track:** AWG Cooling Research & Evolution  
**Status:** Proposed / Ready for implementation planning  
**Baseline:** Existing ThermoCore `main` with Core, AWG, API/Web, calibration/optimization and SolarAirHeater app already implemented.

---

# Vapor Compression Cooling

## Purpose

Define the next active cooling technology after the Peltier demonstrator.

## First model

Use a manufacturer performance map.

Inputs:

```text
Evaporating temperature
Condensing temperature
Compressor speed
```

Outputs:

```text
Cooling capacity
Electrical power
COP
```

## Additional model requirements

```text
Minimum runtime
Minimum off-time
Evaporator UA
Condenser UA
Fan power
Frost threshold
Maximum condensing/discharge temperature
```

## AWG integration

```text
humid process air
→ evaporator
→ condensate separator
→ optional reheat

compressor condenser
→ ambient rejection
or
→ regeneration-air preheat
```

## Prototype strategy

Prefer a complete small DC refrigeration module or donor dehumidifier for first validation. Do not require opening a charged refrigeration circuit.

## Acceptance criteria

- manufacturer map points can be reproduced;
- water removal uses the same psychrometric basis as the existing condenser;
- electrical and thermal balances close;
- cycling is deterministic;
- existing TEC path remains unchanged.
