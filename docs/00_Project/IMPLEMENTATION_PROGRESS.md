# ThermoCore
# IMPLEMENTATION_PROGRESS.md

**Version:** 1.0  
**Status:** Active  
**Purpose:** Human- and AI-readable implementation tracker  
**Applies To:** ThermoCore.Core, ThermoCore.AWG, ThermoCore.Api, ThermoCore.Web, documentation and AI workspace

---

# 1. Purpose

This document is the official implementation tracker for the ThermoCore project.

It shall be used to:

- Track implementation progress
- Select the next development task
- Record task dependencies
- Link tasks to specifications
- Track test readiness
- Track documentation readiness
- Prevent parallel work from violating dependency order
- Provide an AI coding assistant with a clear task queue
- Keep the project aligned with the roadmap

This file is not a replacement for issue tracking, but it is the canonical project-level progress summary.

---

# 2. Status Values

Use exactly one of the following status values.

| Status | Meaning |
|---|---|
| `Planned` | Defined but not ready to start |
| `Blocked` | Cannot start because a dependency is incomplete |
| `Ready` | All required dependencies are available |
| `InProgress` | Currently being implemented |
| `Review` | Implementation is complete and awaiting review |
| `Testing` | Functional implementation exists and tests are running |
| `Done` | Implementation, tests and documentation are complete |
| `Validated` | Verified against trusted reference data or prototype measurements |
| `Deferred` | Intentionally postponed |
| `Cancelled` | No longer planned |

---

# 3. Priority Values

| Priority | Meaning |
|---|---|
| `P0` | Critical path; blocks most later work |
| `P1` | Required for the first usable engineering release |
| `P2` | Required for the web MVP or reliable product use |
| `P3` | Useful after MVP |
| `P4` | Future or optional extension |

---

# 4. Test Status Values

| Test status | Meaning |
|---|---|
| `NotStarted` | No tests exist |
| `Planned` | Test cases are specified |
| `Partial` | Some tests exist |
| `Passing` | Required tests pass |
| `Validated` | Tests also match trusted external reference data |
| `NotApplicable` | No executable code is involved |

---

# 5. Documentation Status Values

| Documentation status | Meaning |
|---|---|
| `Missing` | No document exists |
| `Outline` | Only a short outline exists |
| `Draft` | Detailed draft exists |
| `ReadyForImplementation` | Sufficient for coding |
| `Implemented` | Updated to match implementation |
| `Validated` | Updated after physical or external validation |

---

# 6. Progress Summary

## Current project phase

```text
Phase 1 — ThermoCore.Core foundation (Milestone A2/A3)
```

## Current highest-priority objective

```text
Continue solar-collector fidelity (dynamic absorber balance), then remaining P1 gaps and power-management integration tests.
```

## Recommended next tasks

1. `SC-003` — Dynamic absorber energy balance
2. `PWR-007` — Power-management integration tests
3. `PV-002` — Temperature-corrected PV model
4. `GEN-003` — Electrical source
5. `TEC-005` — Dynamic hot/cold-side state

---

# 7. Global Progress Dashboard

| Area | Total | Done | In progress | Ready | Blocked/Planned |
|---|---:|---:|---:|---:|---:|
| Documentation foundations | 12 | 6 | 0 | 4 | 2 |
| AI workspace | 10 | 9 | 0 | 1 | 0 |
| Repository setup | 12 | 0 | 0 | 3 | 9 |
| ThermoCore.Core | 20 | 0 | 0 | 2 | 18 |
| Physical components | 38 | 0 | 0 | 0 | 38 |
| ThermoCore.AWG | 19 | 0 | 0 | 0 | 19 |
| API and Web | 22 | 0 | 0 | 0 | 22 |
| Validation and optimization | 15 | 0 | 0 | 0 | 15 |

The counts are indicative and shall be updated when tasks are added, split or completed.

---

# 8. Documentation and Navigation Tasks

| ID | Task | Priority | Status | Dependencies | Related file | Doc status | Test status | Notes |
|---|---|---|---|---|---|---|---|---|
| DOC-001 | Maintain documentation master index | P1 | Done | None | `MASTER_INDEX.md` | Draft | NotApplicable | Update whenever a document is added or moved |
| DOC-002 | Maintain architecture map | P1 | Done | DOC-001 | `ARCHITECTURE_MAP.md` | Draft | NotApplicable | Must reflect actual project dependencies |
| DOC-003 | Maintain document dependency graph | P1 | Done | DOC-001, DOC-002 | `DOCUMENT_DEPENDENCY_GRAPH.md` | Draft | NotApplicable | Update with implementation order |
| DOC-004 | Maintain implementation tracker | P0 | Done | DOC-001, DOC-002, DOC-003 | `IMPLEMENTATION_PROGRESS.md` | Draft | NotApplicable | This document |
| DOC-005 | Define repository and folder structure | P0 | Ready | DOC-001, DOC-002 | Repository root | Missing | NotApplicable | Use monorepo initially |
| DOC-006 | Move existing documents into final folders | P1 | Blocked | DOC-005 | `docs/` | Missing | NotApplicable | Do not regenerate existing files |
| DOC-007 | Add standard document metadata header | P2 | Planned | DOC-006 | All documentation | Missing | NotApplicable | Prefer additive metadata, no full rewrite |
| DOC-008 | Validate internal document links | P2 | Blocked | DOC-006 | All documentation | Missing | NotApplicable | Can later be automated in CI |
| DOC-009 | Create implementation-ready document template | P1 | Ready | DOC-007 | `ai/templates/EngineeringDocumentTemplate.md` | Missing | NotApplicable | Used for all new engineering specs |
| DOC-010 | Create architecture decision record template | P1 | Ready | DOC-005 | `docs/ADR/ADR_TEMPLATE.md` | Missing | NotApplicable | Required before major coding decisions |
| DOC-011 | Create document status automation concept | P3 | Planned | DOC-006, DOC-008 | `tools/` | Missing | NotApplicable | Optional script reads front matter |
| DOC-012 | Create MkDocs navigation map | P2 | Blocked | DOC-006 | `mkdocs.yml` | Missing | NotApplicable | Needed for documentation portal |

---

# 9. Existing Engineering Documentation Status

| ID | Document | Priority | Status | Documentation status | Required before coding? | Action |
|---|---|---|---|---|---|---|
| DOC-003A | `03_PhysicalArchitecture.md` | P0 | Done | ReadyForImplementation | Yes | Preserve and place under Architecture/Engineering |
| DOC-004A | `04_MathematicalModel.md` | P0 | Done | ReadyForImplementation | Yes | Review references before final implementation |
| DOC-005A | `05_Psychrometrics.md` | P0 | Done | ReadyForImplementation | Yes | Suitable for first Core milestone |
| DOC-006A | `06_SolarCollector.md` | P1 | Done | ReadyForImplementation | Before collector coding | Keep current detailed version |
| DOC-007A | `07_SolarPanel.md` | P1 | Done | ReadyForImplementation | Before panel coding | Keep current detailed version |
| DOC-008A | `08_Peltier.md` | P1 | Done | ReadyForImplementation | Before Peltier coding | Keep current detailed version |
| DOC-009A | `09_SilicaGel.md` | P1 | Done | ReadyForImplementation | Before silica-gel coding | Keep current detailed version |
| DOC-010A | `10_Condenser.md` | P1 | Ready | Outline | Yes | Expand to same level as 08/09 |
| DOC-011A | `11_HeatRecovery.md` | P1 | Ready | Outline | Before heat-recovery coding | Expand |
| DOC-012A | `12_BatteryAndPowerManagement.md` | P1 | Ready | Outline | Before battery coding | Expand |
| DOC-013A | `13_FanAndAirflow.md` | P1 | Ready | Outline | Before airflow coding | Expand |
| DOC-014A | `14_ControlSystem.md` | P1 | Planned | Missing | Before AWG control coding | Create detailed spec |
| DOC-015A | `15_SystemTopology.md` | P1 | Planned | Missing | Before AWG integration | Create detailed spec |
| DOC-016A | `16_SimulationEngine.md` | P0 | Planned | Missing | Before engine implementation | Create detailed spec |
| DOC-017A | `17_Roadmap.md` | P0 | Done | ReadyForImplementation | Yes | Maintain |
| DOC-018A | `18_CodingRules.md` | P0 | Done | ReadyForImplementation | Yes | Enforce in repository |
| DOC-019A | `19_WebApi.md` | P2 | Planned | Missing | Before API implementation | Create after engine contract stabilizes |
| DOC-020A | `20_BlazorWeb.md` | P2 | Planned | Missing | Before Web implementation | Create after API |
| DOC-021A | `21_DataModel.md` | P2 | Planned | Missing | Before persistence | Create after result model |
| DOC-022 | `22_TestStrategy.md` | P0 | Ready | Missing | Yes | One of next documents |
| DOC-023A | `23_Calibration.md` | P3 | Planned | Missing | Before calibration tooling | Later |
| DOC-024A | `24_Optimization.md` | P3 | Planned | Missing | Before optimization | Later |
| DOC-025 | `25_NumericalMethods.md` | P0 | Ready | Missing | Yes | Highest-priority next document |
| DOC-026 | `26_Constants.md` | P0 | Ready | Missing | Yes | Required before physics coding |
| DOC-027 | `27_Units.md` | P0 | Ready | Missing | Yes | Required before API and Core types |
| DOC-028A | `28_WeatherModel.md` | P2 | Planned | Missing | Before 24-hour simulation | Later |
| DOC-029A | `29_ResultFormats.md` | P2 | Planned | Missing | Before API/export | Later |
| DOC-030A | `30_Deployment.md` | P3 | Planned | Missing | Before public deployment | Later |

---

# 10. AI Workspace Tasks

| ID | Task | Priority | Status | Dependencies | File or folder | Notes |
|---|---|---|---|---|---|---|
| AI-001 | Create AI context | P0 | Done | DOC-001, DOC-002 | `ai/AI_CONTEXT.md` | Canonical AI system context |
| AI-002 | Create prompt guide | P1 | Done | AI-001 | `ai/PROMPT_GUIDE.md` | General usage rules |
| AI-003 | Create glossary | P1 | Done | DOC-004A, DOC-005A | `ai/GLOSSARY.md` | Needs expansion during implementation |
| AI-004 | Create decision summary | P1 | Done | DOC-002 | `ai/DECISIONS.md` | Replace summary entries with full ADRs over time |
| AI-005 | Create implementation playbook | P1 | Done | DOC-017A, DOC-018A | `ai/IMPLEMENTATION_PLAYBOOK.md` | Used for coding workflow |
| AI-006 | Create AI review checklist | P1 | Done | DOC-018A | `ai/AI_REVIEW_CHECKLIST.md` | Used before accepting generated code |
| AI-007 | Create component graph JSON | P1 | Done | DOC-003 | `ai/graphs/COMPONENT_GRAPH.json` | Maintain with code |
| AI-008 | Create document graph JSON | P1 | Done | DOC-003 | `ai/graphs/DOCUMENT_GRAPH.json` | Maintain with docs |
| AI-009 | Create implementation graph JSON | P1 | Done | DOC-017A | `ai/graphs/IMPLEMENTATION_GRAPH.json` | Maintain with milestones |
| AI-010 | Create reusable coding prompts and templates | P1 | Ready | AI-001 to AI-009 | `ai/prompts/`, `ai/templates/`, `ai/agents/` | Next AI workspace expansion |

---

# 11. Repository Setup Tasks

| ID | Task | Priority | Status | Dependencies | Output | Acceptance criteria |
|---|---|---|---|---|---|---|
| REP-001 | Create monorepository | P0 | Done | DOC-005 | Git repository | Clean initial branch |
| REP-002 | Add root README | P1 | Done | REP-001 | `README.md` | Explains ThermoCore and AWG reference app |
| REP-003 | Select and add license | P1 | Planned | REP-001 | `LICENSE` | License decision documented in ADR |
| REP-004 | Add contribution guide | P2 | Planned | REP-002, REP-003 | `CONTRIBUTING.md` | Includes development workflow |
| REP-005 | Add code of conduct | P2 | Planned | REP-001 | `CODE_OF_CONDUCT.md` | Standard open-source policy |
| REP-006 | Add security policy | P2 | Planned | REP-001 | `SECURITY.md` | Vulnerability reporting |
| REP-007 | Add change log | P2 | Planned | REP-001 | `CHANGELOG.md` | Keep a release history |
| REP-008 | Add `.gitignore` | P0 | Done | REP-001 | `.gitignore` | .NET, IDE and OS exclusions |
| REP-009 | Add `.editorconfig` | P0 | Done | REP-001, DOC-018A | `.editorconfig` | Matches coding rules |
| REP-010 | Add issue templates | P3 | Planned | REP-004 | `.github/ISSUE_TEMPLATE/` | Bug, feature, model validation |
| REP-011 | Add pull-request template | P2 | Planned | REP-004 | `.github/pull_request_template.md` | Includes test and balance checklist |
| REP-012 | Add CI workflow | P1 | Planned | DEV-001 | `.github/workflows/build.yml` | Build, test and formatting checks |

---

# 12. Solution Bootstrap Tasks

| ID | Task | Priority | Status | Dependencies | Project | Documentation | Test status |
|---|---|---|---|---|---|---|---|
| DEV-001 | Create solution and base projects | P0 | Done | DOC-025, DOC-026, DOC-027, REP-001 | All | DOC-017A, DOC-018A | Passing |
| DEV-002 | Configure nullable and warning policies | P0 | Done | DEV-001 | All | DOC-018A | Passing |
| DEV-003 | Configure analyzers and formatting | P1 | Review | DEV-001, REP-009 | All | DOC-018A | Partial |
| DEV-004 | Add package version strategy | P2 | Blocked | DEV-001 | All | DOC-018A | NotStarted |
| DEV-005 | Create Core namespace structure | P1 | Done | DEV-001 | `ThermoCore.Core` | DOC-002, DOC-018A | Passing |
| DEV-006 | Create AWG namespace structure | P1 | Done | DEV-001 | `ThermoCore.AWG` | DOC-002 | Passing |
| DEV-007 | Create test project structure | P0 | Done | DEV-001, DOC-022 | Tests | DOC-022 | Passing |
| DEV-008 | Add architecture boundary tests | P1 | Blocked | DEV-001, DEV-005, DEV-006 | Tests | DOC-002, DOC-018A | NotStarted |

---

# 13. ThermoCore.Core Foundation Tasks

| ID | Task | Priority | Status | Dependencies | Related docs | Test status | Completion criteria |
|---|---|---|---|---|---|---|---|
| CORE-001 | Implement unit conversions | P0 | Done | DEV-001, DOC-027 | `27_Units.md` | Passing | All boundary conversions tested |
| CORE-002 | Implement physical constants | P0 | Done | DEV-001, DOC-026 | `26_Constants.md` | Passing | Source and unit for each constant |
| CORE-003 | Implement finite-number validation | P0 | Done | DEV-001 | `18_CodingRules.md` | Passing | NaN/Infinity rejected |
| CORE-004 | Implement numeric tolerances | P0 | Done | CORE-001, DOC-025 | `25_NumericalMethods.md` | Passing | Central tolerance model |
| CORE-005 | Implement saturation-pressure provider | P0 | Done | CORE-001 to CORE-004 | `05_Psychrometrics.md` | Passing | Reference cases pass |
| CORE-006 | Implement vapor pressure and humidity ratio | P0 | Done | CORE-005 | `05_Psychrometrics.md` | Passing | Round-trip tests pass |
| CORE-007 | Implement dew-point calculation | P0 | Done | CORE-005, CORE-004 | `05_Psychrometrics.md`, `25_NumericalMethods.md` | Passing | Forward/inverse consistency |
| CORE-008 | Implement moist-air enthalpy | P0 | Done | CORE-002, CORE-006 | `05_Psychrometrics.md` | Passing | Reference-state consistency |
| CORE-009 | Implement specific volume and density | P0 | Done | CORE-002, CORE-006 | `05_Psychrometrics.md` | Passing | Reference cases pass |
| CORE-010 | Implement immutable `MoistAirState` factory | P0 | Done | CORE-005 to CORE-009 | `05_Psychrometrics.md` | Passing | Inconsistent states impossible |
| CORE-011 | Implement moist-air state validation | P0 | Done | CORE-010 | `05_Psychrometrics.md`, `18_CodingRules.md` | Passing | Supersaturation detected |
| CORE-012 | Implement conservation-balance types | P0 | Done | CORE-001, CORE-002 | `04_MathematicalModel.md` | Passing | Energy, water and dry-air residuals |
| CORE-013 | Implement diagnostic model | P0 | Done | DEV-001 | `03_PhysicalArchitecture.md`, `18_CodingRules.md` | Passing | Structured diagnostics |
| CORE-014 | Implement balance validation | P0 | Done | CORE-004, CORE-012, CORE-013 | `04_MathematicalModel.md` | Passing | Absolute and relative tolerances |
| CORE-015 | Add psychrometric reference test suite | P0 | Done | CORE-005 to CORE-011, DOC-022 | `05_Psychrometrics.md`, `22_TestStrategy.md` | Passing | Selected references pass |
| CORE-016 | Add deterministic execution tests | P1 | Done | CORE-010, DOC-022 | `18_CodingRules.md` | Passing | Repeated runs identical |

---

# 14. Port and Graph Architecture Tasks

| ID | Task | Priority | Status | Dependencies | Related docs | Test status |
|---|---|---|---|---|---|---|
| GRAPH-001 | Define physical domains | P0 | Done | CORE-012 | `03_PhysicalArchitecture.md` | Passing |
| GRAPH-002 | Define typed port abstractions | P0 | Done | GRAPH-001 | `03_PhysicalArchitecture.md` | Passing |
| GRAPH-003 | Define connection model | P0 | Done | GRAPH-002 | `03_PhysicalArchitecture.md` | Passing |
| GRAPH-004 | Define simulation component interface | P0 | Done | GRAPH-002, CORE-013 | `03_PhysicalArchitecture.md` | Passing |
| GRAPH-005 | Implement Evaluate/Commit lifecycle | P0 | Done | GRAPH-004 | `03_PhysicalArchitecture.md`, `18_CodingRules.md` | Passing |
| GRAPH-006 | Implement graph validation | P0 | Done | GRAPH-003, GRAPH-004 | `03_PhysicalArchitecture.md` | Passing |
| GRAPH-007 | Implement topological sorting | P0 | Done | GRAPH-006, DOC-025 | `16_SimulationEngine.md` | Passing |
| GRAPH-008 | Implement acyclic graph execution | P0 | Done | GRAPH-005, GRAPH-007 | `16_SimulationEngine.md` | Passing |
| GRAPH-009 | Implement timestep context | P0 | Done | GRAPH-008 | `16_SimulationEngine.md` | Passing |
| GRAPH-010 | Implement result collection | P1 | Done | GRAPH-008 | `16_SimulationEngine.md`, `29_ResultFormats.md` | Passing |
| GRAPH-011 | Implement cancellation support | P1 | Done | GRAPH-008 | `18_CodingRules.md` | Passing |
| GRAPH-012 | Implement progress reporting | P2 | Done | GRAPH-008 | `16_SimulationEngine.md` | Passing |
| GRAPH-013 | Implement cyclic graph detection | P1 | Done | GRAPH-006 | `16_SimulationEngine.md` | Passing |
| GRAPH-014 | Implement fixed-point loop solver | P1 | Done | GRAPH-013, DOC-025 | `25_NumericalMethods.md`, `16_SimulationEngine.md` | Passing |
| GRAPH-015 | Implement relaxation and convergence diagnostics | P1 | Done | GRAPH-014, CORE-004 | `25_NumericalMethods.md` | Passing |

---

# 15. Generic Component Tasks

| ID | Task | Priority | Status | Dependencies | Related docs | Test status |
|---|---|---|---|---|---|---|
| GEN-001 | Ambient-air source | P1 | Done | GRAPH-008, CORE-010 | `03_PhysicalArchitecture.md` | Passing |
| GEN-002 | Solar-radiation source | P1 | Done | GRAPH-008 | `06_SolarCollector.md`, `07_SolarPanel.md` | Passing |
| GEN-003 | Electrical source | P1 | Ready | GRAPH-008 | `03_PhysicalArchitecture.md` | NotStarted |
| GEN-004 | Environment heat sink | P1 | Done | GRAPH-008 | `03_PhysicalArchitecture.md` | Passing |
| GEN-005 | Exhaust-air sink | P1 | Done | GRAPH-008 | `03_PhysicalArchitecture.md` | Passing |
| GEN-006 | Liquid-water sink | P1 | Done | GRAPH-008 | `03_PhysicalArchitecture.md` | Passing |
| GEN-007 | Moist-air mixer | P0 | Done | CORE-010, CORE-012, GRAPH-008 | `04_MathematicalModel.md`, `05_Psychrometrics.md` | Passing |
| GEN-008 | Moist-air splitter | P0 | Done | CORE-010, CORE-012, GRAPH-008 | `04_MathematicalModel.md` | Passing |
| GEN-009 | Simple sensible-heater component | P0 | Done | CORE-010, CORE-012, GRAPH-008 | `04_MathematicalModel.md` | Passing |
| GEN-010 | Basic duct pressure-loss component | P1 | Done | GRAPH-008, DOC-013A | `13_FanAndAirflow.md` | Passing |

---

# 16. Physical Component Implementation Tasks

## 16.1 Solar panel

| ID | Task | Priority | Status | Dependencies | Doc | Test status |
|---|---|---|---|---|---|---|
| PV-001 | Constant-efficiency PV model | P1 | Done | GEN-002, CORE-012 | `07_SolarPanel.md` | Passing |
| PV-002 | Temperature-corrected PV model | P1 | Ready | PV-001 | `07_SolarPanel.md` | NotStarted |
| PV-003 | Dynamic electrothermal PV model | P2 | Blocked | PV-002, DOC-025 | `07_SolarPanel.md` | NotStarted |
| PV-004 | Rear-air channel heat transfer | P1 | Blocked | PV-003, CORE-010 | `07_SolarPanel.md` | NotStarted |
| PV-005 | PV pressure-drop model | P2 | Blocked | PV-004, GEN-010 | `07_SolarPanel.md` | NotStarted |
| PV-006 | PV integration tests | P1 | Blocked | PV-001 to PV-005 | `22_TestStrategy.md` | NotStarted |

## 16.2 Solar collector

| ID | Task | Priority | Status | Dependencies | Doc | Test status |
|---|---|---|---|---|---|---|
| SC-001 | Constant-efficiency collector | P1 | Done | GEN-002, GEN-009 | `06_SolarCollector.md` | Passing |
| SC-002 | Optical absorption model | P1 | Done | SC-001 | `06_SolarCollector.md` | Passing |
| SC-003 | Dynamic absorber energy balance | P1 | Ready | SC-002, DOC-025 | `06_SolarCollector.md` | NotStarted |
| SC-004 | Environmental heat loss | P1 | Blocked | SC-003 | `06_SolarCollector.md` | NotStarted |
| SC-005 | Stagnation and overtemperature | P1 | Blocked | SC-003, SC-004 | `06_SolarCollector.md` | NotStarted |
| SC-006 | Collector pressure drop | P2 | Blocked | GEN-010 | `06_SolarCollector.md` | NotStarted |
| SC-007 | Collector integration tests | P1 | Blocked | SC-001 to SC-006 | `22_TestStrategy.md` | NotStarted |

## 16.3 Peltier

| ID | Task | Priority | Status | Dependencies | Doc | Test status |
|---|---|---|---|---|---|---|
| TEC-001 | Constant-COP model | P1 | Done | CORE-012, GRAPH-008 | `08_Peltier.md` | Passing |
| TEC-002 | Analytical thermoelectric model | P1 | Done | TEC-001, DOC-025 | `08_Peltier.md` | Passing |
| TEC-003 | Current/power solver | P1 | Done | TEC-002 | `08_Peltier.md` | Passing |
| TEC-004 | External thermal resistances | P1 | Done | TEC-002 | `08_Peltier.md` | Passing |
| TEC-005 | Dynamic hot/cold-side state | P2 | Ready | TEC-004, DOC-025 | `08_Peltier.md` | NotStarted |
| TEC-006 | Off-state conduction | P1 | Done | TEC-002 | `08_Peltier.md` | Passing |
| TEC-007 | Safety limits and diagnostics | P1 | Done | TEC-003 to TEC-006 | `08_Peltier.md` | Passing |
| TEC-008 | Peltier integration tests | P1 | Done | TEC-001 to TEC-007 | `22_TestStrategy.md` | Passing |

## 16.4 Silica gel

| ID | Task | Priority | Status | Dependencies | Doc | Test status |
|---|---|---|---|---|---|---|
| SG-001 | Silica-gel state model | P1 | Done | CORE-010, CORE-012 | `09_SilicaGel.md` | Passing |
| SG-002 | Isotherm interface | P1 | Done | SG-001 | `09_SilicaGel.md` | Passing |
| SG-003 | Generic engineering isotherm | P1 | Done | SG-002 | `09_SilicaGel.md` | Passing |
| SG-004 | LDF kinetic model | P1 | Done | SG-003, DOC-025 | `09_SilicaGel.md` | Passing |
| SG-005 | Adsorption water balance | P1 | Done | SG-004 | `09_SilicaGel.md` | Passing |
| SG-006 | Desorption and storage limits | P1 | Done | SG-004 | `09_SilicaGel.md` | Passing |
| SG-007 | Adsorption heat and thermal state | P1 | Done | SG-005, SG-006 | `09_SilicaGel.md` | Passing |
| SG-008 | Energy-limited regeneration | P1 | Done | SG-007 | `09_SilicaGel.md` | Passing |
| SG-009 | Packed-bed pressure drop | P2 | Done | GEN-010 | `09_SilicaGel.md` | Passing |
| SG-010 | Silica-gel integration tests | P1 | Done | SG-001 to SG-009 | `22_TestStrategy.md` | Passing |

## 16.5 Condenser

| ID | Task | Priority | Status | Dependencies | Doc | Test status |
|---|---|---|---|---|---|---|
| COND-001 | Expand condenser specification | P0 | Done | DOC-005A, DOC-008A, DOC-009A | `10_Condenser.md` | Passing |
| COND-002 | Dew-point and sensible-cooling model | P1 | Done | COND-001, CORE-010 | `10_Condenser.md` | Passing |
| COND-003 | Latent condensation model | P1 | Done | COND-002, CORE-012 | `10_Condenser.md` | Passing |
| COND-004 | Cooling-power limitation | P1 | Done | COND-003, TEC-001 | `10_Condenser.md` | Passing |
| COND-005 | Heat/mass-transfer effectiveness | P2 | Blocked | COND-003, DOC-025 | `10_Condenser.md` | NotStarted |
| COND-006 | Drainage efficiency and water output | P1 | Done | COND-003 | `10_Condenser.md` | Passing |
| COND-007 | Condenser integration tests | P1 | Done | COND-002 to COND-006 | `22_TestStrategy.md` | Passing |

## 16.6 Heat recovery

| ID | Task | Priority | Status | Dependencies | Doc | Test status |
|---|---|---|---|---|---|---|
| HR-001 | Expand heat-recovery specification | P1 | Done | DOC-005A, DOC-010A | `11_HeatRecovery.md` | Passing |
| HR-002 | Sensible effectiveness model | P1 | Done | HR-001, CORE-010 | `11_HeatRecovery.md` | Passing |
| HR-003 | Counter-flow effectiveness–NTU model | P2 | Done | HR-002, DOC-025 | `11_HeatRecovery.md` | Passing |
| HR-004 | Hot/cold pressure-drop models | P2 | Blocked | GEN-010 | `11_HeatRecovery.md` | NotStarted |
| HR-005 | Bypass control | P2 | Blocked | HR-002 | `11_HeatRecovery.md` | NotStarted |
| HR-006 | Heat-recovery integration tests | P1 | Blocked | HR-002 to HR-005 | `22_TestStrategy.md` | NotStarted |

## 16.7 Fan and airflow

| ID | Task | Priority | Status | Dependencies | Doc | Test status |
|---|---|---|---|---|---|---|
| AIR-001 | Expand fan and airflow specification | P0 | Done | DOC-004A, DOC-005A | `13_FanAndAirflow.md` | Passing |
| AIR-002 | Prescribed-flow fan model | P1 | Done | AIR-001, CORE-012 | `13_FanAndAirflow.md` | Passing |
| AIR-003 | Fan power calculation | P1 | Done | AIR-002 | `13_FanAndAirflow.md` | Passing |
| AIR-004 | Fan performance curve | P2 | Blocked | AIR-002 | `13_FanAndAirflow.md` | NotStarted |
| AIR-005 | Airflow network resistance | P2 | Blocked | AIR-004, GEN-010 | `13_FanAndAirflow.md` | NotStarted |
| AIR-006 | Fan/system operating point | P2 | Blocked | AIR-004, AIR-005, DOC-025 | `13_FanAndAirflow.md` | NotStarted |
| AIR-007 | Multi-fan support | P3 | Blocked | AIR-006 | `13_FanAndAirflow.md` | NotStarted |
| AIR-008 | Airflow integration tests | P1 | Blocked | AIR-002 to AIR-006 | `22_TestStrategy.md` | NotStarted |

## 16.8 Battery and power management

| ID | Task | Priority | Status | Dependencies | Doc | Test status |
|---|---|---|---|---|---|---|
| PWR-001 | Expand battery and power specification | P0 | Done | DOC-004A, DOC-007A, DOC-008A | `12_BatteryAndPowerManagement.md` | Passing |
| PWR-002 | Battery SOC model | P1 | Done | PWR-001, CORE-012 | `12_BatteryAndPowerManagement.md` | Passing |
| PWR-003 | Charge/discharge efficiency | P1 | Done | PWR-002 | `12_BatteryAndPowerManagement.md` | Passing |
| PWR-004 | Charge/discharge power limits | P1 | Done | PWR-002 | `12_BatteryAndPowerManagement.md` | Passing |
| PWR-005 | Load priority and shedding | P1 | Done | PWR-003, PWR-004 | `12_BatteryAndPowerManagement.md` | Passing |
| PWR-006 | PV curtailment | P2 | Done | PWR-005, PV-001 | `12_BatteryAndPowerManagement.md` | Passing |
| PWR-007 | Power-management integration tests | P1 | Ready | PWR-002 to PWR-006 | `22_TestStrategy.md` | NotStarted |

---

# 17. ThermoCore.AWG Tasks

| ID | Task | Priority | Status | Dependencies | Related docs | Test status |
|---|---|---|---|---|---|---|
| AWG-001 | Create detailed control-system specification | P1 | Planned | COND-001, HR-001, AIR-001, PWR-001 | `14_ControlSystem.md` | Planned |
| AWG-002 | Create detailed system-topology specification | P1 | Planned | AWG-001 | `15_SystemTopology.md` | Planned |
| AWG-003 | Implement AWG configuration model | P1 | Blocked | AWG-002 | `15_SystemTopology.md` | NotStarted |
| AWG-004 | Implement V3 airflow graph builder | P1 | Blocked | AWG-003, GRAPH-008, AIR-002 | `15_SystemTopology.md` | NotStarted |
| AWG-005 | Implement electrical graph | P1 | Blocked | AWG-003, PWR-002, PV-001 | `15_SystemTopology.md` | NotStarted |
| AWG-006 | Implement water-flow graph | P1 | Blocked | AWG-003, COND-006 | `15_SystemTopology.md` | NotStarted |
| AWG-007 | Implement sensor and measurement points | P2 | Blocked | AWG-003 | `15_SystemTopology.md` | NotStarted |
| AWG-008 | Implement operating-mode state machine | P1 | Blocked | AWG-001, AWG-003 | `14_ControlSystem.md` | NotStarted |
| AWG-009 | Implement fan control | P1 | Blocked | AWG-008, AIR-002 | `14_ControlSystem.md` | NotStarted |
| AWG-010 | Implement Peltier control | P1 | Blocked | AWG-008, TEC-003 | `14_ControlSystem.md` | NotStarted |
| AWG-011 | Implement recirculation control | P1 | Blocked | AWG-008, HR-002, GEN-007, GEN-008 | `14_ControlSystem.md` | NotStarted |
| AWG-012 | Implement battery protection | P1 | Blocked | AWG-008, PWR-005 | `14_ControlSystem.md` | NotStarted |
| AWG-013 | Implement thermal safety rules | P1 | Blocked | AWG-008, SC-005, TEC-007 | `14_ControlSystem.md` | NotStarted |
| AWG-014 | Implement water-tank state | P1 | Blocked | CORE-012, COND-006 | `15_SystemTopology.md` | NotStarted |
| AWG-015 | Integrate cyclic recirculation solver | P1 | Blocked | GRAPH-014, AWG-011 | `16_SimulationEngine.md` | NotStarted |
| AWG-016 | Run first 24-hour simulation | P1 | Blocked | AWG-004 to AWG-015, DOC-028A | `28_WeatherModel.md` | NotStarted |
| AWG-017 | Export first full result dataset | P1 | Blocked | AWG-016, DOC-029A | `29_ResultFormats.md` | NotStarted |
| AWG-018 | Verify system water balance | P0 | Blocked | AWG-016 | `04_MathematicalModel.md` | NotStarted |
| AWG-019 | Verify system energy balance | P0 | Blocked | AWG-016 | `04_MathematicalModel.md` | NotStarted |

---

# 18. Console, API and Web Tasks

## 18.1 Console

| ID | Task | Priority | Status | Dependencies | Test status |
|---|---|---|---|---|---|
| APP-001 | Create console host | P1 | Blocked | DEV-001 | NotStarted |
| APP-002 | Load JSON configuration | P1 | Blocked | APP-001, AWG-003 | NotStarted |
| APP-003 | Run simulation | P1 | Blocked | APP-002, GRAPH-008 | NotStarted |
| APP-004 | Print summary | P1 | Blocked | APP-003 | NotStarted |
| APP-005 | Export CSV | P1 | Blocked | APP-003, DOC-029A | NotStarted |
| APP-006 | Add regression scenarios | P1 | Blocked | APP-003, DOC-022 | NotStarted |

## 18.2 API

| ID | Task | Priority | Status | Dependencies | Related doc | Test status |
|---|---|---|---|---|---|---|
| API-001 | Create API specification | P2 | Planned | DOC-016A, DOC-021A, DOC-029A | `19_WebApi.md` | Planned |
| API-002 | Create ASP.NET Core API project | P2 | Blocked | DEV-001, API-001 | `19_WebApi.md` | NotStarted |
| API-003 | Add psychrometric calculator endpoint | P2 | Blocked | API-002, CORE-010 | `19_WebApi.md` | NotStarted |
| API-004 | Add simulation validation endpoint | P2 | Blocked | API-002, AWG-003 | `19_WebApi.md` | NotStarted |
| API-005 | Add simulation job endpoint | P2 | Blocked | API-002, AWG-016 | `19_WebApi.md` | NotStarted |
| API-006 | Add status and result endpoints | P2 | Blocked | API-005 | `19_WebApi.md` | NotStarted |
| API-007 | Add cancellation | P2 | Blocked | API-005, GRAPH-011 | `19_WebApi.md` | NotStarted |
| API-008 | Add OpenAPI | P2 | Blocked | API-002 | `19_WebApi.md` | NotStarted |
| API-009 | Add resource limits | P2 | Blocked | API-005 | `19_WebApi.md` | NotStarted |
| API-010 | Add integration tests | P2 | Blocked | API-003 to API-009 | `22_TestStrategy.md` | NotStarted |

## 18.3 Blazor Web

| ID | Task | Priority | Status | Dependencies | Related doc | Test status |
|---|---|---|---|---|---|---|
| WEB-001 | Create Blazor architecture specification | P2 | Planned | API-001 | `20_BlazorWeb.md` | Planned |
| WEB-002 | Create Blazor Web project | P2 | Blocked | DEV-001, WEB-001 | `20_BlazorWeb.md` | NotStarted |
| WEB-003 | Create home and project overview | P3 | Blocked | WEB-002 | `20_BlazorWeb.md` | NotStarted |
| WEB-004 | Create psychrometric calculator page | P2 | Blocked | WEB-002, API-003 | `20_BlazorWeb.md` | NotStarted |
| WEB-005 | Create AWG configuration editor | P2 | Blocked | WEB-002, API-004 | `20_BlazorWeb.md` | NotStarted |
| WEB-006 | Create simulation execution UI | P2 | Blocked | WEB-005, API-005 | `20_BlazorWeb.md` | NotStarted |
| WEB-007 | Add progress and cancellation | P2 | Blocked | WEB-006, API-007 | `20_BlazorWeb.md` | NotStarted |
| WEB-008 | Add result summary | P2 | Blocked | WEB-006, API-006 | `20_BlazorWeb.md` | NotStarted |
| WEB-009 | Add charts | P2 | Blocked | WEB-008, DOC-029A | `20_BlazorWeb.md` | NotStarted |
| WEB-010 | Add balance diagnostics UI | P2 | Blocked | WEB-008 | `20_BlazorWeb.md` | NotStarted |
| WEB-011 | Add CSV/JSON export | P2 | Blocked | WEB-008, APP-005 | `20_BlazorWeb.md` | NotStarted |
| WEB-012 | Add Web tests | P2 | Blocked | WEB-004 to WEB-011 | `22_TestStrategy.md` | NotStarted |

---

# 19. Persistence Tasks

| ID | Task | Priority | Status | Dependencies | Related doc | Test status |
|---|---|---|---|---|---|---|
| DATA-001 | Create data-model specification | P2 | Planned | DOC-016A, DOC-029A | `21_DataModel.md` | Planned |
| DATA-002 | Define configuration schema versioning | P2 | Blocked | DATA-001 | `21_DataModel.md` | NotStarted |
| DATA-003 | Define simulation-run metadata | P2 | Blocked | DATA-001 | `21_DataModel.md` | NotStarted |
| DATA-004 | Add SQLite persistence | P3 | Blocked | DATA-002, DATA-003 | `21_DataModel.md` | NotStarted |
| DATA-005 | Add PostgreSQL provider | P3 | Blocked | DATA-004 | `21_DataModel.md` | NotStarted |
| DATA-006 | Save and reload configurations | P2 | Blocked | DATA-004 | `21_DataModel.md` | NotStarted |
| DATA-007 | Save result summaries | P2 | Blocked | DATA-004, DOC-029A | `21_DataModel.md` | NotStarted |
| DATA-008 | Compare simulation runs | P3 | Blocked | DATA-006, DATA-007 | `21_DataModel.md` | NotStarted |

---

# 20. Open Source and Documentation Portal Tasks

| ID | Task | Priority | Status | Dependencies | Output |
|---|---|---|---|---|---|
| OSS-001 | Finalize open-source license | P1 | Planned | REP-003 | ADR and LICENSE |
| OSS-002 | Create public project README | P1 | Planned | REP-002, DOC-002 | GitHub landing page |
| OSS-003 | Add architecture diagram assets | P2 | Planned | OSS-002 | `docs/Images/` |
| OSS-004 | Create first-good-issue backlog | P3 | Planned | REP-010 | GitHub issues |
| OSS-005 | Add release process | P2 | Planned | REP-012 | GitHub workflow |
| DOCSITE-001 | Select MkDocs Material | P2 | Planned | DOC-006 | ADR |
| DOCSITE-002 | Create `mkdocs.yml` | P2 | Blocked | DOCSITE-001, DOC-012 | Documentation site |
| DOCSITE-003 | Add Mermaid support | P2 | Blocked | DOCSITE-002 | Diagram rendering |
| DOCSITE-004 | Add MathJax/KaTeX support | P2 | Blocked | DOCSITE-002 | Equation rendering |
| DOCSITE-005 | Add search and navigation | P2 | Blocked | DOCSITE-002 | Usable portal |
| DOCSITE-006 | Add GitHub Pages deployment | P2 | Blocked | DOCSITE-002, REP-012 | Public docs |
| DOCSITE-007 | Validate all links in CI | P2 | Blocked | DOC-008, DOCSITE-002 | Link checker |

---

# 21. Calibration and Optimization Tasks

| ID | Task | Priority | Status | Dependencies | Related doc | Test status |
|---|---|---|---|---|---|---|
| CAL-001 | Create calibration specification | P3 | Planned | AWG-016, DATA-001 | `23_Calibration.md` | Planned |
| CAL-002 | Define measurement CSV schema | P3 | Blocked | CAL-001 | `23_Calibration.md` | NotStarted |
| CAL-003 | Implement measurement import | P3 | Blocked | CAL-002 | `23_Calibration.md` | NotStarted |
| CAL-004 | Implement time-series alignment | P3 | Blocked | CAL-003 | `23_Calibration.md` | NotStarted |
| CAL-005 | Implement RMSE and bias | P3 | Blocked | CAL-004 | `23_Calibration.md` | NotStarted |
| CAL-006 | Implement parameter fitting | P3 | Blocked | CAL-005, DOC-025 | `23_Calibration.md` | NotStarted |
| CAL-007 | Store calibrated parameter provenance | P3 | Blocked | CAL-006, DATA-003 | `23_Calibration.md` | NotStarted |
| OPT-001 | Create optimization specification | P3 | Planned | CAL-001 | `24_Optimization.md` | Planned |
| OPT-002 | Implement parameter sweeps | P3 | Blocked | OPT-001, AWG-016 | `24_Optimization.md` | NotStarted |
| OPT-003 | Implement sensitivity analysis | P3 | Blocked | OPT-002 | `24_Optimization.md` | NotStarted |
| OPT-004 | Implement liters/day objective | P3 | Blocked | OPT-002 | `24_Optimization.md` | NotStarted |
| OPT-005 | Implement Wh/liter objective | P3 | Blocked | OPT-002 | `24_Optimization.md` | NotStarted |
| OPT-006 | Implement multi-objective comparison | P4 | Blocked | OPT-003 to OPT-005 | `24_Optimization.md` | NotStarted |
| OPT-007 | Add web scenario comparison | P4 | Blocked | OPT-006, WEB-009 | `24_Optimization.md` | NotStarted |

---

# 22. Second ThermoCore Application Tasks

| ID | Task | Priority | Status | Dependencies | Notes |
|---|---|---|---|---|---|
| APP2-001 | Document second thermo concept | P4 | Deferred | AWG-016 | Do not begin before Core/AWG proves reusable |
| APP2-002 | Define system boundary | P4 | Blocked | APP2-001 | Identify energy and mass domains |
| APP2-003 | Map reusable components | P4 | Blocked | APP2-002 | Reuse ThermoCore components |
| APP2-004 | Specify missing components | P4 | Blocked | APP2-003 | Add generic components where possible |
| APP2-005 | Build simulation topology | P4 | Blocked | APP2-004 | Validate framework reusability |
| APP2-006 | Run sizing and feasibility study | P4 | Blocked | APP2-005 | Compare design scenarios |

---

# 23. Milestone Tracking

## Milestone M0 — Documentation ready for implementation

Status:

```text
Done
```

Completion requirements:

- [x] MASTER_INDEX exists
- [x] ARCHITECTURE_MAP exists
- [x] DOCUMENT_DEPENDENCY_GRAPH exists
- [x] Roadmap exists
- [x] Coding rules exist
- [x] AI context exists
- [x] Numerical methods specification ready
- [x] Constants specification ready
- [x] Units specification ready
- [x] Test strategy ready
- [x] Repository structure created

## Milestone M1 — ThermoCore 0.1 Psychrometric Core

Status:

```text
In progress
```

Completion requirements:

- [x] Solution builds
- [x] Psychrometric calculator implemented
- [x] MoistAirState implemented
- [x] Reference tests passing
- [x] Console demo available

## Milestone M2 — ThermoCore 0.2 Simulation Core

Status:

```text
In progress
```

Completion requirements:

- [x] Port model
- [x] Component interface
- [x] Graph validation
- [x] Acyclic execution
- [x] Balance aggregation
- [x] Determinism tests

## Milestone M3 — ThermoCore.AWG 0.5 Engineering Prototype

Status:

```text
Blocked
```

Completion requirements:

- [ ] Solar panel
- [ ] Solar collector
- [ ] Peltier
- [ ] Silica gel
- [ ] Condenser
- [ ] Fan and airflow
- [ ] Battery
- [ ] AWG controller
- [ ] 24-hour simulation
- [ ] Water and energy balances

## Milestone M4 — ThermoCore.Web 0.7 MVP

Status:

```text
Blocked
```

Completion requirements:

- [ ] API
- [ ] Blazor configuration editor
- [ ] Simulation jobs
- [ ] Result charts
- [ ] CSV/JSON export
- [ ] Linux deployment

## Milestone M5 — ThermoCore 1.0 Validated Release

Status:

```text
Blocked
```

Completion requirements:

- [ ] Prototype measurement data
- [ ] Calibration
- [ ] Published model limitations
- [ ] Stable configuration schema
- [ ] Public documentation portal
- [ ] Public GitHub release

---

# 24. Next-Task Selection Rule

The next development task shall be selected using this order:

1. Lowest priority number (`P0` before `P1`)
2. Status is `Ready`
3. All dependencies are `Done` or sufficiently available
4. Task lies on the current milestone's critical path
5. Prefer finishing one coherent layer before starting another

Current next-task queue:

```text
1. SC-003 — Dynamic absorber energy balance
2. PWR-007 — Power-management integration tests
3. PV-002 — Temperature-corrected PV model
4. GEN-003 — Electrical source
5. TEC-005 — Dynamic hot/cold-side state
```

---

# 25. Update Rules

Whenever a task changes:

- Update `Status`.
- Update `Test status`.
- Update `Documentation status` where relevant.
- Add a short note if the task is blocked.
- Update the milestone checklist.
- Update the summary counts periodically.

When a task becomes `Done`, verify:

- Build passes where code exists.
- Required tests pass.
- Documentation matches implementation.
- Dependencies and JSON graphs remain correct.
- No architectural rule is violated.

---

# 26. AI Usage Instructions

Before an AI starts a task, it shall:

1. Locate the task ID in this document.
2. Read all dependency documents.
3. Confirm that the task status is `Ready`.
4. Read `AI_CONTEXT.md`.
5. Read `18_CodingRules.md`.
6. Read component-specific documentation.
7. Implement only the selected scope.
8. Add required tests.
9. Report files changed and commands run.
10. Suggest the exact status update for this file.

An AI shall not mark a task as `Done` unless build and test results were actually verified.

---

# 27. Definition of Done

A task may be marked `Done` only when:

- Its implementation or document is complete for the defined scope.
- Acceptance criteria are met.
- Required tests pass.
- Required documentation exists.
- Public interfaces are documented.
- No critical diagnostics remain unresolved.
- Conservation checks pass where applicable.
- Cross-platform rules remain satisfied.
- The task's downstream dependencies can safely proceed.

A physical model shall be marked `Validated` only after comparison with:

- Trusted reference data
- Manufacturer data
- Published experimental data
- Prototype measurements

Implementation completion alone is not physical validation.

---

**End of Document**
