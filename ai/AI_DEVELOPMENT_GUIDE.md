# AI Development Guide

## Purpose

Defines how any AI contributes to ThermoCore.

## Principles

- Read `AI_CONTEXT.md` first.
- Never bypass architecture.
- Never duplicate physics.
- Update documentation before code.
- Keep changes small and reviewable.
- Preserve deterministic behaviour.
- Reference existing specifications.

## Mandatory reading order

1. `AI_CONTEXT.md`
2. `MASTER_INDEX.md`
3. `IMPLEMENTATION_PROGRESS.md`
4. `docs/07_ProjectManagement/18_CodingRules.md`
5. Target specification
6. This guide (`AI_DEVELOPMENT_GUIDE.md`)

## Source file layout (mandatory)

**One top-level type per file.**

- Each source file shall declare exactly one top-level `class`, `interface`, `record`, `enum`, or `struct`.
- The file name shall match the type name (`RuleBasedAwgController.cs` → `RuleBasedAwgController`).
- Nested private/protected helper types may live inside the owning type's file.
- Do not place multiple public or internal top-level types in the same `.cs` file.
- Test projects follow the same rule for production-like helpers; private nested test doubles inside a test class are allowed.
- When adding or moving types, create a new file rather than appending to an existing multi-type file.

### Examples

Allowed:

```csharp
// AwgOperatingMode.cs
namespace ThermoCore.AWG.Control;

public enum AwgOperatingMode { /* ... */ }
```

```csharp
// SimulationEngine.cs
public sealed class SimulationEngine
{
    private sealed record EvaluationScratch { /* nested helper OK */ }
}
```

Forbidden:

```csharp
// Controllers.cs  — multiple top-level types
public static class AwgFanController { }
public static class AwgPeltierController { }
```

## Related prompts and checklists

- Prefer prompts from `AI_PROMPT_LIBRARY.md`.
- Review diffs against `AI_REVIEW_CHECKLIST.md` and coding rules.
- After structural moves, run `dotnet build` / targeted `dotnet test` before marking work done.
