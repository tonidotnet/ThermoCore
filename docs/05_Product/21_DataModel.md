# ThermoCore
## 21_DataModel.md

**Version:** 1.0  
**Status:** ReadyForImplementation  
**Document Type:** Persistence and application data model specification  
**Applies To:** ThermoCore.Api, ThermoCore.Web and infrastructure

---

# 1. Purpose

This document defines persistent entities and schema-versioning rules for ThermoCore configurations, simulation jobs, results and calibration metadata.

Persistence models shall not replace Core domain models.

# 2. Persistence goals

- reproducible simulations;
- versioned configurations;
- efficient result access;
- provider independence;
- local SQLite support;
- hosted PostgreSQL support;
- safe migration;
- clear ownership and privacy.

# 3. Main entities

```text
User
Configuration
ConfigurationVersion
SimulationRun
SimulationJob
SimulationSummary
ResultSeries
DiagnosticRecord
WeatherDataset
ParameterSet
CalibrationRun
SharedLink
```

# 4. Configuration entity

```csharp
public sealed class ConfigurationEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? OwnerUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public bool IsArchived { get; set; }
}
```

# 5. Configuration version

Configurations are immutable after use by a simulation.

```csharp
public sealed class ConfigurationVersionEntity
{
    public Guid Id { get; set; }

    public Guid ConfigurationId { get; set; }

    public int VersionNumber { get; set; }

    public string SchemaVersion { get; set; } = string.Empty;

    public string ConfigurationJson { get; set; } = string.Empty;

    public string ContentHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
```

# 6. Simulation run

```csharp
public sealed class SimulationRunEntity
{
    public Guid Id { get; set; }

    public Guid ConfigurationVersionId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public string CoreVersion { get; set; } = string.Empty;

    public string AwgVersion { get; set; } = string.Empty;

    public string TopologyVersion { get; set; } = string.Empty;

    public string NumericalSettingsJson { get; set; } = string.Empty;

    public string WeatherDatasetId { get; set; } = string.Empty;

    public string ResultFormatVersion { get; set; } = string.Empty;
}
```

# 7. Simulation job

Job-specific fields:

```text
Queue status
Worker ID
Progress
Cancellation requested
Failure code
Retry count
Heartbeat
```

# 8. Summary entity

Store query-friendly summary values:

```text
Collected water kg
Collected water L approximation
Water production rate
Total solar energy J
Total electrical energy J
Fan energy J
Peltier energy J
Minimum battery SOC
Final battery SOC
Maximum temperatures
Maximum pressure drop
Water residual
Energy residual
Warning count
Error count
```

# 9. Time-series storage

Options:

```text
Relational rows
Compressed JSON
Columnar file
Binary file
Object storage
```

Recommended MVP:

- relational metadata;
- compressed result file for full series;
- selected downsampled series in database.

# 10. Result-series descriptor

```csharp
public sealed class ResultSeriesEntity
{
    public Guid Id { get; set; }

    public Guid SimulationRunId { get; set; }

    public string ChannelId { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public string StorageLocation { get; set; } = string.Empty;

    public int SampleCount { get; set; }

    public DateTimeOffset StartTimeUtc { get; set; }

    public double IntervalSeconds { get; set; }
}
```

# 11. Diagnostics

```csharp
public sealed class DiagnosticRecordEntity
{
    public Guid Id { get; set; }

    public Guid SimulationRunId { get; set; }

    public long StepIndex { get; set; }

    public DateTimeOffset SimulationTimeUtc { get; set; }

    public string Severity { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? ComponentId { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? NumericContextJson { get; set; }
}
```

# 12. Weather dataset

Store:

```text
Source
Version
Location
Time range
Timezone
Input hash
Original file reference
Derived-field models
Gap-filling policy
License
```

# 13. Parameter sets

Reusable parameter sets may represent:

```text
Peltier model
Silica gel
Solar collector
PV panel
Fan
Battery
Condenser
```

Every set shall store provenance and validity range.

# 14. Calibration run

Store:

```text
Input measurement dataset
Base parameter set
Objective function
Bounds
Algorithm
Random seed if any
Result parameter set
Training error
Validation error
Run metadata
```

# 15. Schema versioning

Every serialized configuration shall include:

```json
{
  "schemaVersion": "1.0"
}
```

Breaking changes require:

- new schema version;
- migration;
- explicit unsupported-version error.

# 16. Migration rules

Migration shall be:

- deterministic;
- version-to-version;
- testable;
- non-destructive;
- documented.

# 17. Entity/domain mapping

```text
Database entity
    ↓ mapping
Application model
    ↓ mapping
Core/AWG configuration
```

Core shall not reference ORM attributes.

# 18. Provider strategy

Recommended:

```text
SQLite for local/self-hosted
PostgreSQL for hosted
```

The application layer shall depend on repository abstractions.

# 19. Repository interfaces

```csharp
public interface IConfigurationRepository
{
    Task<ConfigurationVersionModel?> GetVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken);

    Task<Guid> SaveVersionAsync(
        ConfigurationVersionModel model,
        CancellationToken cancellationToken);
}
```

# 20. Ownership

Every persistent user-owned resource shall have an owner or explicit anonymous/public status.

# 21. Soft delete

User-facing resources may use archive or soft-delete semantics.

Physical deletion policy shall be defined for privacy and storage management.

# 22. Retention

Configurable retention:

```text
Failed jobs
Full time series
Exports
Anonymous runs
Calibration artifacts
```

# 23. Concurrency

Use optimistic concurrency for editable resources.

Configuration versions themselves are immutable.

# 24. Indexing

Important indexes:

```text
OwnerUserId
CreatedAtUtc
Status
ConfigurationId + VersionNumber
SimulationRunId + ChannelId
SimulationRunId + Severity
ContentHash
```

# 25. Result immutability

Completed simulation results shall be immutable.

A rerun creates a new run.

# 26. Reproducibility bundle

A simulation shall be reproducible from:

```text
Configuration version
Model versions
Numerical settings
Weather dataset
Initial state
Topology version
Result capture policy
```

# 27. Security

- no secrets inside configuration JSON;
- protect user-owned data;
- validate file locations;
- avoid arbitrary path access;
- encrypt sensitive data at infrastructure level where required.

# 28. Required tests

- configuration version immutability;
- schema migration;
- content hash;
- save/load round trip;
- provider compatibility;
- ownership filtering;
- simulation metadata completeness;
- diagnostic persistence;
- result descriptor;
- deletion/retention policy;
- concurrency conflict.

# 29. Acceptance criteria

The data model is accepted when:

1. Core domain types remain persistence-independent;
2. every simulation stores reproducibility metadata;
3. configurations are versioned;
4. completed results are immutable;
5. large series are stored efficiently;
6. SQLite and PostgreSQL can share application contracts.

---

**End of Document**
