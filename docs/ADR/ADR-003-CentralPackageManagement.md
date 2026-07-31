# ADR-003: Central Package Management

**Status:** Accepted (migrated)  
**Date:** 2026-07-31  
**Updated:** 2026-07-28  
**Related:** DEV-004

## Context

Session C asked for `Directory.Packages.props` (CPM) across the solution. An earlier revision deferred CPM while product sessions landed in parallel. Dependency churn is now stable enough for a full migration.

## Decision

Adopt NuGet Central Package Management:

- Root `Directory.Packages.props` owns all `PackageVersion` entries and sets `ManagePackageVersionsCentrally`.
- Project files declare `PackageReference` items **without** `Version` attributes.
- `Directory.Build.props` continues to centralize nullable / warning policy (DEV-003).

## Consequences

- DEV-004 is Done via CPM.
- Version bumps happen in one file; restore fails if a project references an undeclared package id.
- Architecture boundary tests (DEV-008) remain independent of package versioning.
