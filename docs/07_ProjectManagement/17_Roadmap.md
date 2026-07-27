# ThermoCore
## 17_Roadmap.md

**Version:** 1.0  
**Document Type:** Product and Development Roadmap  
**Status:** Active Draft  
**Applies To:** ThermoCore framework, ThermoCore.AWG, ThermoCore.Web and supporting applications  
**Primary implementation language:** C#  
**Primary runtime:** .NET 8 or newer  
**Primary delivery targets:** Console, Web API, Blazor Web App, optional desktop client

---

# 1. Purpose

This document defines the staged implementation roadmap for ThermoCore and its first concrete application, ThermoCore.AWG.

The roadmap is designed to support:

- Incremental implementation
- AI-assisted coding
- Early executable milestones
- Test-driven validation
- Platform independence
- Web-first product delivery
- Future calibration against physical prototypes
- Controlled growth from simple models to higher-fidelity simulations

The roadmap prioritizes a working, testable simulation platform over premature architectural or UI complexity.

---

# 2. Product Vision

ThermoCore is a reusable thermodynamic and mass-transfer simulation framework.

ThermoCore.AWG is the first implementation and models a portable atmospheric water generator containing:

- Ambient moist air
- Peltier hot-side heat recovery
- Photovoltaic rear-air cooling
- Directly irradiated solar air collector
- Silica-gel adsorption and regeneration
- Peltier-assisted condensation
- Water collection
- Heat recovery
- Air recirculation
- Battery and electrical-power management

The first publicly usable product shall be a web application where users can configure, run, compare and export simulations.

---

# 3. Guiding Principles

Development shall follow these principles:

1. Core physics before visual polish.
2. Conservation before empirical tuning.
3. Deterministic results before optimization.
4. Platform-independent Core libraries.
5. Web application as the primary user-facing product.
6. Console application as the simplest reference host.
7. Every milestone must compile and run.
8. Every physical module must include unit tests.
9. Every model assumption must be documented.
10. Higher fidelity shall be introduced only after lower-fidelity validation.

---

# 4. Target Solution Structure

```text
ThermoCore.sln

src/
├── ThermoCore.Core
├── ThermoCore.AWG
├── ThermoCore.Console
├── ThermoCore.Api
├── ThermoCore.Web
└── ThermoCore.Desktop          # optional, not required for MVP

tests/
├── ThermoCore.Core.Tests
├── ThermoCore.AWG.Tests
├── ThermoCore.Api.Tests
└── ThermoCore.Web.Tests

docs/
├── Requirements
├── Engineering
├── Mathematics
├── Modules
│   ├── AWG
│   └── Web
└── Decisions

samples/
├── configurations
├── weather
└── results
```

---

# 5. Delivery Strategy

The implementation shall be divided into five major stages:

```text
Stage A — Mathematical Core
Stage B — ThermoCore Graph Engine
Stage C — AWG Physical Modules
Stage D — Web Product
Stage E — Calibration and Optimization
```

Each stage contains independently verifiable milestones.

---

# 6. Stage A — Mathematical Core

## Objective

Create a platform-independent, deterministic mathematical foundation.

## Milestone A1 — Solution Bootstrap

Deliverables:

- `ThermoCore.sln`
- `ThermoCore.Core`
- `ThermoCore.Console`
- `ThermoCore.Core.Tests`
- Nullable reference types enabled
- Central package management if used
- Build scripts
- Basic CI configuration
- Initial README
- Versioning policy

Acceptance criteria:

- Solution builds without warnings treated as errors where configured.
- Tests run successfully.
- Console application starts.
- Core project has no UI or ASP.NET dependency.

## Milestone A2 — Units and Constants

Deliverables:

- SI-unit policy
- Physical constants
- Temperature conversion
- Flow conversion
- Energy and power conversion
- Validation helpers
- Finite-number guards

Acceptance criteria:

- No duplicated physical constants.
- Unit tests verify conversions.
- Public property names include units where ambiguity is possible.

## Milestone A3 — Psychrometrics

Deliverables:

- Saturation-pressure provider
- Relative humidity
- Humidity ratio
- Vapor pressure
- Dew point
- Moist-air enthalpy
- Specific volume
- Density
- Moist-air immutable state factory
- Round-trip tests

Acceptance criteria:

- Sensible heating preserves humidity ratio and dew point.
- RH and humidity-ratio conversions round-trip within tolerance.
- Supersaturation is detected.
- Reference cases pass.

## Milestone A4 — Conservation Infrastructure

Deliverables:

- Energy-balance model
- Dry-air balance
- Water balance
- Electrical balance
- Residual calculation
- Balance validation
- Diagnostics model

Acceptance criteria:

- Component-level residuals can be aggregated.
- Invalid balance results are visible and testable.
- No silent clamping of conservation errors.

---

# 7. Stage B — ThermoCore Graph Engine

## Objective

Create a generic port-based directed simulation graph.

## Milestone B1 — Physical State and Port Model

Deliverables:

- Physical-domain enumeration
- Typed port abstractions
- Moist-air ports
- Heat ports
- Electrical ports
- Liquid-water ports
- Solar-radiation ports
- Control ports

Acceptance criteria:

- Incompatible domains cannot connect.
- Required ports can be validated.
- Ports do not contain UI logic.

## Milestone B2 — Component Abstraction

Deliverables:

- `ISimulationComponent`
- Evaluation context
- Step result
- Evaluate/Commit separation
- Internal-state abstraction
- Component diagnostics

Acceptance criteria:

- A component can be unit-tested without a graph.
- Evaluation does not mutate committed state.
- Results are immutable where practical.

## Milestone B3 — Acyclic Graph Execution

Deliverables:

- Component registry
- Connection registry
- Graph validation
- Topological sorting
- Acyclic execution engine
- Timestep loop
- Result capture

Acceptance criteria:

- A linear simulation graph executes deterministically.
- Invalid graph topology prevents simulation start.
- Component and system balances are reported.

## Milestone B4 — Generic Sources, Sinks, Mixer and Splitter

Deliverables:

- Ambient-air source
- Solar-radiation source
- Electrical source
- Exhaust sink
- Environment heat sink
- Water sink
- Moist-air mixer
- Moist-air splitter

Acceptance criteria:

- Mixer conserves dry air, water and enthalpy.
- Splitter conserves all stream quantities.
- Sources and sinks make external boundaries explicit.

## Milestone B5 — Cyclic Graph Solver

Deliverables:

- Cycle detection
- Fixed-point iteration
- Relaxation
- Convergence tolerances
- Iteration diagnostics
- Failure handling

Acceptance criteria:

- A recirculation loop converges for a stable test case.
- Non-convergence is reported clearly.
- Previous-timestep state can initialize loop variables.

---

# 8. Stage C — AWG Physical Modules

## Objective

Implement the first complete application using ThermoCore.

## Milestone C1 — Solar Panel

Deliverables:

- Constant-efficiency model
- Temperature-corrected model
- Dynamic electrothermal model
- Rear-air channel
- Electrical output
- Thermal balance
- Unit tests

Acceptance criteria:

- Electrical output falls with temperature for negative coefficient.
- Rear airflow cools panel and heats air.
- Total energy balance is valid.

## Milestone C2 — Solar Air Collector

Deliverables:

- Incident and absorbed solar power
- Dynamic absorber temperature
- Air heating
- Environmental loss
- Pressure drop
- Stagnation behavior
- Unit tests

Acceptance criteria:

- Humidity ratio is unchanged during sensible heating.
- Energy balance is valid.
- Zero-flow stagnation is handled.

## Milestone C3 — Peltier Module

Deliverables:

- Constant-COP model
- Analytical thermoelectric model
- Current/power control
- Hot-side and cold-side temperatures
- Thermal resistances
- Driver loss
- Unit tests

Acceptance criteria:

- \(Q_h=Q_c+P_e\) within tolerance.
- Hot-side overtemperature is detected.
- Off-state conduction is modelled.

## Milestone C4 — Silica-Gel Model

Deliverables:

- Adsorbent state
- Water loading
- Equilibrium loading
- Adsorption/desorption kinetics
- Heat of adsorption
- Regeneration behavior
- Water and energy balances
- Unit tests

Acceptance criteria:

- Water is neither created nor destroyed.
- Outlet humidity is derived from mass balance.
- Desorption cannot exceed stored water.

## Milestone C5 — Condenser

Deliverables:

- Sensible cooling
- Dew-point detection
- Cooling-power limit
- Condensation rate
- Latent heat
- Drain-water output
- Remaining vapor
- Unit tests

Acceptance criteria:

- Condensation never exceeds inlet vapor.
- Latent heat is included.
- Surface temperature and available cooling power constrain output.

## Milestone C6 — Battery and Electrical Network

Deliverables:

- Battery SOC
- Charge/discharge efficiency
- Power limits
- Solar-panel connection
- Fan load
- Peltier load
- Curtailment
- Unit tests

Acceptance criteria:

- SOC remains in valid range.
- Electrical power is conserved.
- Loads cannot consume unavailable power.

## Milestone C7 — AWG V3 Topology

Deliverables:

- AWG configuration builder
- Ambient inlet
- Peltier hot-side heat exchanger
- PV rear channel
- Solar collector
- Silica-gel bed
- Condenser
- Water tank
- Exhaust
- Optional recirculation
- Operating controller

Acceptance criteria:

- Full system runs for at least 24 simulated hours.
- Water, dry-air and energy balances remain within configured tolerances.
- Exhaust vapor loss is reported.
- Recirculation fraction is configurable.

---

# 9. Stage D — Web Product

## Objective

Deliver a browser-accessible simulation product.

## Milestone D1 — ASP.NET Core API

Deliverables:

- `ThermoCore.Api`
- Simulation request contract
- Validation response
- Synchronous small-run endpoint
- Asynchronous simulation job model
- Result endpoint
- OpenAPI documentation
- Health endpoint

Suggested endpoints:

```text
POST /api/simulations
GET  /api/simulations/{id}
GET  /api/simulations/{id}/results
GET  /api/models
POST /api/psychrometrics/calculate
```

Acceptance criteria:

- API does not duplicate physics calculations.
- API DTOs convert user units to Core SI units.
- Invalid inputs return structured validation errors.
- API integration tests pass.

## Milestone D2 — Blazor Web Application

Recommended initial choice:

```text
Blazor Web App
Interactive Server rendering for simulation pages
```

Deliverables:

- Configuration editor
- Weather input editor
- Component parameter forms
- Simulation start and cancel
- Progress indication
- Result summary
- Tables
- Charts
- CSV export
- JSON configuration import/export

Acceptance criteria:

- Browser can configure and run an AWG simulation.
- UI remains responsive during simulation.
- The same configuration produces the same result as Console.
- No physical formula exists in UI code.

## Milestone D3 — Simulation Persistence

Deliverables:

- Database abstraction
- Saved configurations
- Saved simulation runs
- Result metadata
- User-defined names and descriptions
- Run comparison

Initial persistence options:

```text
SQLite for local/self-hosted deployment
PostgreSQL for hosted deployment
```

Acceptance criteria:

- Saved configurations can be rerun.
- Results retain model and version metadata.
- Schema does not store UI-specific computed values as authoritative physics state.

## Milestone D4 — User Accounts and Sharing

Optional after anonymous MVP.

Deliverables:

- Authentication
- User-owned simulations
- Public/private configurations
- Shareable result links
- Quotas
- Data deletion

Acceptance criteria:

- Anonymous mode remains possible if configured.
- Users cannot access private simulations belonging to others.
- Public links expose only approved data.

## Milestone D5 — Deployment

Deliverables:

- Dockerfile
- Container configuration
- Environment-based settings
- Database migrations
- HTTPS
- Logging
- Monitoring
- Backup guidance

Suggested deployment targets:

```text
Azure App Service
Azure Container Apps
Docker host
Kubernetes
Linux VPS
```

Acceptance criteria:

- Application runs on Linux containers.
- No Windows-only dependency exists in Core, AWG, API or Web projects.
- Deployment process is documented.

---

# 10. Stage E — Calibration and Optimization

## Objective

Improve prediction accuracy using measured hardware data.

## Milestone E1 — Measurement Import

Deliverables:

- CSV measurement schema
- Sensor mapping
- Timestamp alignment
- Missing-data handling
- Unit conversion
- Measurement validation

## Milestone E2 — Simulation-to-Measurement Comparison

Deliverables:

- Error metrics
- Time-series comparison
- Bias and RMSE
- Component-level comparison
- Calibration report

## Milestone E3 — Parameter Calibration

Deliverables:

- Parameter bounds
- Objective functions
- Optimization runner
- Reproducible calibrated parameter sets
- Parameter provenance

Initial calibration targets:

```text
Collector loss coefficient
Collector thermal capacity
Silica-gel kinetic coefficient
Silica-gel equilibrium loading
Peltier thermal resistances
Condenser bypass factor
Fan pressure-flow behavior
```

## Milestone E4 — Scenario Optimization

Deliverables:

- Parameter sweeps
- Sensitivity analysis
- Multi-objective optimization
- Comparison dashboard

Possible objectives:

```text
Maximum liters/day
Minimum Wh/liter
Minimum system mass
Minimum cost
Maximum water recovery
Minimum exhaust-vapor loss
Maximum battery autonomy
```

---

# 11. Suggested Release Plan

## Version 0.1 — Psychrometric Calculator

Contains:

- Core setup
- Units and constants
- Psychrometric calculator
- Console demonstration
- Unit tests

Usable result:

```text
Calculate air state, dew point, humidity ratio and enthalpy.
```

## Version 0.2 — Generic Simulation Core

Contains:

- Components
- Ports
- Connections
- Acyclic graph
- Conservation balances
- Time loop

Usable result:

```text
Run simple thermodynamic component chains.
```

## Version 0.3 — Solar and Electrical Components

Contains:

- Solar panel
- Solar collector
- Battery
- Fan
- Basic electrical network

Usable result:

```text
Simulate solar heating and available electrical energy.
```

## Version 0.4 — Peltier and Condenser

Contains:

- Peltier
- Condenser
- Water tank

Usable result:

```text
Estimate direct condensation under configured air conditions.
```

## Version 0.5 — Silica-Gel AWG

Contains:

- Silica gel
- Adsorption
- Regeneration
- Full V3 topology

Usable result:

```text
Estimate cyclic atmospheric water production.
```

## Version 0.6 — Recirculation and Heat Recovery

Contains:

- Mixer
- Splitter
- Cyclic solver
- Recirculation
- Exhaust-loss reporting

Usable result:

```text
Compare open-loop and partially recirculated AWG operation.
```

## Version 0.7 — Web MVP

Contains:

- ASP.NET Core API
- Blazor UI
- Configuration editor
- Charts
- CSV export

Usable result:

```text
Publicly usable browser-based simulation tool.
```

## Version 0.8 — Persistence and Comparison

Contains:

- Saved runs
- Saved configurations
- Run comparison
- Database

## Version 0.9 — Prototype Calibration

Contains:

- Measurement import
- Calibration tools
- Accuracy reports

## Version 1.0 — Validated Public Release

Required conditions:

- Full documentation
- Stable configuration schema
- Reproducible simulations
- Validated conservation balances
- Prototype comparison where data exists
- Deployment documentation
- Security review
- Public web deployment

---

# 12. Priority Classification

## Critical

Required before meaningful AWG results:

- Psychrometrics
- Conservation balances
- Graph execution
- Solar collector
- Peltier
- Silica gel
- Condenser
- Battery
- AWG topology

## High

Required before web MVP:

- API
- Blazor UI
- Configuration validation
- CSV export
- Charts
- Cancellation
- Error reporting

## Medium

Useful after MVP:

- Persistence
- User accounts
- Run comparison
- Parameter sweeps
- Deployment automation

## Low or Future

- WPF desktop client
- Full I–V solar model
- Frost model
- CFD integration
- Machine learning
- Mobile application
- Public component marketplace

---

# 13. Definition of Done

A milestone is complete only when:

1. Code compiles.
2. Tests pass.
3. Public API is documented.
4. Configuration is validated.
5. Physical units are explicit.
6. Balance residuals are available where applicable.
7. Invalid states produce diagnostics.
8. No placeholder formula is presented as validated physics.
9. Example configuration is included.
10. Documentation is updated.
11. Console or integration example proves usability.
12. Web-facing changes include API contract tests where applicable.

---

# 14. AI-Assisted Implementation Workflow

For each milestone, the coding AI shall receive:

```text
Relevant specification documents
Current solution structure
Existing public interfaces
Acceptance criteria
Required tests
Explicit non-goals
```

Recommended workflow:

```text
1. Implement one milestone only.
2. Add or update tests.
3. Build the full solution.
4. Run all tests.
5. Report assumptions.
6. Report deviations from specification.
7. Do not redesign unrelated modules.
8. Do not introduce UI dependencies into Core.
9. Do not proceed to the next milestone until the current one passes.
```

---

# 15. Main Risks

## Physical-model risk

Mitigation:

- Fidelity levels
- Explicit assumptions
- Calibration support
- Sensitivity analysis

## Architecture overengineering

Mitigation:

- Start with acyclic execution
- Add cyclic solver only when needed
- Avoid general DAE solver in MVP
- Keep interfaces small

## Peltier performance risk

Mitigation:

- Power-limited condensation model
- Hot-side thermal-resistance modelling
- Datasheet and prototype calibration

## Silica-gel uncertainty

Mitigation:

- Pluggable isotherm and kinetic models
- Conservative initial estimates
- Measurement-driven calibration

## Web resource usage

Mitigation:

- Server-side execution for long simulations
- Job queue
- Result downsampling
- Simulation limits and quotas
- Cancellation support

## Specification drift

Mitigation:

- Versioned documents
- Architecture decision records
- Configuration schema versions
- Model metadata stored with results

---

# 16. Deferred Decisions

The following decisions are intentionally deferred:

```text
Final database technology
Final authentication provider
Final charting library
Final cloud host
Final optimization algorithm
Final manufacturer Peltier model
Final silica-gel isotherm
Final public pricing model
Whether desktop WPF remains necessary
```

These decisions shall not block the initial Core and Web MVP.

---

# 17. Immediate Development Start

Recommended first implementation sequence:

```text
1. Create solution and projects.
2. Add coding rules and analyzers.
3. Implement constants and unit conversions.
4. Implement saturation pressure.
5. Implement MoistAirState factory.
6. Implement psychrometric unit tests.
7. Implement conservation-balance types.
8. Implement component and port abstractions.
9. Implement a simple sensible-heater component.
10. Run it from ThermoCore.Console.
```

This provides the shortest route to an executable and testable foundation.

---

# 18. Roadmap Completion Criteria

The roadmap is fulfilled when:

- ThermoCore is a reusable platform-independent library.
- ThermoCore.AWG runs the V3 system topology.
- Full water and energy balances are available.
- The simulator is accessible through a web browser.
- Configurations and results can be exported.
- Results are reproducible.
- The model can be calibrated against prototype data.
- Additional thermodynamic applications can reuse the same engine.

---

**End of Document**
