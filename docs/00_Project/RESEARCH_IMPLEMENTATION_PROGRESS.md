# Research Implementation Progress

| ID | Task | Priority | Status | Dependencies |
|---|---|---|---|---|
| R0-001 | Capture current regression baseline | P0 | Done | none |
| R1-001 | Add water/energy comparison KPIs | P0 | Done | R0-001 |
| R1-002 | Add cooling COP/dew-point channels | P0 | Done | R0-001 |
| R2-001 | Add TEC manufacturer profile schema | P0 | Done | R1 |
| R2-002 | Add dew-point-tracking TEC controller | P0 | Done | R2-001 |
| R3-001 | Add prototype hardware metadata/CSV profile | P0 | Done | R1 |
| R3-002 | Add commercial Peltier black-box model | P1 | Done | R3-001 |
| R4-001 | Add AWG cooling-plant abstraction | P1 | Planned | R2, R3 |
| R5-001 | Add vapor-compression map contract | P1 | Planned | R4-001 |
| R5-002 | Add compressor plant model | P1 | Planned | R5-001 |
| R6-001 | Add hybrid comparison scenarios | P1 | Planned | R4, R5 |
| R7-001 | Absorption feasibility model | P3 | Deferred | R6 |
