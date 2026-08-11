# R0-001 — Capture Existing AWG Baseline

Goal: create a reproducible pre-research baseline.

Actions:

1. Find the existing AWG regression commands/tests.
2. Run the full relevant baseline suite.
3. Store a small machine-readable baseline summary under an appropriate test artifact/sample location.
4. Do not change physics.
5. Document commands and result identifiers.

Acceptance:
- build passes;
- regression suite passes;
- baseline outputs are reproducible;
- later research PRs can compare against this baseline.
