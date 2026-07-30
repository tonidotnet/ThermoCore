# Review Code

## Goal
Review a ThermoCore change against architecture, coding rules and physics constraints.

## Must read
1. `ai/AI_CONTEXT.md`
2. `ai/AI_REVIEW_CHECKLIST.md`
3. `docs/07_ProjectManagement/18_CodingRules.md`
4. Relevant engineering specification(s)
5. The diff under review

## Task
Scope: `{DIFF_OR_FILES}`
Task ID: `{TASK_ID}`

## Checklist
- [ ] Core remains free of UI/API/persistence dependencies
- [ ] SI units preserved internally
- [ ] Conservation balances reported and validated
- [ ] Diagnostics are structured (`SimulationDiagnostic`)
- [ ] No invented physics
- [ ] Evaluate/Commit lifecycle respected
- [ ] Tests cover new behavior and failure paths
- [ ] Tracker/docs updates suggested when status changes

## Output format
1. Blocking findings (must fix)
2. Non-blocking findings
3. Questions / assumptions
4. Verdict: Approve / Approve with nits / Request changes
