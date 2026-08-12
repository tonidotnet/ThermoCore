# R5-002 — Compressor / Vapor-Compression Plant Model

**Status:** Done

Requirements:
COOL-006, COOL-007.

Purpose:
build a map-based vapor-compression cooling plant on the R5-001 contract.

Implement:
- Core `VaporCompressionCoolingPlantModel` (map → evaporator-coil psychrometrics via CondenserComponent);
- deterministic min-runtime / min-off-time cycling;
- AWG `VaporCompressionCoolingPlantAdapter` + factory wiring.

Acceptance:
- water removal uses the same psychrometric condenser basis;
- electrical/thermal balances close;
- cycling is deterministic;
- existing TEC path / regression unchanged.
