# ThermoCore architecture overview

```mermaid
flowchart TB
  subgraph clients [Clients]
    Web[ThermoCore.Web Blazor]
    ApiClient[HTTP clients]
    Console[ThermoCore.Console]
  end

  subgraph app [Application]
    Application[ThermoCore.Application]
    Persist[ThermoCore.Persistence]
  end

  subgraph domain [Domain]
    AWG[ThermoCore.AWG]
    Core[ThermoCore.Core]
  end

  Web --> Application
  ApiClient --> Api[ThermoCore.Api]
  Api --> Application
  Console --> AWG
  Console --> Persist
  Application --> AWG
  Application --> Persist
  AWG --> Core
  Persist --> AWG
```

Layering rules (enforced by architecture tests):

- Core must not reference AWG, Application, Api, Web, or Persistence
- AWG must not reference Application, Api, or Web
- Application may reference AWG, Core, and Persistence
