# R7-001 — Absorption Feasibility Model

**Status:** Done (research-only scaffold)

Requirements:
COOL-008.

Purpose:
provide a documentation-aligned performance-map feasibility screen without enabling
absorption as a production AWG cooling technology.

Implement:
- Core `AbsorptionPerformanceMap` + IDW interpolator + `AbsorptionCoolingResearchModel`;
- AWG `AbsorptionCoolingResearchFacade` (not `ICoolingPlantModel`);
- factory continues to reject `CoolingTechnology.AbsorptionResearch`.

Do **not** build a detailed absorption-cycle solver until a specific device/dataset justifies it.
