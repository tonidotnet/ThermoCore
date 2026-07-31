# ADR-003: Central Package Management deferral

**Status:** Accepted  
**Date:** 2026-07-31  
**Related:** DEV-004

## Context

Session C asked for `Directory.Packages.props` (CPM) across the solution. Migrating every PackageReference in one pass risks breaking restore/CI while many product sessions land in parallel.

## Decision

Defer full CPM. Keep per-project `PackageReference` versions for now. `Directory.Build.props` centralizes nullable/warning policy (DEV-003). Revisit CPM when dependency churn stabilizes.

## Consequences

- DEV-004 remains Partially Deferred (documented here) until a dedicated CPM migration PR.
- Architecture boundary tests (DEV-008) proceed without waiting on CPM.
