# ThermoCore

Component-based thermodynamic simulation platform with an Atmospheric Water Generator (AWG) reference application.

## Structure

```text
src/
  ThermoCore.Core         # physics, math, simulation engine
  ThermoCore.AWG          # atmospheric water generator reference app
  ThermoCore.Application  # shared API/Web job and result services
  ThermoCore.Persistence  # SQLite store for configs / summaries / calibration
  ThermoCore.Console      # CLI host
  ThermoCore.Api          # ASP.NET Core API (/api/v1)
  ThermoCore.Web          # Blazor UI
tests/                    # xUnit test projects
docs/                     # engineering documentation
samples/                  # configs, scenarios, result packages
ai/                       # AI development workspace
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

GitHub Pages deploys from `.github/workflows/docs.yml` after enabling Pages (GitHub Actions source).

## License

Apache License 2.0 — see `LICENSE` and `docs/ADR/ADR-001-OpenSourceLicense.md`.

## Contributing

See `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, and `SECURITY.md`.
