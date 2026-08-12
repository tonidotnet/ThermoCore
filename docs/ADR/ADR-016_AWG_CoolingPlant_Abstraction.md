# ADR-016: AWG Cooling Plant Abstraction

**Status:** Accepted (R4-001)

## Context

ThermoCore already has explicit Peltier physics, while new research requires comparing thermoelectric, vapor-compression and future absorption cooling.

## Decision

Introduce a common cooling-plant orchestration abstraction in `ThermoCore.AWG`, not `ThermoCore.Core`.

Existing physical Peltier components remain unchanged.

Implemented under `ThermoCore.AWG/Cooling/`:
- `ICoolingPlantModel`, `CoolingTechnology`, `CoolingPlantRequest` / `CoolingPlantResult`
- `ThermoelectricCoolingPlantAdapter` (Condenser + heat-source proxy)
- `CommercialPeltierCoolingPlantAdapter` (Core black-box)
- `CoolingPlantFactory` + optional `AwgSystemConfiguration.Cooling` (default `Thermoelectric`)

Full AWG V3 graph wiring remains the thermoelectric ControllableHeatSource + Condenser path;
technology switching for comparison uses the shared request/result contract without rewriting unrelated topology.

## Consequences

Positive:

- existing physics stays explicit;
- old configs remain valid;
- technologies become comparable at AWG level;
- Core avoids premature abstraction.

Negative:

- one additional orchestration layer;
- some duplicated technology-selection mapping may exist until reuse is demonstrated.

## Promotion rule

Move the abstraction into Core only after a second independent ThermoCore application needs the same cooling-plant concept.
