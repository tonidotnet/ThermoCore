**Project:** ThermoCore  
**Track:** AWG Cooling Research & Evolution  
**Status:** Proposed / Ready for implementation planning  
**Baseline:** Existing ThermoCore `main` with Core, AWG, API/Web, calibration/optimization and SolarAirHeater app already implemented.

---

# Current Implementation Audit

## Purpose

Record the current architectural baseline so future AI-assisted work does not treat ThermoCore as an unimplemented MVP.

## Existing baseline to preserve

The current repository already contains:

```text
ThermoCore.Core
ThermoCore.AWG
ThermoCore.Application
ThermoCore.Persistence
ThermoCore.Console
ThermoCore.Api
ThermoCore.Web
ThermoCore.App2.SolarAirHeater
```

The existing platform already supports a reusable simulation Core and at least two application-level usages.

The current AWG implementation already includes the major physical subsystems:

- psychrometrics;
- conservation balances;
- graph execution and cyclic recirculation;
- thermoelectric cooling;
- silica-gel adsorption/regeneration;
- condenser;
- heat recovery;
- fan/airflow;
- PV and solar collector;
- battery/power;
- control;
- weather;
- calibration;
- optimization.

## Existing thermoelectric baseline

The current codebase contains both simplified and analytical Peltier modeling. New work shall extend this capability rather than rename or replace it.

## Main validation gap

The largest remaining scientific gap is physical measurement and calibration against a real prototype.

## New research track

The next development track shall focus on:

1. fair cooling and solar-resource KPIs;
2. manufacturer TEC characterization;
3. dew-point-tracking TEC control;
4. commercial Peltier dehumidifier measurement;
5. cooling technology abstraction at AWG level;
6. vapor-compression cooling model;
7. hybrid sorbent + active cooling comparison;
8. absorption cooling feasibility research.

## Non-goals

Do not:

- rewrite ThermoCore.Core;
- move AWG-specific orchestration into Core;
- remove existing Peltier components;
- break existing configuration files;
- change historical AWG V3 regression outputs without explicit migration and justification.
