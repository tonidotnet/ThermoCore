# AI Prompt Guide

## Purpose
Guide AI coding assistants when implementing ThermoCore.

## Always read first
1. MASTER_INDEX.md
2. ARCHITECTURE_MAP.md
3. DOCUMENT_DEPENDENCY_GRAPH.md
4. 17_Roadmap.md
5. 18_CodingRules.md

## Workflow
1. Read required engineering documents.
2. Do not invent physics.
3. Preserve SI units.
4. Keep Core platform independent.
5. Write tests before implementation.
6. Update documentation.
7. Prefer prompts/templates from `AI_PROMPT_LIBRARY.md`.

## Forbidden
- UI code in Core
- Mutable global state
- Hidden unit conversions
- Ignoring conservation laws
- Multiple top-level types in one `.cs` file
