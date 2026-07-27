# README_FIRST.md

## What this ZIP is

This is the clean ThermoCore documentation workspace.

It contains only:

```text
docs/
ai/
```

It intentionally contains no source code, solution, test project or build configuration.

## Extraction

Extract the ZIP into the root of your future ThermoCore repository so that you get:

```text
ThermoCore/
├── docs/
└── ai/
```

## Files you should copy from your local downloads

The following detailed files were generated earlier, but were not available in the active file workspace when this package was assembled:

```text
04_MathematicalModel.md
05_Psychrometrics.md
06_SolarCollector.md
03_PhysicalArchitecture.md, if you already downloaded it
```

Copy them over their marked placeholders:

```text
docs/02_Mathematics/04_MathematicalModel.md
docs/02_Mathematics/05_Psychrometrics.md
docs/03_Components/06_SolarCollector.md
docs/01_Architecture/03_PhysicalArchitecture.md
```

## Repaired files

The following previous short outlines were replaced by expanded versions:

```text
10_Condenser.md
11_HeatRecovery.md
12_BatteryAndPowerManagement.md
13_FanAndAirflow.md
25_NumericalMethods.md
```

New foundation documents:

```text
26_Constants.md
27_Units.md
```

## Where to start

1. Open `docs/00_Project/DOCUMENT_INVENTORY.md`.
2. Replace the local-required placeholders.
3. Open `docs/00_Project/IMPLEMENTATION_PROGRESS.md`.
4. Generate the next grouped documentation set.
5. Keep all AI-specific files under `ai/`.

## Recommended next grouped package

Foundation and testing group:

```text
22_TestStrategy.md
14_ControlSystem.md
15_SystemTopology.md
16_SimulationEngine.md
28_WeatherModel.md
```

These should be generated as detailed specifications, not outlines.
