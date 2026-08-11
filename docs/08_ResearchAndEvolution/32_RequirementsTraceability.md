**Project:** ThermoCore  
**Track:** AWG Cooling Research & Evolution  
**Status:** Proposed / Ready for implementation planning  
**Baseline:** Existing ThermoCore `main` with Core, AWG, API/Web, calibration/optimization and SolarAirHeater app already implemented.

---

# Requirements Traceability

## New requirements

| ID | Requirement | Priority |
|---|---|---|
| COOL-001 | Preserve existing Peltier baseline and regressions | P0 |
| COOL-002 | Add AWG cooling-technology selection without breaking existing configs | P0 |
| COOL-003 | Add manufacturer TEC profile support | P0 |
| COOL-004 | Add dew-point-tracking TEC control | P0 |
| COOL-005 | Add commercial Peltier dehumidifier black-box model | P1 |
| COOL-006 | Add vapor-compression performance-map model | P1 |
| COOL-007 | Add compressor cycling/minimum runtime behavior | P1 |
| COOL-008 | Keep absorption cooling research-only initially | P3 |
| HYB-001 | Compare direct TEC vs sorbent+TEC | P0 |
| HYB-002 | Compare direct compressor vs sorbent+compressor | P1 |
| HYB-003 | Track exhausted regeneration vapor explicitly | P0 |
| KPI-001 | Add L/kWh_electric | P0 | Done (R1-001) |
| KPI-002 | Add L/kWh_solar_primary | P0 | Done (R1-001) |
| KPI-003 | Add L/day/m² solar aperture | P0 | Done (R1-001) |
| KPI-004 | Add WaterRecoveryFraction | P1 | Done (R1-001) |
| KPI-005 | Add CoolingPlantCOP and dew-point margin | P0 | Done (R1-002) |
| VAL-001 | Define A/B/C prototype measurement campaign | P0 |
| VAL-002 | Store hardware identity/provenance with calibration data | P0 |
| VAL-003 | Distinguish Bench/Integrated/Outdoor validation | P1 |

## Implementation rule

Every issue/PR in this track must reference at least one requirement ID and name:

- reused existing component;
- new component;
- specification;
- tests;
- validation evidence;
- comparison baseline.
