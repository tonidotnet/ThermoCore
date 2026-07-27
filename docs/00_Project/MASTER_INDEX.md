# ThermoCore
# MASTER_INDEX.md

> **Single Source of Truth for the ThermoCore Documentation**

Version: 1.0  
Status: Active  
Project: ThermoCore Framework + ThermoCore.AWG

---

# 1. Purpose

This document is the entry point for the entire ThermoCore documentation.

It provides:

- Documentation map
- Reading order
- Module dependencies
- Development order
- Document status
- Cross references
- Architecture overview

Every new document should be registered here.

---

# 2. Project Vision

ThermoCore is a reusable thermodynamic simulation framework.

The first implementation is **ThermoCore.AWG**, a portable Atmospheric Water Generator simulator.

Future reusable applications may include:

- Heat pumps
- HVAC systems
- Solar thermal systems
- Energy storage
- Refrigeration
- Drying systems
- Industrial process simulations

---

# 3. Documentation Structure

```text
docs/
│
├── MASTER_INDEX.md
│
├── Architecture/
├── Engineering/
│   ├── Physics/
│   ├── Components/
│   ├── Mathematics/
│   ├── Control/
│   ├── Simulation/
│   ├── Electrical/
│   └── Validation/
│
├── Specification/
│
├── Product/
│   ├── API/
│   ├── Web/
│   ├── Deployment/
│   └── Examples/
│
└── ADR/
```

---

# 4. Reading Order

1. Roadmap
2. Coding Rules
3. Mathematical Model
4. Psychrometrics
5. Solar Collector
6. Solar Panel
7. Peltier
8. Silica Gel
9. Condenser
10. Heat Recovery
11. Battery & Power
12. Fan & Airflow
13. Control System
14. Simulation Engine
15. Web API
16. Blazor
17. Calibration
18. Optimization
19. Deployment

---

# 5. Current Document Inventory

| ID | Document | Status |
|----|----------|--------|
|04|Mathematical Model|Planned|
|05|Psychrometrics|Available|
|06|SolarCollector|Available|
|07|SolarPanel|Available|
|08|Peltier|Available|
|09|SilicaGel|Available|
|10|Condenser|Available|
|11|HeatRecovery|Available|
|12|BatteryAndPowerManagement|Available|
|13|FanAndAirflow|Available|
|14|ControlSystem|Planned|
|15|SystemTopology|Planned|
|16|SimulationEngine|Planned|
|17|Roadmap|Available|
|18|CodingRules|Available|
|19|WebApi|Planned|
|20|BlazorWeb|Planned|
|21|DataModel|Planned|
|22|TestStrategy|Planned|
|23|Calibration|Planned|
|24|Optimization|Planned|
|25|NumericalMethods|Planned|
|26|Constants|Planned|
|27|Units|Planned|
|28|WeatherModel|Planned|
|29|ResultFormats|Planned|
|30|Deployment|Planned|

---

# 6. Component Dependency Graph

```text
Ambient Air
      │
      ▼
Solar Panel
      │
      ▼
PV Rear Air Channel
      │
      ▼
Solar Collector
      │
      ▼
Silica Gel
      │
      ▼
Condenser
      │
      ▼
Heat Recovery
      │
      ▼
Recirculation / Exhaust
```

Supporting systems:

```text
Battery
 ├── Fans
 ├── Peltier
 └── Controller
```

---

# 7. Architecture Layers

```text
Presentation
    │
Blazor / Console
    │
API
    │
ThermoCore.AWG
    │
ThermoCore.Core
```

Only lower layers may be referenced.

---

# 8. Module Status Legend

- Planned
- Draft
- Available
- Implemented
- Tested
- Validated

Update this table as development progresses.

---

# 9. Development Workflow

For every module:

1. Requirement
2. Engineering specification
3. Mathematical model
4. Interfaces
5. Unit tests
6. Integration tests
7. Implementation
8. Validation
9. Documentation update

---

# 10. Architecture Decision Records

Store significant decisions in:

```text
docs/ADR/
```

Examples:

- ADR-001 Port-based simulation graph
- ADR-002 SI units only
- ADR-003 Web-first architecture
- ADR-004 ThermoCore as generic framework

---

# 11. AI Development Workflow

Before implementing any module, the coding AI should read:

1. MASTER_INDEX.md
2. Roadmap
3. CodingRules
4. Relevant Engineering document(s)
5. Mathematical references
6. Existing interfaces

After implementation it should:

- Run tests
- Update documentation
- Record assumptions
- Update module status

---

# 12. Naming Convention

Documents:

```text
NN_Name.md
```

Namespaces:

```text
ThermoCore.Core
ThermoCore.AWG
ThermoCore.Api
ThermoCore.Web
```

Units:

- SI internally
- Conversion only at boundaries

---

# 13. Long-Term Vision

ThermoCore should evolve into a reusable engineering simulation platform where new thermodynamic components can be assembled as reusable building blocks.

AWG is the reference implementation—not the limit of the framework.

---

# 14. Maintenance Rules

Whenever a new document is added:

- Register it here.
- Define dependencies.
- Define status.
- Link related documents.
- Update reading order if required.

Whenever a document changes significantly:

- Increment version.
- Record breaking changes.
- Verify links.

---

# 15. Success Criteria

The documentation is considered complete when:

- Every component has an engineering specification.
- Every mathematical model is documented.
- Every public interface is defined.
- Every module has acceptance criteria.
- Cross references are complete.
- An AI developer can implement the framework without relying on chat history.

---

End of Document
