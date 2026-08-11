# Cursor Master Implementation Prompt

You are implementing the ThermoCore AWG Cooling Research & Evolution track.

Before changing code:

1. Read the mandatory files listed in `ai/cursor/README.md`.
2. Inspect the current code before assuming types or locations.
3. Identify the exact task ID.
4. State which existing components will be reused.
5. State which files you expect to change.

Implementation rules:

- Preserve public behavior unless the task explicitly changes it.
- Do not move AWG-specific orchestration into Core.
- Do not replace the existing analytical Peltier component.
- Prefer additive, backward-compatible configuration.
- Use SI internally.
- Preserve mass, water and energy balances.
- Keep execution deterministic.
- Add unit/integration/regression tests.
- Do not mark research work validated without measurement evidence.

At completion report:

- files changed;
- tests added;
- commands run;
- build result;
- test result;
- baseline/regression comparison;
- assumptions;
- limitations;
- recommended next task.
