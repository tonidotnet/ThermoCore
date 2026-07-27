# ThermoCore Architecture Context

## Layering

```text
ThermoCore.Web
    ↓
ThermoCore.Api
    ↓
ThermoCore.AWG
    ↓
ThermoCore.Core
```

## Dependency rules

Allowed:

- Web → API/Application
- API → AWG
- AWG → Core

Forbidden:

- Core → AWG
- Core → API
- Core → Web
- AWG → Web
- physics → persistence
- physics → UI

## Core responsibilities

- units;
- constants;
- psychrometrics;
- balances;
- diagnostics;
- graph abstractions;
- numerical solvers;
- simulation engine.

## AWG responsibilities

- component selection;
- AWG topology;
- AWG controller;
- AWG-specific configuration;
- scenario orchestration.

## State model

Use:

```text
Evaluate → proposed result
Commit → accepted state
```

Evaluation must not mutate committed state.
