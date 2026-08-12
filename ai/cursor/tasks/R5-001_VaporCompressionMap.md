# R5-001 — Vapor Compression Performance-Map Contract

**Status:** Done

Requirements:
COOL-006, COOL-007.

Initial model must be map-based, not a full refrigerant solver.

Inputs:
- evaporating temperature;
- condensing temperature;
- speed/control.

Outputs:
- cooling capacity;
- electrical power;
- COP.

Also define:
- map interpolation;
- extrapolation policy;
- min runtime/off-time;
- frost/safety diagnostics.

Acceptance:
- reference manufacturer map points reproduced;
- explicit out-of-range behavior;
- deterministic interpolation tests.
