#Requires -Version 7.0
<#
.SYNOPSIS
  Creates GitHub issues from the remaining good-first backlog (OSS-004).
#>
$ErrorActionPreference = "Stop"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI (gh) not found. Install from https://cli.github.com/ then run: gh auth login"
}

gh auth status 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Not authenticated. Run: gh auth login"
}

$issues = @(
    @{
        Title = "good first issue: APP2 sizing CLI axes (--aperture/--flow/--irradiance)"
        Body  = @"
## Why
``app2 --size`` uses a fixed aperture × flow × irradiance grid.

## Task
Parse optional CSV lists for ``SolarAirHeaterSizingRunner`` (e.g. ``--aperture 1,2,4 --flow 0.03,0.05 --irradiance 400,800``).

## Refs
- ``src/ThermoCore.App2.SolarAirHeater/SolarAirHeaterSizingRunner.cs``
- ``src/ThermoCore.Console/DemoHost.cs``
- ``docs/00_Project/GOOD_FIRST_ISSUES.md`` item A
"@
        Labels = @("good first issue", "enhancement")
    },
    @{
        Title = "good first issue: link model limitations from Blazor Documentation"
        Body  = @"
## Why
``docs/06_Testing/26_ModelLimitations.md`` is published on MkDocs but easy to miss in the Blazor app.

## Task
Add a clear link/bullet on ``/documentation`` to the model limitations page (external docs URL or repo-relative note).

## Refs
- ``docs/00_Project/GOOD_FIRST_ISSUES.md`` item C
"@
        Labels = @("good first issue", "documentation")
    },
    @{
        Title = "good first issue: multi-channel synthetic campaign CSV"
        Body  = @"
## Why
``AwgSyntheticCampaignGenerator`` currently writes condenser outlet temperature only.

## Task
Also export ambient temperature and solar irradiance channels using the long-format CSV schema.

## Refs
- ``src/ThermoCore.AWG/Calibration/AwgSyntheticCampaignGenerator.cs``
- ``samples/calibration/README.md``
- ``docs/00_Project/GOOD_FIRST_ISSUES.md`` item D
"@
        Labels = @("good first issue", "enhancement")
    },
    @{
        Title = "good first issue: weather provider howto note in docs"
        Body  = @"
## Why
Custom weather drivers are a common contributor entry point.

## Task
Add a short “How to plug a custom ``IWeatherProvider``” note under ``docs/04_Simulation/28_WeatherModel.md`` (or MkDocs nav polish) and verify ``mkdocs build``.

## Refs
- ``docs/00_Project/GOOD_FIRST_ISSUES.md`` item E
"@
        Labels = @("good first issue", "documentation")
    }
)

foreach ($issue in $issues) {
    $labelArgs = @()
    foreach ($label in $issue.Labels) {
        $labelArgs += @("--label", $label)
    }

    Write-Host "Creating: $($issue.Title)"
    gh issue create --title $issue.Title --body $issue.Body @labelArgs
}
