# ThermoCore
# AI_CONTEXT.md

**Version:** 1.0  
**Audience:** AI coding assistants (Codex, Claude Code, Cursor, GitHub Copilot, Roo Code, etc.)

# Purpose

This document is the canonical context that an AI should load before implementing any part of the ThermoCore project.

The goal is to ensure that every generated implementation follows the same architecture, engineering assumptions and coding standards.

---

# Project Identity

ThermoCore is a reusable thermodynamic simulation framework.

The first product built on the framework is:

**ThermoCore.AWG**

which simulates a portable Atmospheric Water Generator (AWG).

The framework is intentionally generic so future products can include:

- Heat pumps
- HVAC systems
- Solar thermal systems
- Thermal storage
- Refrigeration systems
- Industrial drying systems

Do **not** hardcode AWG assumptions into `ThermoCore.Core`.

---

# Source of Truth

Always treat the documentation as authoritative.

Read in this order:

1. MASTER_INDEX.md
2. ARCHITECTURE_MAP.md
3. DOCUMENT_DEPENDENCY_GRAPH.md
4. 17_Roadmap.md
5. 18_CodingRules.md
6. Component-specific engineering documents
7. JSON dependency graphs

If documentation and code disagree, prefer the documentation unless an Architecture Decision Record explicitly supersedes it.

---

# Core Principles

- SI units internally
- Immutable state whenever practical
- Deterministic simulation
- Explicit mass conservation
- Explicit energy conservation
- Replaceable physical models
- Platform-independent Core
- Separation of physics and presentation

---

# Layer Rules

ThermoCore.Core
- Physics
- Math
- Simulation engine
- Diagnostics
- Interfaces

Must NOT reference:
- ASP.NET
- Blazor
- WPF
- WinUI
- Database frameworks
- UI libraries

ThermoCore.AWG
- Implements AWG-specific components.
- Depends only on ThermoCore.Core.

ThermoCore.Api
- Hosts REST endpoints.
- Must not duplicate physics.

ThermoCore.Web
- User interface only.
- Uses API/Application layer.

---

# Simulation Model

Simulation uses:

Evaluate()
→ calculate proposed state

Commit()
→ apply accepted state

Evaluate() must never mutate component state.

---

# Engineering Rules

Every component shall:

- define inputs
- define outputs
- expose diagnostics
- conserve mass
- conserve energy
- validate parameters
- support unit testing

No component may silently create or destroy:

- water
- dry air
- energy

---

# Numerical Rules

Prefer stable numerical methods.

Avoid hidden approximations.

All approximations must be documented.

Expose convergence diagnostics.

---

# Coding Style

Prefer:

- immutable records
- dependency injection
- small interfaces
- composition
- explicit naming

Avoid:

- static mutable state
- magic numbers
- hidden unit conversions
- duplicated equations

---

# Testing Requirements

Every feature requires:

- unit tests
- integration tests
- deterministic execution
- conservation validation

Simulation results must be reproducible.

---

# Documentation Policy

Whenever implementation changes behaviour:

1. Update documentation.
2. Update dependency graph if required.
3. Update implementation progress.
4. Record significant architectural decisions.

---

# AI Workflow

For each task:

1. Read required documents.
2. Identify dependencies.
3. Implement minimal correct solution.
4. Add tests.
5. Validate balances.
6. Review architecture.
7. Update documentation.

Never skip validation.

---

# Definition of Done

A feature is complete only if:

- Code builds.
- Tests pass.
- Architecture rules are respected.
- Public API documented.
- Diagnostics available.
- Conservation checks pass.
- Documentation updated.

---

# Preferred Output

When generating code:

- explain assumptions
- identify limitations
- reference related documents
- avoid inventing undocumented behaviour

When uncertain:

Stop and request clarification instead of guessing.

---

# Related Files

- MASTER_INDEX.md
- ARCHITECTURE_MAP.md
- DOCUMENT_DEPENDENCY_GRAPH.md
- COMPONENT_GRAPH.json
- DOCUMENT_GRAPH.json
- IMPLEMENTATION_GRAPH.json
- PROMPT_GUIDE.md
- IMPLEMENTATION_PLAYBOOK.md
- AI_REVIEW_CHECKLIST.md
- AI_DEVELOPMENT_GUIDE.md

---

# Long-Term Vision

ThermoCore should become a reusable engineering simulation platform with interchangeable physical components and multiple products sharing the same Core engine.

Every implementation should move the project closer to that goal.

**End of Document**
