# ADR-001: Open-source license selection

**Status:** Accepted  
**Date:** 2026-07-30

## Context

ThermoCore is intended as a reusable thermodynamic simulation platform with a public reference AWG application. Contributors and downstream users need a clear redistribution license before packaging, CI publication, and external contributions.

## Decision

Use the **Apache License 2.0** for the ThermoCore repository (source, documentation, and samples unless a file states otherwise).

## Alternatives

- MIT: simpler, but weaker explicit patent grant language.
- GPL-family: stronger copyleft; less suitable for embedding the Core library in proprietary engineering tools.
- Proprietary / delayed OSS: conflicts with the stated open engineering-platform goal.

## Consequences

- The full Apache-2.0 text lives in `LICENSE`.
- Contributions are assumed to be under Apache-2.0 unless otherwise agreed.
- Third-party dependencies must remain license-compatible.

## Related documents

- `LICENSE`
- `docs/00_Project/IMPLEMENTATION_PROGRESS.md` (REP-003, OSS-001)
- `README.md`
