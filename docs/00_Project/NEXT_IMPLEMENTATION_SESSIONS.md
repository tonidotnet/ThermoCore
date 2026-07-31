# ThermoCore
## NEXT_IMPLEMENTATION_SESSIONS.md

**Version:** 1.0  
**Status:** Sessions A–H completed; M5/OPT/CPM/APP2 wave landed (2026-07-28)  
**Document Type:** Gap audit and session backlog  
**Date:** 2026-07-31  
**Related:** `IMPLEMENTATION_PROGRESS.md`, product specs 19–24, 30

---

# 1. Audit verdict

Per-task rows in `IMPLEMENTATION_PROGRESS.md` are largely accurate: Core, physical components, AWG V3, API/Web MVP, SQLite/Postgres, CAL, and OPT MVP exist under `src/` with tests.

What is **not** accurate:

- §7 dashboard still shows Core / Physical / Validation as **0 Done**
- Milestone M3 still **Blocked** with unchecked components while AWG/component tasks are Done
- Several specs still say `ReadyForImplementation` / “fitting later” / “charts remain” while code is past that
- `DOCUMENT_INVENTORY.md` still lists calibration/optimization as Outline

**Real remaining work** is product polish, honesty on soft-Done items, OSS/DEV gates, and documented “later” features — not rebuilding the foundation.

---

# 2. Gap categories

## 2.1 Bookkeeping debt (docs/tracker lie)

| Gap | Evidence |
|---|---|
| Stale §7 dashboard | Core/Physical/CAL-OPT Done counts = 0 while task tables are Done |
| Stale M1–M3 / phase banner | Still “Phase 1 Core”; M3 components unchecked |
| Spec headers lag code | `16_SimulationEngine`, Condenser/HR/Fan/Battery still RFI |
| Inventory lag | `23_Calibration` / `24_Optimization` Outline in inventory |
| Soft Done | AIR-005 network types not in code; AWG HR uses prescribed ε not NTU; Web `/models` missing |

## 2.2 Spec-described MVP gaps (code thinner than docs)

| Area | Missing vs spec |
|---|---|
| API `19_WebApi` | Rich summary KPIs; series `from`/`to`/`interval`; RFC 9457 Problem Details; `Idempotency-Key`; full model catalog metadata |
| Web `20_BlazorWeb` | 13-step wizard; `/models`; `/documentation`; config import/clone; full KPI cards; balance view; remove Counter/Weather |
| Data `21_DataModel` | Durable `SimulationJob`; `DiagnosticRecord`; User/ownership/retention; downsampled series in DB |
| Deploy `30_Deployment` | `/health/live` + `/health/ready`; worker image; metrics; retention; fuller CI |
| Tests `22_TestStrategy` | Architecture boundary tests (`DEV-008`); performance tests; real a11y/Web coverage; fuller scenario pack |

## 2.3 Explicit later / deferred (keep deferred unless prioritized)

| Area | Items |
|---|---|
| Engine | Nested SCC loops; timestep retry; `SelectedChannels` / `EventTriggered` |
| OPT | Random search, Nelder–Mead, DE, Bayesian; solar/battery objectives; design vars |
| CAL | Holdout validation; prototype measurement campaign (M5) |
| Topology | Dual-bed, two-stage TEC, latent HR, lab closed loop |
| APP2 | Entire second application (`APP2-001`…006) |
| Auth / hosted quotas | API authenticated mode |

---

# 3. Implementation sessions

Work one session at a time. Each session ends with tracker + CHANGELOG updates and green tests for touched projects.

### Session A — Tracker and inventory truth (S) — **do first**

| | |
|---|---|
| **Goal** | Make progress tracking trustworthy so agents stop restarting finished work |
| **IDs** | DOC-004 meta; inventory; M1–M3; §6–§7 dashboard |
| **Work** | Recount Done/Planned; set phase to post-M4; M3 → Done; clarify M5 “Calibration” = prototype-validated not code MVP; bump thin-spec headers to Implemented where code matches; fix AIR-005/HR-003 notes (Deferred or honest MVP) |
| **AC** | Dashboard matches task tables; inventory CAL/OPT = Implemented; next-queue points here |
| **Size** | S |

### Session B — OSS diagrams and good-first issues (S–M)

| | |
|---|---|
| **Goal** | Public visual entry + contributor on-ramp |
| **IDs** | OSS-003, OSS-004 |
| **Work** | Architecture SVGs/Mermaid exports under `docs/Images/`; README links; 3–5 labeled good-first GitHub issues |
| **AC** | Images linked; issues published or draft bodies in `docs/` |
| **Size** | S–M |

### Session C — Engineering quality gates (M)

| | |
|---|---|
| **Goal** | Close DEV bootstrap debt |
| **IDs** | DEV-003, DEV-004, DEV-008 |
| **Work** | Analyzer / TreatWarningsAsErrors policy; `Directory.Packages.props` (or ADR deferral); NetArchTest-style boundary tests (Core ← AWG ← Api/Web) |
| **AC** | CI fails on boundary violations; package versions centralized or explicitly deferred |
| **Size** | M |

### Session D — API contract hardening (M)

| | |
|---|---|
| **Goal** | Close `19_WebApi` acceptance gaps that Web/result UX need |
| **IDs** | API follow-ups (new: API-011… if needed) |
| **Work** | Enrich summary KPIs (L/day, Wh/L, collected water, residuals already partial); series time downsample query; Problem Details; optional Idempotency-Key; expand model catalog DTO |
| **AC** | Spec §14/§15/§20 tested; OpenAPI updated |
| **Size** | M |

### Session E — Web result UX and pages (M)

| | |
|---|---|
| **Goal** | Close largest Blazor gaps vs `20_BlazorWeb` |
| **IDs** | WEB follow-ups |
| **Work** | `/models`, `/documentation` (or docs portal link); KPI cards; balance panel; richer charts; remove template Counter/Weather; update spec Notes |
| **AC** | Spec §3 main pages present; §11 overview usable without raw tables only |
| **Size** | M |

### Session F — Configuration wizard and profiles (M–L)

| | |
|---|---|
| **Goal** | Replace sectioned MVP editor with guided wizard + import |
| **IDs** | WEB-005 deepen |
| **Work** | Multi-step wizard aligned to §6; JSON import/clone/examples (§8/§23); keep server-side validate |
| **AC** | New user can configure AWG without editing all sections at once |
| **Size** | M–L |

### Session G — Durable jobs and ops (M–L)

| | |
|---|---|
| **Goal** | Persist jobs; ops-ready health |
| **IDs** | DATA follow-ups; deploy §11 |
| **Work** | Persist SimulationJob metadata; optional DiagnosticRecord; `/health/live` + `/health/ready`; SQLite retention note/tooling |
| **AC** | Restart Web and still list completed persisted jobs; readiness fails if DB missing |
| **Size** | M–L |

### Session H — OPT / CAL polish or AIR honesty (M) — pick one track

| Track | Goal | AC |
|---|---|---|
| **H1 OPT** | Random search + one new objective (solar or battery) | Console + tests; `24_Optimization.md` updated |
| **H2 CAL** | Holdout validation report | Spec §7/§9; sample split workflow |
| **H3 AIR** | Implement `AirflowNode`/`AirflowBranch` **or** mark network Deferred and fix AIR-005 status | Tracker honest; tests match claim |

---

# 4. Suggested order

```text
A (truth) → B (OSS) → C (DEV gates)
                 ↘ D (API) → E (Web UX) → F (wizard)
                 ↘ G (durable jobs/ops) when hosting matters
                 ↘ H (OPT/CAL/AIR) when product physics/tools matter
APP2-001…005 Done (`ThermoCore.App2.SolarAirHeater`). APP2-006 sizing deferred.
```

---

# 5. Out of scope for these sessions

- Inventing new component physics beyond documented models
- APP2 second application
- Bayesian / DE optimizers
- Full authenticated multi-tenant hosted SaaS

---

**End of Document**
