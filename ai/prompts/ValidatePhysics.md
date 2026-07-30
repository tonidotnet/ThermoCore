# Validate Physics

## Goal
Validate conservation, units and physical bounds for a component or system change.

## Must read
1. `ai/AI_CONTEXT.md`
2. Governing engineering document for the model
3. `docs/01_Architecture/04_MathematicalModel.md` (or current math-model path)
4. Diff or files: `{SCOPE}`

## Checklist
- [ ] All internal quantities are SI
- [ ] Dry-air, water and energy balances are reported
- [ ] Residuals are checked against central tolerances
- [ ] Storage terms match Evaluate/Commit state updates
- [ ] Boundary sources/sinks are explicit
- [ ] Empirical coefficients cite source or calibration placeholder
- [ ] Validity ranges and out-of-range diagnostics exist
- [ ] No silent energy/mass creation

## Output
1. Blocking physics defects
2. Missing diagnostics or tests
3. Accepted modeling approximations
4. Verdict
