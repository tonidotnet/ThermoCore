# Implement Component

## Goal
Implement or extend one `ISimulationComponent` for ThermoCore.Core.

## Must read
1. `ai/AI_CONTEXT.md`
2. `docs/07_ProjectManagement/18_CodingRules.md`
3. Governing engineering document for this component
4. `docs/00_Project/IMPLEMENTATION_PROGRESS.md` task `{TASK_ID}`
5. Existing similar component under `src/ThermoCore.Core/Components/`

## Task
Component: `{COMPONENT_NAME}`
Task ID: `{TASK_ID}`
Specification: `{DOC_PATH}`

## Requirements
- Implement `ISimulationComponent` (`Initialize`, `Evaluate`, `Commit`, ports, diagnostics).
- One top-level type per file; file name matches the type name.
- Use SI units only inside Core.
- Return `ConservationBalance` for the timestep.
- Reject NaN/Infinity via existing validation helpers.
- Do not depend on API, Blazor, WPF or database types.
- Do not invent physics missing from the specification.
- Add unit tests and at least one graph integration test when the component has ports.

## Deliverables
1. Component source file(s)
2. Tests
3. Suggested tracker status update for `{TASK_ID}`
4. Short list of known limitations

## Forbidden
- Hidden unit conversions
- Mutating another component's state
- Silent clamping without diagnostics
- UI or host-layer code in Core
