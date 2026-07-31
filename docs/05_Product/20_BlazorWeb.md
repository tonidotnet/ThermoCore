# ThermoCore
## 20_BlazorWeb.md

**Version:** 1.0  
**Status:** Implemented  
**Document Type:** Blazor Web application specification  
**Applies To:** ThermoCore.Web  
**Notes:** Pages include home, psychrometrics, stepped config wizard with JSON import, simulations list, compare, models, documentation, KPI cards, and basic charts. 13-step marketing wizard and full §12 chart set remain optional polish.

---

# 1. Purpose

This document defines the browser-based ThermoCore user interface.

The Web application shall allow users to:

- calculate psychrometric properties;
- configure AWG simulations;
- run and cancel simulations;
- inspect progress;
- visualize results;
- compare runs;
- export configuration and results;
- understand diagnostics and physical limits.

# 2. Recommended hosting model

Initial recommendation:

```text
Blazor Web App
Interactive Server rendering for simulation workflows
```

The UI shall call application/API services and shall not contain physical equations.

# 3. Main pages

```text
/
 /psychrometrics
 /simulations/new
 /simulations/{id}
 /simulations/{id}/results
 /simulations/compare
 /configurations
 /models
 /documentation
```

# 4. Home page

Content:

- ThermoCore purpose;
- AWG reference application;
- current model status;
- validation disclaimer;
- quick links;
- example result screenshot;
- open-source repository link later.

# 5. Psychrometric calculator

Inputs:

```text
Temperature
Relative humidity
Pressure
Optional airflow
```

Outputs:

```text
Humidity ratio
Dew point
Enthalpy
Specific volume
Vapor pressure
```

The page uses API or shared application contracts.

# 6. Simulation configuration wizard

Suggested steps:

1. scenario and time range;
2. weather;
3. topology;
4. solar panel;
5. solar collector;
6. Peltier;
7. silica gel;
8. condenser;
9. airflow and fan;
10. battery and power;
11. control strategy;
12. result capture;
13. validation and review.

# 7. Form rules

- show units beside every value;
- provide sensible examples;
- distinguish required and optional fields;
- display valid range;
- never silently change invalid input;
- surface warnings separately from errors;
- support advanced sections collapsed by default.

# 8. Configuration profiles

Users may:

```text
Start from default profile
Load example profile
Import JSON
Clone saved configuration
Reset a section
```

# 9. Simulation start

Before start:

- validate locally for basic form errors;
- call server validation;
- show warnings;
- require explicit acknowledgement for engineering-estimate parameters where configured.

# 10. Progress page

Display:

```text
Job status
Progress
Simulation time
Current mode
Completed steps
Warnings
Cancellation control
```

Do not display wall-clock estimates as guaranteed completion times.

# 11. Result overview

Key cards:

```text
Collected water
Liters per day
Wh per liter
Solar energy used
Electrical energy used
Exhaust vapor loss
Battery minimum SOC
Maximum Peltier hot-side temperature
Maximum collector temperature
Balance status
```

# 12. Charts

Initial charts:

```text
Ambient temperature and RH
Air temperature by measurement point
Dew point and condenser surface temperature
Water production rate
Cumulative collected water
Silica-gel loading
Battery SOC
PV generation and load
Peltier power and cooling
Fan flow and pressure
Energy balance residual
Water balance residual
```

# 13. Chart data

Large datasets shall be downsampled for rendering.

The full-resolution data remains downloadable.

Downsampling shall not alter summary values.

# 14. Diagnostics panel

Group by:

```text
Severity
Component
Code
Simulation time
```

Show:

- human-readable message;
- numeric context;
- suggested action;
- related component documentation link.

# 15. Balance view

Show:

```text
System water balance
Dry-air balance
Energy balance
Electrical balance
```

Include:

- inputs;
- outputs;
- storage change;
- residual;
- tolerance;
- pass/fail status.

# 16. Configuration editor architecture

Recommended feature folders:

```text
Features/Psychrometrics
Features/Simulation
Features/Configurations
Features/Results
Features/Models
Features/Documentation
```

# 17. View models

UI view models may contain display-unit values.

Mapping to API DTOs shall be explicit.

# 18. State management

Initial approach:

```text
Scoped services
Page-level state
URL-based simulation identifiers
```

Avoid global mutable singleton state.

# 19. Error handling

The UI shall distinguish:

```text
Form validation
Server validation
Simulation failure
Network failure
Cancellation
Authorization failure
```

# 20. Accessibility

Required:

- keyboard navigation;
- form labels;
- sufficient contrast;
- non-color-only status indicators;
- accessible tables;
- chart summaries.

# 21. Localization

Initial language may be English.

Architecture shall allow later localization.

Units shall be configurable independently from language.

# 22. Responsive design

Priority:

```text
Desktop
Tablet
Readable mobile summary
```

Complex configuration may remain desktop-first.

# 23. Configuration import

Import flow:

1. select JSON;
2. validate schema version;
3. migrate if supported;
4. display errors and warnings;
5. preview;
6. load into editor.

# 24. Export

Support:

```text
Configuration JSON
Result CSV
Result JSON
Summary report later
```

# 25. Comparison page

Compare:

```text
Two or more simulations
Summary metrics
Selected time series
Configuration differences
Diagnostics
```

# 26. Security

- no secrets in browser state;
- validate all server requests;
- protect private simulations;
- use antiforgery protections where applicable;
- avoid rendering untrusted HTML;
- constrain uploaded files.

# 27. Performance

- lazy-load heavy result views;
- virtualize large tables;
- downsample charts;
- avoid transferring full results unless requested;
- cache immutable model metadata.

# 28. Required tests

- psychrometric form validation;
- configuration wizard navigation;
- server validation display;
- start simulation;
- progress update;
- cancellation;
- summary rendering;
- diagnostic grouping;
- export;
- import invalid schema;
- accessibility smoke tests;
- equivalent user inputs produce equivalent API DTOs.

# 29. Acceptance criteria

The Web application is accepted when:

1. no physical formula exists in UI code;
2. every input unit is explicit;
3. invalid configuration is understandable;
4. long runs remain cancellable;
5. full results are exportable;
6. balance and diagnostic information is visible;
7. UI remains responsive for expected result sizes.

---

**End of Document**
