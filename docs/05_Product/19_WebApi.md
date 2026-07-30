# ThermoCore
## 19_WebApi.md

**Version:** 1.0  
**Status:** Implemented  
**Document Type:** ASP.NET Core Web API specification  
**Applies To:** ThermoCore.Api  
**Primary runtime:** .NET 10 or newer  
**Internal units:** SI

---

# 1. Purpose

This document defines the public and internal HTTP API used to run ThermoCore simulations, validate configurations, perform psychrometric calculations and retrieve results.

The API shall expose application services only. It shall not duplicate physical equations from ThermoCore.Core or ThermoCore.AWG.

# 2. Architectural placement

```text
ThermoCore.Web
    ↓
ThermoCore.Api
    ↓
ThermoCore.AWG
    ↓
ThermoCore.Core
```

The API may depend on application and persistence abstractions but must not contain UI logic.

# 3. API goals

- explicit versioned contracts;
- structured validation;
- deterministic DTO-to-Core mapping;
- safe long-running simulation jobs;
- cancellation;
- result pagination and export;
- resource limits;
- OpenAPI documentation;
- stable error model.

# 4. Route versioning

Initial route prefix:

```text
/api/v1
```

Example:

```text
POST /api/v1/simulations
```

Breaking contract changes require a new version.

# 5. Main endpoints

```text
GET  /api/v1/health
GET  /api/v1/models
POST /api/v1/psychrometrics/calculate
POST /api/v1/configurations/validate
POST /api/v1/simulations
GET  /api/v1/simulations/{simulationId}
POST /api/v1/simulations/{simulationId}/cancel
GET  /api/v1/simulations/{simulationId}/summary
GET  /api/v1/simulations/{simulationId}/series
GET  /api/v1/simulations/{simulationId}/diagnostics
GET  /api/v1/simulations/{simulationId}/export
```

# 6. Health endpoint

```text
GET /api/v1/health
```

Response:

```json
{
  "status": "Healthy",
  "applicationVersion": "0.1.0",
  "coreVersion": "0.1.0",
  "timestampUtc": "2026-07-27T20:00:00Z"
}
```

No detailed infrastructure secrets shall be exposed publicly.

# 7. Model catalog endpoint

```text
GET /api/v1/models
```

Returns supported:

- topology IDs;
- component model IDs;
- fidelity levels;
- schema versions;
- supported result channels;
- parameter metadata.

# 8. Psychrometric calculation endpoint

```text
POST /api/v1/psychrometrics/calculate
```

Request:

```json
{
  "temperatureC": 30.0,
  "relativeHumidityPercent": 50.0,
  "absolutePressurePa": 101325.0
}
```

Response:

```json
{
  "temperatureC": 30.0,
  "relativeHumidityPercent": 50.0,
  "humidityRatioKgPerKgDryAir": 0.01331,
  "dewPointTemperatureC": 18.45,
  "specificEnthalpyKJPerKgDryAir": 64.2,
  "specificVolumeM3PerKgDryAir": 0.877
}
```

The API layer performs unit conversion only.

# 9. Configuration validation endpoint

```text
POST /api/v1/configurations/validate
```

Response:

```json
{
  "isValid": false,
  "errors": [
    {
      "path": "peltier.maximumCurrentA",
      "code": "ValueOutOfRange",
      "message": "Maximum current must be greater than zero.",
      "expectedRange": "> 0 A"
    }
  ],
  "warnings": []
}
```

# 10. Simulation creation

```text
POST /api/v1/simulations
```

Request contains:

```text
schemaVersion
topology
time range
timestep
weather source
component configuration
initial state
result capture policy
numerical settings
```

Response:

```json
{
  "simulationId": "01J...",
  "status": "Queued",
  "createdAtUtc": "2026-07-27T20:00:00Z"
}
```

# 11. Small synchronous calculations

Short deterministic calculations may return immediately.

Full simulations shall use a job workflow.

# 12. Simulation status

```text
GET /api/v1/simulations/{simulationId}
```

Response:

```json
{
  "simulationId": "01J...",
  "status": "Running",
  "progressFraction": 0.42,
  "completedSteps": 604,
  "totalSteps": 1440,
  "simulationTimeUtc": "2026-07-27T10:04:00Z",
  "startedAtUtc": "2026-07-27T20:00:10Z"
}
```

# 13. Cancellation

```text
POST /api/v1/simulations/{simulationId}/cancel
```

Cancellation is idempotent.

Possible responses:

```text
202 Accepted
404 Not Found
409 Already completed
```

# 14. Summary result

```text
GET /api/v1/simulations/{simulationId}/summary
```

Response includes:

```text
Collected water
Exhausted water vapor
Solar energy
Electrical consumption
Peltier energy
Fan energy
Battery minimum and final SOC
Maximum temperatures
Balance residuals
Diagnostic counts
```

# 15. Time-series endpoint

```text
GET /api/v1/simulations/{simulationId}/series
```

Query parameters:

```text
channels
from
to
interval
page
pageSize
```

Large result sets shall be paginated or downsampled.

# 16. Diagnostics endpoint

```text
GET /api/v1/simulations/{simulationId}/diagnostics
```

Filter options:

```text
severity
componentId
code
fromStep
toStep
```

# 17. Export endpoint

```text
GET /api/v1/simulations/{simulationId}/export?format=csv
```

Supported initial formats:

```text
csv
json
zip
```

# 18. DTO separation

API DTOs shall not be reused as Core domain types.

Mapping flow:

```text
HTTP DTO
    ↓
Application command
    ↓
Core/AWG configuration
```

# 19. Unit mapping

DTO property names shall include units.

Examples:

```csharp
TemperatureC
AbsolutePressurePa
VolumetricFlowM3PerHour
EnergyWh
PowerW
RelativeHumidityPercent
```

Core receives SI.

# 20. Error model

Use RFC 9457-compatible problem details where appropriate.

Example:

```json
{
  "type": "https://thermocore.dev/problems/configuration-invalid",
  "title": "Configuration is invalid",
  "status": 400,
  "traceId": "...",
  "errors": [
    {
      "path": "battery.minimumSocFraction",
      "code": "InvalidRange",
      "message": "Minimum SOC must be lower than maximum SOC."
    }
  ]
}
```

# 21. HTTP status guidance

```text
200 OK
201 Created
202 Accepted
400 Validation error
404 Resource not found
409 State conflict
413 Payload too large
422 Physically invalid request
429 Resource limit exceeded
500 Unexpected server error
503 Job system unavailable
```

# 22. Job execution abstraction

```csharp
public interface ISimulationJobService
{
    Task<SimulationJobCreated> CreateAsync(
        CreateSimulationCommand command,
        CancellationToken cancellationToken);

    Task<SimulationJobStatus?> GetStatusAsync(
        string simulationId,
        CancellationToken cancellationToken);

    Task<bool> CancelAsync(
        string simulationId,
        CancellationToken cancellationToken);
}
```

# 23. Resource limits

Configurable limits:

```text
Maximum simulation duration
Minimum timestep
Maximum timestep count
Maximum parameter count
Maximum upload size
Maximum concurrent jobs per user
Maximum result channels
Maximum export size
```

# 24. Authentication

Anonymous mode may be supported for local or demo deployments.

Future authenticated mode shall support:

```text
User-owned configurations
User-owned simulations
Private results
Public share links
Quotas
```

# 25. Idempotency

Simulation creation may accept:

```text
Idempotency-Key
```

Repeated requests with the same key and body shall not create duplicate jobs.

# 26. OpenAPI

Every endpoint shall document:

- request schema;
- response schema;
- units;
- validation;
- errors;
- examples;
- authentication requirement.

# 27. Logging

Structured logs shall include:

```text
simulationId
userId if available
route
status
duration
result size
failure code
```

No sensitive configuration or token shall be logged.

# 28. API version metadata

Responses may include:

```text
apiVersion
schemaVersion
modelVersion
```

# 29. Required tests

- health endpoint;
- model catalog;
- psychrometric mapping;
- invalid configuration;
- simulation creation;
- idempotency;
- status;
- cancellation;
- summary;
- pagination;
- export;
- resource limits;
- problem-details structure;
- no stack-trace leakage;
- equivalent Console/API Core input.

# 30. Acceptance criteria

The API is accepted when:

1. no physical formula exists in controllers;
2. all user-unit conversion is explicit;
3. long simulations use cancellable jobs;
4. validation errors are structured;
5. result endpoints handle large datasets;
6. contracts are versioned;
7. API and Console produce equivalent Core results.

---

**End of Document**
