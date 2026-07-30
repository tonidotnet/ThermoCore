# Implement AWG Feature

## Goal
Implement an AWG application-layer feature without moving physics into hosts.

## Must read
1. `ai/AI_CONTEXT.md`
2. `docs/07_ProjectManagement/18_CodingRules.md`
3. `docs/04_Simulation/14_ControlSystem.md` and/or `15_SystemTopology.md` as applicable
4. `docs/00_Project/IMPLEMENTATION_PROGRESS.md` task `{TASK_ID}`
5. Existing Core components to reuse

## Task
Feature: `{FEATURE_NAME}`
Task ID: `{TASK_ID}`
Namespace: `ThermoCore.AWG.{AREA}`

## Requirements
- Compose Core components; do not duplicate component equations in AWG.
- Keep control logic free of Blazor/API/database types.
- Configuration and topology IDs must be stable and explicit.
- Validate graphs before execution.
- Add AWG unit/integration tests under `tests/ThermoCore.AWG.Tests`.

## Deliverables
1. AWG source files
2. Tests
3. Suggested tracker status update
4. Known limitations
