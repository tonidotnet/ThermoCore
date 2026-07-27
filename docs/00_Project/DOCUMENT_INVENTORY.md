# DOCUMENT_INVENTORY.md

**Version:** 1.0  
**Purpose:** Authoritative inventory of the documentation package.

| File | Package state | Quality | Next action |
|---|---|---|---|
| MASTER_INDEX.md | Preserved | Draft navigation | Review links after final placement |
| IMPLEMENTATION_PROGRESS.md | Preserved | Detailed tracker | Update repaired document statuses |
| ARCHITECTURE_MAP.md | Preserved | Draft navigation | Review assembly decisions |
| DOCUMENT_DEPENDENCY_GRAPH.md | Preserved | Draft GPS | Synchronize with JSON graphs |
| 04_MathematicalModel.md | Local replacement required | Previously detailed | Copy earlier downloaded file |
| 05_Psychrometrics.md | Local replacement required | Previously detailed | Copy earlier downloaded file |
| 06_SolarCollector.md | Local replacement required | Previously detailed | Copy earlier downloaded file |
| 07_SolarPanel.md | Preserved | Detailed | Review only |
| 08_Peltier.md | Preserved | Detailed | Review only |
| 09_SilicaGel.md | Preserved | Detailed | Review references |
| 10_Condenser.md | Repaired | Implementation-ready draft | Review against psychrometric core |
| 11_HeatRecovery.md | Repaired | Implementation-ready draft | Review condensation extension |
| 12_BatteryAndPowerManagement.md | Repaired | Implementation-ready draft | Finalize battery chemistry abstraction |
| 13_FanAndAirflow.md | Repaired | Implementation-ready draft | Finalize fan curve data model |
| 17_Roadmap.md | Preserved | Detailed | Update after grouped docs |
| 18_CodingRules.md | Preserved | Detailed | Use as mandatory AI rule set |
| 25_NumericalMethods.md | Repaired | Implementation-ready draft | Validate default tolerances |
| 26_Constants.md | New | Implementation-ready draft | Add exact source citations later |
| 27_Units.md | New | Implementation-ready draft | Use before API design |

## Status interpretation

- **Preserved:** copied from an existing generated file.
- **Repaired:** a short outline was replaced by a substantially expanded specification.
- **New:** generated as part of this package.
- **Local replacement required:** the earlier detailed document was not present in the active runtime, so this package contains a clearly marked placeholder.
- **Planned:** intentionally contains no implementation contract yet.

## Important rule

Do not treat a file marked `Planned` or `Local replacement required` as implementation-ready.
