# ThermoCore
## 22_TestStrategy.md

**Version:** 1.0  
**Status:** Implemented  
**Document Type:** Test and validation strategy  
**Applies To:** All ThermoCore projects and documentation-driven implementations

---

# 1. Purpose

This document defines the required testing strategy for ThermoCore.

The objective is not merely code coverage. Tests shall establish:

- mathematical correctness;
- conservation;
- deterministic behavior;
- architecture boundaries;
- physical-domain validation;
- stable numerical behavior;
- reproducible integration results;
- trustworthy web/API behavior.

# 2. Test layers

```text
Level 1 — Pure mathematical unit tests
Level 2 — Physical state and component unit tests
Level 3 — Component integration tests
Level 4 — Graph and engine tests
Level 5 — Full AWG scenario tests
Level 6 — Calibration and prototype validation
Level 7 — API and Web contract tests
```

# 3. Unit-test framework

Recommended:

```text
xUnit
```

Optional assertion library may be used consistently.

Test projects shall not depend on the internet, local culture, local timezone or real current time.

# 4. Naming convention

```text
Method_Scenario_ExpectedResult
```

Examples:

```csharp
CalculateDewPoint_KnownReference_ReturnsExpectedTemperature()
Evaluate_InsufficientCooling_LimitsCondensation()
Run_RecirculationLoop_ConvergesWithinConfiguredIterations()
```

# 5. Test data principles

Every reference-value test shall document:

```text
Source
Formula or table
Input units
Expected output
Tolerance
Model version
```

Unexplained expected values are prohibited.

# 6. Mathematical unit tests

Required areas:

```text
Unit conversions
Physical constants
Saturation pressure
Humidity ratio
Dew point inversion
Enthalpy
Density
Root solvers
Interpolation
Fixed-point iteration
Time integration
```

# 7. Round-trip tests

Examples:

```text
RH → humidity ratio → RH
Temperature C → K → C
m³/h → m³/s → m³/h
Dew point → vapor pressure → dew point
Enthalpy and humidity ratio → temperature → enthalpy
```

# 8. Conservation tests

Every physical component shall assert applicable balances:

```text
Dry air
Water
Energy
Electrical energy
Liquid inventory
Stored energy
```

A plausible outlet is not sufficient if a balance residual is invalid.

# 9. Boundary tests

Required boundary categories:

```text
Zero flow
Zero power
Zero irradiance
Zero vapor
100% relative humidity
Minimum and maximum temperatures
Battery SOC bounds
Adsorbent loading bounds
Tank capacity
Maximum fan pressure
Peltier current and temperature limits
```

# 10. Invalid-input tests

Each public API shall test:

```text
NaN
Infinity
Negative mass or area
Invalid fractions
Pressure below vapor pressure
Invalid topology
Missing required parameter
Duplicate ID
Unsupported model selection
```

# 11. Determinism tests

Run the same scenario multiple times and assert:

- identical status;
- identical diagnostics;
- identical step count;
- identical result values within exact serialization or configured numerical policy;
- identical graph ordering.

# 12. Timestep sensitivity

Dynamic models shall be tested at multiple timesteps.

The expected behavior is convergence toward a stable result as timestep decreases.

Document acceptable differences for:

```text
Total collected water
Peak temperature
Battery SOC
Adsorbent loading
Energy residual
```

# 13. Solver tests

Every solver shall include:

- convergent case;
- endpoint solution;
- poor initial guess;
- invalid bracket;
- divergence;
- oscillation;
- maximum iterations;
- non-finite intermediate value;
- deterministic termination reason.

# 14. Component test requirements

## Solar panel

- reference-condition output;
- temperature coefficient;
- rear-air cooling;
- disconnected condition;
- energy balance.

## Solar collector

- zero irradiance;
- sensible heating;
- stagnation;
- increased airflow;
- wind loss;
- energy balance.

## Peltier

- \(Q_h=Q_c+P_e\);
- off-state conduction;
- maximum current;
- hot-side overtemperature;
- dynamic response.

## Silica gel

- zero driving force;
- adsorption;
- desorption;
- storage and capacity limits;
- energy-limited regeneration;
- water balance.

## Condenser

- no condensation above dew point;
- condensation onset;
- power limitation;
- drainage;
- water and energy balances.

## Heat recovery

- equal inlet temperature;
- known effectiveness;
- bypass;
- no water transfer in sensible-only mode;
- pressure drop.

## Fan and airflow

- system curve;
- fan curve intersection;
- no operating point;
- mixer and splitter;
- recirculation balance.

## Battery and power

- charge;
- discharge;
- limits;
- efficiencies;
- load shedding;
- curtailment.

# 15. Architecture tests

Automated tests should enforce:

```text
ThermoCore.Core does not reference ThermoCore.AWG
ThermoCore.Core does not reference API or UI frameworks
ThermoCore.AWG does not reference Web
Physics namespaces do not reference persistence
UI contains no physical formulas
```

# 16. Graph tests

Required:

```text
Valid acyclic graph
Cycle detection
Valid loop definition
Invalid port-domain connection
Missing required port
Duplicate component ID
Unconnected sink
Deterministic topological order
```

# 17. Full scenario regression tests

Store canonical scenarios under:

```text
samples/scenarios/
```

Examples:

```text
Dry cool day
Warm humid day
Hot dry day
High solar regeneration
Low battery
No recirculation
50% recirculation
Peltier derated
Tank full
```

# 18. Golden-result files

Golden results may be used when:

- configuration is stored beside output;
- model versions are stored;
- numerical settings are stored;
- changes are reviewed manually;
- snapshots are not updated automatically merely to pass tests.

# 19. API tests

Required:

- request validation;
- DTO-to-Core conversion;
- structured problem details;
- simulation creation;
- status;
- cancellation;
- result retrieval;
- resource-limit enforcement;
- no stack-trace leakage.

# 20. Web tests

Required:

- form validation;
- unit conversion display;
- start and cancel flow;
- result summary;
- chart downsampling;
- error display;
- accessibility smoke tests.

# 21. Performance tests

Performance testing shall not replace correctness.

Measure:

```text
Steps per second
Memory per run
Allocation rate
Loop iteration count
Result serialization size
Concurrent scenario throughput
```

# 22. Calibration tests

After prototype data exists:

- sensor-data import;
- time alignment;
- missing-data handling;
- RMSE and bias;
- parameter-fit reproducibility;
- holdout validation.

# 23. CI test stages

Recommended pipeline:

1. restore;
2. format verification;
3. build Release;
4. unit tests;
5. integration tests;
6. architecture tests;
7. optional coverage;
8. documentation-link checks;
9. Linux compatibility.

# 24. Coverage policy

Coverage is informative.

High-priority coverage:

```text
Psychrometrics
Conservation balances
Numerical solvers
Peltier
Silica gel
Condenser
Battery
Graph validation
```

Meaningless coverage-only tests are prohibited.

# 25. Test fixtures and factories

Provide reusable factories for:

```text
Ambient air states
Reference weather
Battery states
Silica-gel states
Peltier parameters
Simulation contexts
```

Factories shall use physically consistent states.

# 26. Tolerance policy

Tests shall use quantity-specific tolerances from `25_NumericalMethods.md`.

Do not use one universal tolerance for all quantities.

# 27. Failure reporting

A failing physical test should report:

```text
Inputs
Expected value
Actual value
Absolute error
Relative error
Tolerance
Model version
Balance residuals
```

# 28. Definition of passing

A physical component is `Passing` when:

- required unit tests pass;
- required balance tests pass;
- invalid inputs are handled;
- deterministic test passes;
- integration tests with adjacent components pass.

It is `Validated` only after external reference or measurement comparison.

# 29. Required documentation tests

Optional automation should verify:

- every planned document is listed in inventory;
- internal links resolve;
- document status is valid;
- JSON graphs reference existing files;
- no implementation-ready document is empty.

# 30. Acceptance criteria

The test strategy is accepted when:

1. every implemented physical module has balance tests;
2. all critical numerical algorithms have failure-path tests;
3. architecture boundaries are automated;
4. full scenarios are reproducible;
5. validation status is distinct from implementation status;
6. CI can run without proprietary local dependencies.

---

**End of Document**
