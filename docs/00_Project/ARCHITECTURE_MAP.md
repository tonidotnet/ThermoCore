# ThermoCore
# ARCHITECTURE_MAP.md

**Version:** 1.0  
**Status:** Active Architecture Map

# Purpose

This document is the architectural map of the ThermoCore ecosystem. It describes the projects, dependencies, responsibilities and boundaries of every assembly.

---

# Solution Structure

```text
ThermoCore.sln

src/
├── ThermoCore.Core
├── ThermoCore.AWG
├── ThermoCore.Console
├── ThermoCore.Api
├── ThermoCore.Web
└── ThermoCore.Desktop (optional)

tests/
├── ThermoCore.Core.Tests
├── ThermoCore.AWG.Tests
├── ThermoCore.Api.Tests
└── ThermoCore.Web.Tests

docs/
├── MASTER_INDEX.md
├── ARCHITECTURE_MAP.md
├── DOCUMENT_DEPENDENCY_GRAPH.md
├── IMPLEMENTATION_PROGRESS.md
└── Engineering/...
```

# Layered Architecture

```text
+-------------------------------+
| Blazor / Console / Desktop    |
+---------------▲---------------+
                |
+---------------+---------------+
| ASP.NET Core API              |
+---------------▲---------------+
                |
+---------------+---------------+
| ThermoCore.AWG               |
+---------------▲---------------+
                |
+---------------+---------------+
| ThermoCore.Core              |
+-------------------------------+
```

Only upper layers may depend on lower layers.

---

# Project Responsibilities

## ThermoCore.Core

Contains:

- Physics engine
- Simulation graph
- Psychrometrics
- Numerical methods
- Ports
- Conservation balances
- Diagnostics
- Common abstractions

Must **never** reference:

- ASP.NET
- Blazor
- WPF
- Databases
- UI frameworks

---

## ThermoCore.AWG

Contains the first implementation of ThermoCore.

Modules:

- Solar Collector
- Solar Panel
- Peltier
- Silica Gel
- Condenser
- Heat Recovery
- Battery
- Fan & Airflow
- Controller

Depends only on:

```text
ThermoCore.Core
```

---

## ThermoCore.Api

Responsibilities:

- REST API
- Validation
- DTO mapping
- Authentication (future)
- Simulation orchestration

Must not duplicate physics.

---

## ThermoCore.Web

Responsibilities:

- Blazor UI
- Configuration editor
- Charts
- Result visualization
- Import / Export

Must consume Core only through the Application/API layer.

---

## ThermoCore.Console

Reference implementation.

Purposes:

- Regression testing
- Batch simulations
- Benchmarking
- Example scenarios

---

# Dependency Rules

Allowed:

```text
Console → AWG → Core
API     → AWG → Core
Web     → API/Application → AWG → Core
```

Forbidden:

```text
Core → Web
Core → API
AWG → Web
Web → Core (direct physics duplication)
```

---

# Cross-Cutting Concerns

Shared across all layers:

- Logging
- Diagnostics
- Units
- Configuration schema
- Version metadata
- Serialization contracts

---

# Namespace Guidelines

```text
ThermoCore.Core.*
ThermoCore.AWG.*
ThermoCore.Api.*
ThermoCore.Web.*
```

Namespaces should follow feature folders.

---

# Data Flow

```text
Weather
    │
Configuration
    │
Simulation Request
    │
Simulation Graph
    │
Component Evaluation
    │
Results
    │
API DTO
    │
Charts / CSV / JSON
```

---

# Component Graph

```text
Ambient
   ↓
Peltier Hot Side
   ↓
PV Rear Channel
   ↓
Solar Collector
   ↓
Silica Gel
   ↓
Condenser
   ↓
Heat Recovery
   ↓
Recirculation / Exhaust
```

Electrical subsystem:

```text
Solar Panel
   ↓
MPPT
   ↓
Battery
   ├── Fans
   ├── Peltier
   └── Controller
```

---

# Design Principles

- SI units internally
- Immutable state
- Deterministic simulation
- Explicit conservation
- Replaceable physical models
- Platform-independent Core
- Web-first product

---

# Future Extension Points

Possible reusable packages:

- ThermoCore.HVAC
- ThermoCore.HeatPump
- ThermoCore.Refrigeration
- ThermoCore.SolarThermal
- ThermoCore.Storage

All shall reuse ThermoCore.Core.

---

# Acceptance Criteria

- Clear project boundaries
- No cyclic dependencies
- Single source of physical calculations
- Replaceable component models
- Architecture supports additional thermodynamic products without Core redesign

**End of Document**
