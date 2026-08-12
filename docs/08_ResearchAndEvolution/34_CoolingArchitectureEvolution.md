**Project:** ThermoCore  
**Track:** AWG Cooling Research & Evolution  
**Status:** Proposed / Ready for implementation planning  
**Baseline:** Existing ThermoCore `main` with Core, AWG, API/Web, calibration/optimization and SolarAirHeater app already implemented.

---

# Cooling Architecture Evolution

## Decision

Keep existing thermoelectric physical components unchanged.

Add AWG-level orchestration:

```text
ThermoCore.AWG/Cooling/
  ICoolingPlantModel
  CoolingTechnology
  CoolingPlantRequest
  CoolingPlantResult
  ThermoelectricCoolingPlantAdapter
  CommercialPeltierDehumidifierModel   // adapter over Core black-box (R4)
  VaporCompressionCoolingPlant
  AbsorptionCoolingResearchModel
```

Empirical commercial black-box physics already lives in Core (R3-002 / COOL-005):
`CommercialPeltierDehumidifierProfile`, `…Model`, `…Component`, fitter from R3-001 packages.
Do not move the AWG cooling-plant abstraction to Core until another application demonstrates reuse.

## Common orchestration result

Every cooling plant shall report:

```text
Outlet moist-air state
Collected water
Cooling delivered
Electrical input
Thermal input
Rejected heat
Cooling-plant COP
Pressure drop
Diagnostics
Conservation balance
```

Technology-specific state and parameters remain technology-specific.

## Backward compatibility

Existing AWG configurations shall default to the existing thermoelectric path.

New schema fields must be optional for old configurations.

## Acceptance criteria

- existing regression scenarios remain stable;
- old configuration files continue to load;
- technology can be switched without rewriting topology;
- all technologies expose comparable KPIs.
