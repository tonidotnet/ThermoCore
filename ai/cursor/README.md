# Cursor Research Track

Use this folder as the execution entry point.

## Mandatory reading order

1. `ai/AI_CONTEXT.md`
2. `ai/context/CURRENT_IMPLEMENTATION_CONTEXT.md`
3. `ai/context/COOLING_SYSTEM_CONTEXT.md`
4. `docs/08_ResearchAndEvolution/31_CurrentImplementationAudit.md`
5. `docs/08_ResearchAndEvolution/43_DevelopmentRecommendations.md`
6. `docs/00_Project/RESEARCH_IMPLEMENTATION_PROGRESS.md`
7. target task file under `ai/cursor/tasks/`

## Rules

- Work on one task at a time.
- Preserve existing regressions.
- Do not rewrite existing Peltier physics unless a task explicitly requires it.
- Make backward-compatible configuration changes.
- Add tests for every change.
- Run the existing AWG regression suite before and after relevant changes.
- Update `RESEARCH_IMPLEMENTATION_PROGRESS.md` only after verified completion.
