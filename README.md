# ThermoCore

Component-based thermodynamic simulation platform.

## Structure

```text
src/
  ThermoCore.Core      # physics, math, simulation engine
  ThermoCore.AWG       # atmospheric water generator reference app
  ThermoCore.Console   # CLI host
  ThermoCore.Api       # ASP.NET Core API
  ThermoCore.Web       # Blazor UI
tests/                 # xUnit test projects
docs/                  # engineering documentation
ai/                    # AI development workspace
```

## Prerequisites

- .NET SDK 10.0 or newer (see `global.json`)

## Build

```bash
dotnet build ThermoCore.sln
dotnet test ThermoCore.sln
dotnet run --project src/ThermoCore.Console
```

## Documentation

Start at `README_FIRST.md`, then `docs/00_Project/DOCUMENT_INVENTORY.md` and `docs/00_Project/IMPLEMENTATION_PROGRESS.md`.

## License

Apache License 2.0 — see `LICENSE` and `docs/ADR/ADR-001-OpenSourceLicense.md`.

## Contributing

See `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, and `SECURITY.md`.
