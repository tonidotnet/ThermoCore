# DOCUMENT_INVENTORY.md

**Version:** 1.1  
**Purpose:** Authoritative inventory of the documentation package.

| File | Package state | Quality | Next action |
|---|---|---|---|
| MASTER_INDEX.md | Preserved | Draft navigation | Review links after final placement |
| IMPLEMENTATION_PROGRESS.md | Preserved | Detailed tracker | Update task statuses after bootstrap |
| ARCHITECTURE_MAP.md | Preserved | Draft navigation | Align with `src/` solution layout |
| DOCUMENT_DEPENDENCY_GRAPH.md | Preserved | Draft GPS | Synchronize with JSON graphs |
| 01_ProjectVision.md | Restored from ThermoCoreold (`01_ProjectOverview`) | Draft | Retitle/align with framework vision |
| 02_Architecture.md | Restored from ThermoCoreold (`02_SystemRequirements`) | Draft | Merge with `03_PhysicalArchitecture` reading order |
| 03_PhysicalArchitecture.md | Restored from ThermoCoreold | Draft / implementation-usable | Review before graph coding |
| 04_MathematicalModel.md | Restored from ThermoCoreold (unescaped) | Draft / implementation-usable | Review before balance types |
| 05_Psychrometrics.md | Restored from ThermoCoreold (unescaped) | Draft / implementation-usable | First Core physics milestone |
| 06_SolarCollector.md | Restored from ThermoCoreold (unescaped) | Draft / implementation-usable | Before collector coding |
| 07_SolarPanel.md | Preserved | Detailed | Review only |
| 08_Peltier.md | Preserved | Detailed | Review only |
| 09_SilicaGel.md | Preserved | Detailed | Review references |
| 10_Condenser.md | Repaired | Implementation-ready draft | Review against psychrometric core |
| 11_HeatRecovery.md | Repaired | Implementation-ready draft | Review condensation extension |
| 12_BatteryAndPowerManagement.md | Repaired | Implementation-ready draft | Finalize battery chemistry abstraction |
| 13_FanAndAirflow.md | Repaired | Implementation-ready draft | Finalize fan curve data model |
| 14_ControlSystem.md | Preserved | ReadyForImplementation | Before AWG control coding |
| 15_SystemTopology.md | Preserved | ReadyForImplementation | Before AWG integration |
| 16_SimulationEngine.md | Preserved | ReadyForImplementation | Before engine implementation |
| 17_Roadmap.md | Preserved | Detailed | Update after milestones |
| 18_CodingRules.md | Preserved | Detailed | Use as mandatory AI rule set |
| 19–21, 29–30 Product docs | Preserved | ReadyForImplementation | After engine contract |
| 22_TestStrategy.md | Preserved | ReadyForImplementation | Enforce for all coding |
| 23_Calibration.md | Implemented | MVP import/align/fit/provenance | Keep |
| 24_Optimization.md | Implemented | Sweep/sensitivity/Pareto MVP | Keep |
| 25_NumericalMethods.md | Repaired | ReadyForImplementation | In use by Core.Numerics |
| 26_Constants.md | New | ReadyForImplementation | In use by Core.Physics |
| 27_Units.md | New | ReadyForImplementation | In use by Core.Units |
| 28_WeatherModel.md | Preserved | ReadyForImplementation | Before 24-hour simulation |

## Status interpretation

- **Preserved:** copied from an existing generated file.
- **Repaired:** a short outline was replaced by a substantially expanded specification.
- **New:** generated as part of this package.
- **Restored from ThermoCoreold:** placeholder replaced with the earlier detailed local document.
- **Planned:** intentionally contains no implementation contract yet.

## Important rule

Do not treat a file marked `Planned` as implementation-ready.
