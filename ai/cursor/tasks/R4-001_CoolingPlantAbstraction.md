# R4-001 — Backward-Compatible AWG Cooling Plant Abstraction

**Status:** Done

Requirements:
COOL-001, COOL-002.

Implement in AWG/Application layer first.

Introduce:
- cooling technology selection;
- common orchestration request/result;
- thermoelectric adapter using existing Peltier + condenser;
- commercial black-box adapter if available.

Do not move physical Peltier equations.

Acceptance:
- existing AWG configs default to current TEC behavior;
- old regression baseline stays within tolerance;
- technology switching requires no unrelated topology rewrite.
