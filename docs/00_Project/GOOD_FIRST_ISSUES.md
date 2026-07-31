# Good first issues (OSS-004)

Contributor on-ramp. Labels: `good first issue`, plus `documentation` or `enhancement`.

**Published on GitHub** (2026-07-31): [#2](https://github.com/tonidotnet/ThermoCore/issues/2), [#3](https://github.com/tonidotnet/ThermoCore/issues/3), [#4](https://github.com/tonidotnet/ThermoCore/issues/4).  
Extend with `pwsh scripts/publish-good-first-issues.ps1` if needed.

## Completed in-repo (draft wave)

| # | Title | Status |
|---|---|---|
| 1 | Expand MkDocs nav for component/math pages | Done (`mkdocs.yml`) |
| 2 | Add `dry-cool-day` regression scenario | Done |
| 3 | Improve Blazor empty-state copy | Done |
| 4 | Document console OPT examples in README | Done |
| 5 | Series downsample edge-case tests | Done |

## Open on GitHub

| Issue | Topic |
|---|---|
| [#2](https://github.com/tonidotnet/ThermoCore/issues/2) | APP2 sizing CLI axes (`--aperture` / `--flow` / `--irradiance`) |
| [#3](https://github.com/tonidotnet/ThermoCore/issues/3) | Link model limitations from Blazor Documentation |
| [#4](https://github.com/tonidotnet/ThermoCore/issues/4) | Weather provider howto note in docs |

## Additional backlog (not yet filed)

### Persist APP2 run summary to SQLite

**Why:** AWG runs can use `--db`; APP2 cannot yet.  
**Task:** Map `SolarAirHeaterRunResult` into a minimal stored summary or document why APP2 stays console-only.  
**Skills:** Persistence, C#.

### Add ambient + solar channels to synthetic campaign CSV

**Why:** Campaign fixture currently exports condenser outlet temperature only.  
**Task:** Extend `AwgSyntheticCampaignGenerator` to include ambient T and irradiance channels.  
**Skills:** C#, calibration CSV schema.
