# ThermoCore Research Evolution Cursor Package v1

This package is **additive**.

Extract it over the ThermoCore repository root.

It adds:

```text
docs/08_ResearchAndEvolution/
docs/ADR/ADR-016_AWG_CoolingPlant_Abstraction.md
docs/00_Project/RESEARCH_IMPLEMENTATION_PROGRESS.md
ai/context/
ai/graphs/
ai/cursor/
```

It does not intentionally overwrite existing Core/AWG implementation specifications.

## Recommended Git workflow

```bash
git switch main
git pull
git switch -c docs/research-evolution
```

Extract/copy the package, review it, commit the documentation, then merge it to `main`.

For implementation, create one branch per task, starting with:

```text
R0-001
R1-001
R1-002
R2-001
R2-002
...
```

## Cursor entry point

Open:

```text
ai/cursor/00_MASTER_IMPLEMENTATION_PROMPT.md
```

Then give Cursor exactly one task file from:

```text
ai/cursor/tasks/
```

Do not ask Cursor to implement the entire research track in one prompt.
