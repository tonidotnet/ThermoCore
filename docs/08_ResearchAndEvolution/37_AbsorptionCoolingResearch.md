**Project:** ThermoCore  
**Track:** AWG Cooling Research & Evolution  
**Status:** Proposed / Ready for implementation planning  
**Baseline:** Existing ThermoCore `main` with Core, AWG, API/Web, calibration/optimization and SolarAirHeater app already implemented.

---

# Absorption Cooling Research

## Status

Research only (**R7-001 / COOL-008**).

**Implemented scaffold:** Core `AbsorptionPerformanceMap` + `AbsorptionCoolingResearchModel`
(performance-map feasibility screen). AWG exposes `AbsorptionCoolingResearchFacade`.
`CoolingTechnology.AbsorptionResearch` is **not** selectable for production plants.

## Motivation

Absorption cooling is potentially interesting because direct solar thermal energy could drive refrigeration without first converting all energy to electricity.

## Reasons not to implement first

- pressurized refrigerant system;
- ammonia safety concerns in ammonia/water systems;
- low small-scale thermal COP;
- slow dynamics;
- harder DIY fabrication;
- limited open performance data.

## Recommended first model

If pursued later, use a performance map:

```text
Generator temperature
Heat-sink temperature
Evaporator temperature
Thermal input
Cooling output
Thermal COP
```

Do not build a detailed absorption-cycle solver until a specific device or dataset justifies it.
