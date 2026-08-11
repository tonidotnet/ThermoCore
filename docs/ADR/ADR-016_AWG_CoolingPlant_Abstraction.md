# ADR-016: AWG Cooling Plant Abstraction

**Status:** Proposed

## Context

ThermoCore already has explicit Peltier physics, while new research requires comparing thermoelectric, vapor-compression and future absorption cooling.

## Decision

Introduce a common cooling-plant orchestration abstraction in `ThermoCore.AWG`, not `ThermoCore.Core`.

Existing physical Peltier components remain unchanged.

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
