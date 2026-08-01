# Changelog

All notable changes to ThermoCore are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project aims to follow [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Summer diurnal 24 h AWG + PV/battery sizing for 0.5–3 L/day (`summer-diurnal`, DOC-031 modes)
- Controlled Full AWG silica-mass and Peltier-power sweeps at 35 °C / 50% RH (`full-flow-silica-matrix`, `full-flow-peltier-matrix`)
- Wire `RuleBasedAwgController` into `AwgSimulationRunner` (collector air-coupling gate + condenser cooling actuator)
- Full AWG ambient T×RH matrix with controller (`full-flow-ambient-matrix`: 20–35 °C × 30–60% RH); water yield now increases with RH
- Simulation SVG visuals: full-flow station train + bars, L/day heatmaps for full-awg-flow and dry-sunny-matrix
- Full AWG V3 flow pack + `full-flow` console command (HR+electrical, station T/RH/W report in `samples/scenarios/full-awg-flow/`)
- Dry-sunny AWG scenario matrix (30 cases: 10–35 °C × 1–5 kg silica, 30% RH, G=950, SOC 90%) under `samples/scenarios/dry-sunny-matrix/` with result heatmaps (`RESULTS.md`, `results-heatmap.svg`)
- `silicaGelDryAdsorbentMassKg` on `AwgRegressionScenario`
- APP2-006 sizing grid (`app2 --size`), console APP2 smoke run, synthetic multi-regime campaign CSV (`write-campaign`)
- Good-first backlog refresh + published GitHub issues #2–#4; MkDocs Components nav; `dry-cool-day` scenario
- M5 holdout validation (`holdout` console), model limitations doc, prototype campaign protocol
- OPT solar utilization + battery throughput/SOC-swing objectives on AWG summaries and sweeps
- `ThermoCore.App2.SolarAirHeater` second-application MVP (Core-only forced-air collector)
- Central Package Management via root `Directory.Packages.props` (ADR-003 migrated)
- Post-MVP sessions A–H: architecture tests, API KPIs/series downsample/Problem Details/`Idempotency-Key`, `/health/live|ready`, Blazor models/docs/simulations list + wizard/import, random search CLI
- Architecture overview + good-first-issue drafts (`docs/Images/`, `GOOD_FIRST_ISSUES.md`)
- Gap audit and session backlog (`docs/00_Project/NEXT_IMPLEMENTATION_SESSIONS.md`)
- PostgreSQL `IThermoCoreStore` provider with `BYTEA` series payloads (DATA-005)
- Persisted simulation list/compare API and Blazor compare source toggle (DATA-008)
- Offline Lychee internal-link check on MkDocs `site/` (DOCSITE-007)
- MkDocs Material documentation portal (`mkdocs.yml`, ADR-002, GitHub Pages workflow)
- SQLite result-series persistence (gzip channel payloads + descriptors) and `run --db`
- Bi-objective Pareto front on sweeps (max L/day, min Wh/L) (OPT-006)
- One-at-a-time sensitivity analysis (`sensitivity`) ranked by liters/day elasticity (OPT-003)
- Parameter grid sweeps (`sweep`) with liters/day and Wh/liter ranking (OPT-002)
- Sectioned Blazor AWG configuration wizard (collector, silica, condenser, HR)
- Bounded coordinate-descent parameter fitting (`calibrate`) and SQLite calibration provenance
- `ThermoCore.Persistence` SQLite store for configuration versions, summaries, calibration runs
- Measurement CSV import, timestamp alignment, and RMSE/MAE/bias comparison (CAL-002…005)
- Sample validation dataset under `samples/calibration/` and console `validate` command
- Blazor simulation comparison page and `GET /api/v1/simulations` list
- Linux Docker image / compose for `ThermoCore.Web`
- `ThermoCore.Application` shared job/result services used by API and Blazor
- API result endpoints: series, diagnostics, export (csv/json/zip) plus configurable resource limits
- Blazor MVP: home, psychrometrics, AWG configuration editor, simulation start/progress/cancel/summary/diagnostics/charts/export
- ASP.NET Core API v1: health, models, psychrometrics, configuration validate, simulation jobs/status/summary/cancel, OpenAPI
- AWG DOC-029 full result export packages with manifest hashes (AWG-017)
- System water/energy/dry-air balance verification reports (AWG-018/019)
- Weather-driven ambient/solar boundaries and 24-hour synthetic diurnal runs (AWG-016)
- Heat-recovery and recirculation V3 topology paths, including combined two-tear mode
- Multi-tear fixed-point solver in `SimulationEngine` (joint convergence across loop tears)
- PV rear-air channel path using dynamic electrothermal PV
- DOC-022 regression scenario catalog under `samples/scenarios/` (APP-006)
- Apache-2.0 license, contributing guide, CI build/test and tag release workflows
