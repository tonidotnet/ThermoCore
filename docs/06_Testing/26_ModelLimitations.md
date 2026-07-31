# ThermoCore
## 26_ModelLimitations.md

**Version:** 1.0  
**Status:** Published  
**Document Type:** Model-limitations statement (M5)  
**Date:** 2026-07-28  
**Related:** `23_Calibration.md`, prototype campaign protocol

---

# 1. Purpose

Publish known modeling limits so users do not treat ThermoCore MVP results as field-validated performance claims. Closing M5 still requires physical prototype measurements; this document is the software-side limitations baseline.

---

# 2. Domains

| Area | Limitation |
|---|---|
| Psychrometrics | Ideal-gas moist air; standard atmosphere defaults unless overridden |
| Solar collector (AWG) | Dynamic lumped absorber; IAM and wind loss optional; not a full ISO 9806 identification |
| Solar collector (APP2) | Level-1 constant efficiency only |
| Silica gel | Lumped bed; transfer coefficients are calibratable, not manufacturer-identified |
| Condenser / TEC | Bypass-factor style condenser; Peltier uses documented electrothermal model, not datasheet auto-import |
| Heat recovery | Prescribed effectiveness on AWG MVP path (not full NTU network solve) |
| Fan / airflow | Prescribed mass flow or curve fan; no full duct-network pressure solve (AIR network deferred) |
| Battery / power | Lumped SOC + charge/discharge efficiencies; MPPT as constant efficiency |
| Weather | Synthetic diurnal providers unless user supplies series |
| Control | Rule-based AWG controller; not MPC |

---

# 3. Optimization objectives

| Objective | Caveat |
|---|---|
| Liters/day | Extrapolates run water mass to 24 h |
| Wh/liter | Uses final bus power as constant-power proxy |
| Solar utilization | Useful enthalpy rise across collector / Σ G·A·Δt |
| Battery throughput / SOC swing | Requires electrical subsystem; short runs understate cycling |

---

# 4. Calibration / holdout

- Synthetic smoke and synthetic condenser holdout prove the **workflow**, not field accuracy.
- Holdout split is chronological by unique timestamps (`MeasurementDatasetSplitter`).
- Physical campaign protocol: `samples/calibration/PROTOTYPE_CAMPAIGN.md`.

---

**End of Document**
