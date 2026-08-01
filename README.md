# ThermoCore

Component-based thermodynamic simulation platform with an Atmospheric Water Generator (AWG) reference application.

## Structure

```text
src/
  ThermoCore.Core                 # physics, math, simulation engine
  ThermoCore.AWG                  # atmospheric water generator reference app
  ThermoCore.App2.SolarAirHeater  # second app (forced-air solar collector)
  ThermoCore.Application          # shared API/Web job and result services
  ThermoCore.Persistence          # SQLite store for configs / summaries / calibration
  ThermoCore.Console              # CLI host
  ThermoCore.Api                  # ASP.NET Core API (/api/v1)
  ThermoCore.Web                  # Blazor UI
tests/                            # xUnit test projects
docs/                             # engineering documentation
samples/                          # configs, scenarios, result packages
ai/                               # AI development workspace
```

## Prerequisites

- .NET SDK 10.0 or newer (see `global.json`)

## Quick start

```bash
dotnet build ThermoCore.sln
dotnet test ThermoCore.sln

# Console AWG run + DOC-029 export
dotnet run --project src/ThermoCore.Console -- run samples/awg-v3-mvp.json --duration 30 --dt 1 --export samples/results/awg-v3-mvp-smoke

# Level-5 regression pack
dotnet run --project src/ThermoCore.Console -- regress

# Dry sunny matrix (T=10…35 °C × silica 1…5 kg, 30% RH, strong sun, high battery)
dotnet run --project src/ThermoCore.Console -- regress --dir samples/scenarios/dry-sunny-matrix

# Full AWG flow (HR + electrical) + station T/RH/W diagram
dotnet run --project src/ThermoCore.Console -- full-flow
dotnet run --project src/ThermoCore.Console -- regress --dir samples/scenarios/full-awg-flow

# Full AWG ambient matrix (T=20…35 °C × RH=30…60%) + summary table
dotnet run --project src/ThermoCore.Console -- full-flow-ambient-matrix

# Full AWG silica / Peltier sweeps @ 35 °C / 50% RH
dotnet run --project src/ThermoCore.Console -- full-flow-silica-matrix
dotnet run --project src/ThermoCore.Console -- full-flow-peltier-matrix

# Summer diurnal 24h + PV/battery sizing for 0.5–3 L/day
dotnet run --project src/ThermoCore.Console -- summer-diurnal

# HTTP API
dotnet run --project src/ThermoCore.Api
# GET  /api/v1/health
# GET  /openapi/v1.json
# POST /api/v1/psychrometrics/calculate
# POST /api/v1/configurations/validate
# POST /api/v1/simulations

# Blazor UI (in-process application services)
dotnet run --project src/ThermoCore.Web

# Measurement validation (CAL)
dotnet run --project src/ThermoCore.Console -- validate samples/calibration/awg-mvp-ambient-smoke.csv --duration 3 --dt 1 --max-rmse 1e-6

# Parameter calibration + SQLite provenance
dotnet run --project src/ThermoCore.Console -- calibrate samples/calibration/awg-mvp-ambient-smoke.csv --duration 3 --dt 1 --params condenser.bypassFactor --db samples/results/calibration.db

# Linux container (Web)
docker compose up --build
# http://localhost:8080
```

## Documentation

Start at `README_FIRST.md`, then `docs/00_Project/DOCUMENT_INVENTORY.md` and `docs/00_Project/IMPLEMENTATION_PROGRESS.md`.

API contract: `docs/05_Product/19_WebApi.md`.

Browseable docs portal (MkDocs Material):

```bash
pip install -r requirements-docs.txt
mkdocs serve
```

Architecture diagram: [`docs/Images/architecture-overview.md`](docs/Images/architecture-overview.md).  
Session backlog: [`docs/00_Project/NEXT_IMPLEMENTATION_SESSIONS.md`](docs/00_Project/NEXT_IMPLEMENTATION_SESSIONS.md).  
Good first issues: [`docs/00_Project/GOOD_FIRST_ISSUES.md`](docs/00_Project/GOOD_FIRST_ISSUES.md).

### Optimization (console)

```bash
dotnet run --project src/ThermoCore.Console -- sweep --params condenser.bypassFactor=0.1,0.2 --duration 10 --dt 1
dotnet run --project src/ThermoCore.Console -- sensitivity --duration 10 --dt 1
dotnet run --project src/ThermoCore.Console -- random-search --samples 20 --seed 42 --duration 10 --dt 1
```

### Holdout / campaign (M5 workflow)

```bash
dotnet run --project src/ThermoCore.Console -- write-campaign samples/calibration/awg-mvp-campaign-synthetic.csv
dotnet run --project src/ThermoCore.Console -- holdout samples/calibration/awg-mvp-campaign-synthetic.csv \
  --duration 8 --dt 1 --train-fraction 0.7 --params condenser.bypassFactor
```

### APP2 solar air heater

```bash
dotnet run --project src/ThermoCore.Console -- app2
dotnet run --project src/ThermoCore.Console -- app2 --size
```

GitHub Pages deploys from `.github/workflows/docs.yml` after enabling Pages (GitHub Actions source).

## License

Apache License 2.0 — see `LICENSE` and `docs/ADR/ADR-001-OpenSourceLicense.md`.

## Contributing

See `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, and `SECURITY.md`.
