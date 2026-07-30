# Write Tests

## Goal
Add unit and/or integration tests for ThermoCore.

## Must read
1. `ai/AI_CONTEXT.md`
2. `docs/07_ProjectManagement/18_CodingRules.md`
3. `docs/07_ProjectManagement/22_TestStrategy.md` (if present)
4. Target production code and existing tests in the same area

## Task
Target: `{TARGET_TYPE_OR_FEATURE}`
Task ID: `{TASK_ID}`
Test project: `{TEST_PROJECT}` (usually `ThermoCore.Core.Tests`)

## Requirements
- Prefer xUnit + fluent assertions already used in the repo.
- Cover happy path, invalid inputs, and conservation residuals where applicable.
- Use SI values in assertions; convert only at display boundaries in demos.
- Integration tests must build a real `SimulationGraph` and run an engine.
- Tests must be deterministic (fixed inputs, no wall-clock dependence).
- Name tests after behavior, not implementation details.

## Deliverables
1. Test files
2. Commands run (`dotnet test ...`)
3. Pass/fail summary
4. Suggested tracker test-status update
