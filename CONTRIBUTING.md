# Contributing to ThermoCore

## Development setup

1. Install .NET SDK 10 (`global.json`).
2. Restore and build: `dotnet build ThermoCore.sln`
3. Run tests: `dotnet test ThermoCore.sln`

## Workflow

- Prefer small, reviewable changes tied to an ID in `docs/00_Project/IMPLEMENTATION_PROGRESS.md`.
- Keep one top-level C# type per source file.
- Use SI units in Core; convert at boundaries.
- Do not invent physics beyond the engineering specs under `docs/`.
- Update `IMPLEMENTATION_PROGRESS.md` when completing tracker tasks.

## Checks before opening a PR

- `dotnet build` and `dotnet test` succeed.
- New public behavior has tests.
- System water/energy balance checks still pass for AWG runs that claim conservation.
- Specs cited in code comments remain accurate.

## License

By contributing, you agree that your contributions are licensed under the Apache License 2.0 (`LICENSE`, `docs/ADR/ADR-001-OpenSourceLicense.md`).
