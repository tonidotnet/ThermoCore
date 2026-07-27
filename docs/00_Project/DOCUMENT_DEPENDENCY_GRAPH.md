# ThermoCore
# DOCUMENT_DEPENDENCY_GRAPH.md

Version: 1.0
Status: Architecture Navigation ("Project GPS")

## Purpose

This document is the navigation hub of the ThermoCore ecosystem.
It describes **what depends on what**, **what should be implemented first**,
and **which documents an AI or developer must read** before working on a module.

---

# 1. Dependency Layers

Level 0
- MASTER_INDEX
- Roadmap
- CodingRules

Level 1 – Foundation
- MathematicalModel
- Psychrometrics
- Constants
- Units
- NumericalMethods

Level 2 – Generic Core
- MoistAir
- EnergyBalance
- MassBalance
- Simulation Engine

Level 3 – Components
- SolarCollector
- SolarPanel
- Peltier
- SilicaGel
- Condenser
- HeatRecovery
- Fan
- Battery

Level 4 – System
- ControlSystem
- SystemTopology
- WeatherModel

Level 5 – Product
- WebApi
- Blazor
- Deployment

---

# 2. Component Dependency Matrix

| Component | Depends On |
|-----------|------------|
| SolarCollector | MathematicalModel, Psychrometrics |
| SolarPanel | Constants |
| Peltier | Psychrometrics, NumericalMethods |
| SilicaGel | Psychrometrics, MathematicalModel, Constants |
| Condenser | SilicaGel, Peltier, Psychrometrics |
| HeatRecovery | Condenser |
| Fan | Constants |
| Battery | Constants |
| Controller | All runtime components |

---

# 3. Runtime Execution Order

1. Weather
2. Solar Panel
3. Battery
4. Fans
5. Peltier
6. PV Rear Channel
7. Solar Collector
8. Silica Gel
9. Condenser
10. Heat Recovery
11. Recirculation
12. Results

---

# 4. AI Reading Paths

## Implementing Silica Gel

Read:
1. MASTER_INDEX
2. CodingRules
3. MathematicalModel
4. Psychrometrics
5. NumericalMethods
6. Constants
7. SilicaGel

## Implementing Condenser

Read:
1. Psychrometrics
2. Peltier
3. SilicaGel
4. Condenser

## Implementing Web

Read:
1. MASTER_INDEX
2. ArchitectureMap
3. WebApi
4. Blazor
5. ResultFormats

---

# 5. Validation Flow

Physics
→ Unit Tests
→ Integration Tests
→ Full AWG Simulation
→ Calibration
→ Optimization
→ Release

---

# 6. Traceability

Requirement
→ Engineering Document
→ Mathematical Model
→ Interface
→ Implementation
→ Unit Test
→ Integration Test
→ Validation

Every public feature shall be traceable through this chain.

---

# 7. Assembly Dependencies

ThermoCore.Core
↑
ThermoCore.AWG
↑
ThermoCore.Api
↑
ThermoCore.Web

Console may reference AWG directly.

---

# 8. Document Status

Planned
Draft
Available
Implemented
Validated

---

# 9. Future Machine-readable Graph

Maintain a matching DOCUMENT_DEPENDENCY_GRAPH.json
to allow automated tooling, graph visualization and AI navigation.

---

# 10. Maintenance Rules

Whenever a new component or document is added:
- Register it here.
- Define dependencies.
- Define implementation order.
- Define validation order.
- Update MASTER_INDEX.

End of Document
