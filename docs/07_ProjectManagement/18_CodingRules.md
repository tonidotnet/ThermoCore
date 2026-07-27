# ThermoCore
## 18_CodingRules.md

**Version:** 1.0  
**Document Type:** Coding Standards and AI Implementation Rules  
**Status:** Active  
**Applies To:** All ThermoCore source code, tests, tools and generated code  
**Primary language:** C#  
**Primary runtime:** .NET 8 or newer  
**Primary architecture:** Platform-independent Core with Console, API and Web hosts

---

# 1. Purpose

This document defines mandatory coding rules for ThermoCore.

It is intended for:

- Human developers
- AI coding assistants
- Code generators
- Reviewers
- Automated analyzers

The goals are:

- Correct physical calculations
- Maintainable architecture
- Predictable generated code
- Cross-platform compatibility
- Deterministic simulation
- Testability
- Safe web deployment
- Clear units and state ownership

These rules take precedence over stylistic preferences suggested by an implementation tool.

---

# 2. Language and Runtime

Required:

```text
C#
.NET 8 or newer
Nullable reference types enabled
Implicit global usings allowed only when explicitly configured
```

The exact language version shall be compatible with the selected .NET SDK.

Do not specify an impossible combination such as a language version unsupported by the SDK.

Recommended project settings:

```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <InvariantGlobalization>false</InvariantGlobalization>
</PropertyGroup>
```

Warnings that cannot reasonably be treated as errors shall be explicitly documented and narrowly suppressed.

---

# 3. Cross-Platform Requirement

The following projects shall remain cross-platform:

```text
ThermoCore.Core
ThermoCore.AWG
ThermoCore.Console
ThermoCore.Api
ThermoCore.Web
All corresponding test projects
```

Prohibited dependencies in these projects:

```text
System.Windows
WPF
WinForms
Windows Registry
COM
Windows-only native libraries
Hard-coded Windows paths
Platform-specific UI dialogs
```

An optional `ThermoCore.Desktop` project may use WPF, but no other project may depend on it.

---

# 4. Dependency Direction

Allowed dependency direction:

```text
ThermoCore.Console ─┐
ThermoCore.Api     ─┼→ ThermoCore.AWG → ThermoCore.Core
ThermoCore.Web     ─┘
```

Where possible:

```text
ThermoCore.Web → ThermoCore.Api client contracts
ThermoCore.Api → ThermoCore.AWG
ThermoCore.AWG → ThermoCore.Core
```

Prohibited:

```text
ThermoCore.Core → ThermoCore.AWG
ThermoCore.Core → ThermoCore.Web
ThermoCore.AWG → ThermoCore.Web
ThermoCore.Core → database infrastructure
```

The Core project shall not know which application uses it.

---

# 5. General Design Rules

Mandatory:

1. Prefer simple, explicit code.
2. Avoid premature abstraction.
3. Every class shall have one clear responsibility.
4. Physical calculations shall be separated from UI, persistence and transport.
5. Component state shall have one owner.
6. Every mass or energy transfer shall be explicit.
7. No hidden mutation across components.
8. Avoid service locators.
9. Avoid global mutable state.
10. Avoid reflection-based magic in physical calculations.
11. Prefer composition over inheritance.
12. Do not add interfaces unless they support substitution, testing or multiple implementations.
13. Do not redesign unrelated modules while implementing a task.

---

# 6. Namespace Rules

Recommended namespaces:

```text
ThermoCore.Core
ThermoCore.Core.Components
ThermoCore.Core.Graph
ThermoCore.Core.Physics
ThermoCore.Core.Psychrometrics
ThermoCore.Core.Simulation
ThermoCore.Core.Validation
ThermoCore.Core.Diagnostics
ThermoCore.AWG
ThermoCore.AWG.Components
ThermoCore.AWG.Configuration
ThermoCore.AWG.Control
ThermoCore.Api
ThermoCore.Web
```

Namespace names shall match folder responsibilities, but physical folder structure shall not be excessively deep.

---

# 7. File and Type Rules

Required:

- One primary public type per file.
- File name shall match the primary type name.
- Small private nested types are allowed where they improve locality.
- Avoid files containing unrelated types.
- Avoid generic names such as `Helper`, `Manager`, `Utils`, `Common` or `Processor` without a clear domain qualifier.

Preferred examples:

```text
PsychrometricCalculator.cs
MoistAirState.cs
SolarCollectorParameters.cs
ConservationBalance.cs
SimulationGraphValidator.cs
```

Avoid:

```text
PhysicsHelper.cs
DataManager.cs
CommonUtils.cs
```

---

# 8. Naming Rules

Use English identifiers.

## Types and public members

Use PascalCase:

```csharp
MoistAirState
CalculateDewPointTemperatureK
EnergyResidualJ
```

## Local variables and parameters

Use camelCase:

```csharp
temperatureK
pressurePa
humidityRatio
```

## Private fields

Use underscore-prefixed camelCase:

```csharp
private readonly ISaturationPressureProvider _saturationPressureProvider;
```

## Constants

Use PascalCase:

```csharp
public const double CelsiusOffsetK = 273.15;
```

## Booleans

Use affirmative names:

```csharp
IsEnabled
HasConverged
CanRecirculate
```

Avoid negative forms such as:

```csharp
IsNotDisabled
```

---

# 9. Unit Naming Rules

Every ambiguous physical value shall include its unit in the identifier.

Required examples:

```csharp
TemperatureK
TemperatureC
PressurePa
PowerW
EnergyJ
MassKg
MassFlowKgPerSecond
VolumeM3
VolumetricFlowM3PerSecond
AreaM2
ThermalResistanceKPerW
SpecificEnthalpyJPerKgDryAir
HumidityRatioKgPerKgDryAir
RelativeHumidityFraction
```

Do not use:

```csharp
Temperature
Pressure
Flow
Energy
Humidity
```

unless the type itself enforces the unit unambiguously.

---

# 10. SI Internal Units

All Core calculations shall use SI units.

Required internal conventions:

```text
Temperature: K
Pressure: Pa
Time: s
Mass: kg
Mass flow: kg/s
Length: m
Area: m²
Volume: m³
Volumetric flow: m³/s
Power: W
Energy: J
Relative humidity: 0–1
Angles: rad
```

Input and presentation layers may use:

```text
°C
%
m³/h
Wh
kWh
L
mL
degrees
```

Conversions shall occur at boundaries only.

---

# 11. Numeric Type Rules

Use:

```csharp
double
```

for physical calculations.

Do not use:

```csharp
float
decimal
```

for the simulation engine unless a specific documented reason exists.

`decimal` may be used for financial or billing calculations outside the physics engine.

Every public calculation method shall reject:

```text
NaN
PositiveInfinity
NegativeInfinity
```

Use:

```csharp
double.IsFinite(value)
```

---

# 12. Floating-Point Comparison

Do not use exact equality for calculated floating-point values.

Prohibited:

```csharp
if (temperature == expectedTemperature)
```

Required approach:

```csharp
Math.Abs(actual - expected) <= absoluteTolerance
```

For general convergence:

```csharp
Math.Abs(newValue - oldValue)
    <= absoluteTolerance
       + relativeTolerance * Math.Max(Math.Abs(newValue), Math.Abs(oldValue));
```

Tolerance values shall be centralized and documented.

---

# 13. Magic Numbers

Magic numbers are prohibited in physical calculations.

Prohibited:

```csharp
return 0.621945 * vaporPressure / (pressure - vaporPressure);
```

Preferred:

```csharp
return PsychrometricConstants.MolecularMassRatio
       * vaporPressure
       / (pressure - vaporPressure);
```

Every physical constant shall include:

- Name
- Unit
- Value
- Source
- Validity or reference conditions where applicable

---

# 14. Immutable State

Prefer immutable records for:

```text
Physical states
Configuration
Parameters
Step results
Diagnostics
API contracts
```

Example:

```csharp
public sealed record MoistAirState
{
    public required double TemperatureK { get; init; }

    public required double PressurePa { get; init; }

    public required double HumidityRatioKgPerKgDryAir { get; init; }
}
```

Do not expose public setters on committed physical state unless required by a serializer and carefully isolated.

---

# 15. Authoritative State Variables

Do not allow callers to independently set mutually dependent physical properties.

Prohibited state creation:

```csharp
new MoistAirState
{
    TemperatureK = 300,
    RelativeHumidityFraction = 0.9,
    HumidityRatioKgPerKgDryAir = 0.001,
    DewPointTemperatureK = 295
};
```

Required:

```csharp
calculator.CreateFromHumidityRatio(
    temperatureK,
    pressurePa,
    humidityRatio,
    dryAirMassFlowKgPerSecond);
```

Derived values shall be calculated from authoritative inputs.

---

# 16. Evaluation and Commit

Stateful simulation components shall separate evaluation from mutation.

Required conceptual pattern:

```csharp
ComponentStepResult Evaluate(ComponentStepContext context);

void Commit(ComponentStepResult acceptedResult);
```

During `Evaluate`:

- Do not mutate committed state.
- Do not mutate other components.
- Do not write global data.
- Return proposed state and outputs.

During `Commit`:

- Apply the accepted result exactly once.
- Do not recalculate physics.
- Do not perform external I/O.

---

# 17. Component Communication

Components shall communicate only through:

```text
Typed ports
Simulation context
Explicit control signals
Explicit configuration
```

Prohibited:

- Directly changing another component's internal state
- Looking up arbitrary components through a global service locator
- Calling downstream component methods from inside a physical model
- Hidden static event communication

---

# 18. Conservation Rules

Every applicable component shall report:

```text
Input
Output
Storage change
Residual
```

For:

```text
Energy
Dry air
Water
Electrical energy
Other configured species
```

Never create condensed water without:

- Removing equal vapor mass
- Including latent heat
- Reporting liquid-water output

Never desorb more water than stored.

Never discard exhaust vapor without reporting it.

---

# 19. Clamping Rules

Clamping is allowed only for real physical constraints.

Allowed examples:

```text
Battery SOC limited to 0–1
Tank capacity
Maximum adsorbent loading
Maximum electrical power
Non-negative condensed-water mass
```

When clamping:

1. Record the unclamped value.
2. Report a diagnostic.
3. Create an explicit overflow, rejected-power or limiting term.
4. Preserve conservation.

Prohibited:

```csharp
relativeHumidity = Math.Clamp(relativeHumidity, 0, 1);
```

when the state is supersaturated and condensation has not been resolved.

---

# 20. Error Handling

Use domain-specific exceptions for invalid setup and programmer errors.

Examples:

```csharp
ConfigurationValidationException
PsychrometricInputException
SimulationGraphException
SimulationConvergenceException
PhysicalStateException
```

During simulation, expected physical limit events should normally be diagnostics rather than exceptions.

Examples:

```text
Battery depleted
Tank full
Peltier derated
Insufficient solar power
No condensation possible
```

Exceptions shall not be used for ordinary control flow.

---

# 21. Diagnostic Model

Diagnostics shall contain:

```text
Code
Severity
Message
Component identifier
Port identifier if applicable
Step index
Simulation time
Solver iteration
Relevant numeric values
Suggested action if known
```

Severity levels:

```csharp
Information
Warning
Error
Critical
```

Diagnostic codes shall be stable strings or enums suitable for API clients.

---

# 22. Logging Rules

Use structured logging in host applications.

Recommended:

```csharp
ILogger<T>
```

Do not log from low-level pure mathematical functions unless failure context cannot otherwise be returned.

Do not use:

```csharp
Console.WriteLine
Debug.WriteLine
MessageBox.Show
```

inside Core or AWG libraries.

Do not log secrets, tokens or private user data.

---

# 23. Async Rules

Pure calculation methods shall normally be synchronous.

Use async for:

```text
Database access
File I/O
HTTP calls
Long-running job orchestration
Streaming results
```

Do not create fake async methods:

```csharp
Task.FromResult(Calculate())
```

unless required by a stable interface.

Simulation execution may expose an asynchronous host method for cancellation and progress, while the numerical loop remains synchronous internally.

---

# 24. Cancellation

Long-running simulations shall support:

```csharp
CancellationToken
```

Check cancellation:

- Between timesteps
- Between parameter-sweep scenarios
- During long solver iterations at reasonable intervals

Do not swallow `OperationCanceledException`.

---

# 25. Thread Safety

Core services shall be:

- Stateless, or
- Immutable, or
- Scoped to one simulation run

Avoid static mutable caches.

If caching is introduced:

- It must be thread-safe.
- It must not change numerical results.
- It must be optional.
- It must be covered by concurrency tests.

---

# 26. Determinism

Physical results shall not depend on:

```text
Wall-clock time
Local culture
Local timezone
Operating system
Thread scheduling
Unseeded randomness
Dictionary enumeration when order matters
```

Use explicit ordering for graph execution and result serialization where reproducibility matters.

---

# 27. Culture Rules

Internal parsing and formatting shall use explicit culture.

For machine-readable formats:

```csharp
CultureInfo.InvariantCulture
```

User-facing UI may use the user's culture.

JSON shall use standard invariant numeric representation.

CSV export shall document:

- Delimiter
- Decimal format
- Encoding
- Unit headers

---

# 28. Date and Time Rules

Use:

```csharp
DateTimeOffset
```

for real-world timestamps.

Use:

```csharp
TimeSpan
```

for durations and simulation timesteps.

Do not use local `DateTime.Now` in Core calculations.

Simulation time shall be supplied through `SimulationContext`.

Weather data timestamps shall include timezone or UTC offset.

---

# 29. Collection Rules

Expose read-only collections:

```csharp
IReadOnlyList<T>
IReadOnlyCollection<T>
IReadOnlyDictionary<TKey, TValue>
```

Avoid returning mutable internal collections.

Use arrays or immutable collections for high-frequency, fixed result sets when appropriate.

Do not use LINQ in performance-critical inner loops without measuring its cost.

---

# 30. Performance Rules

Correctness has priority over micro-optimization.

Before optimizing:

1. Create a benchmark or profiling case.
2. Measure the bottleneck.
3. Preserve deterministic output.
4. Add regression tests.

Avoid per-timestep allocations in very long simulations where practical.

Do not pool objects until profiling demonstrates value.

---

# 31. API Design

Public methods shall:

- Have clear domain-specific names.
- Expose units in names or types.
- Validate public inputs.
- Return immutable results.
- Avoid boolean parameter lists with unclear meaning.
- Avoid more than a reasonable number of primitive parameters.

Prefer parameter records:

```csharp
calculator.Calculate(parameters);
```

over long primitive lists when a domain object exists.

---

# 32. Optional Values

Use nullable values when absence is meaningful:

```csharp
double? RequestedVoltageV
```

Do not use sentinel values such as:

```text
−1
NaN
0 when zero is physically valid
```

unless the format requires it and validation converts it immediately.

---

# 33. Enums

Use enums for finite stable modes:

```csharp
PeltierControlMode
SimulationDiagnosticSeverity
PhysicalDomain
PortDirection
AwgOperatingMode
```

Do not encode modes as arbitrary strings inside Core.

API DTOs may serialize enums as strings for readability.

---

# 34. Configuration

Configuration records shall be immutable after simulation start.

Every configuration type shall have:

- Validation
- Default values only where physically meaningful
- Explicit units
- Schema or version metadata where persisted
- Example JSON

Do not silently substitute defaults for missing critical physical parameters.

---

# 35. Configuration Validation

Validation shall happen before simulation start.

Use:

- Dedicated validators
- `IValidateOptions<T>` in ASP.NET hosts where appropriate
- Domain validation methods in application layer

Validation errors shall include:

```text
Property path
Invalid value
Expected range
Unit
Message
```

Do not scatter validation logic across UI event handlers.

---

# 36. JSON Rules

Use `System.Text.Json` unless a documented requirement justifies another serializer.

Recommended:

```text
camelCase property names
String enum serialization
Explicit schemaVersion
No polymorphic deserialization from untrusted input without a whitelist
```

Do not serialize internal implementation objects directly as long-term public contracts.

Use API and persistence DTOs.

---

# 37. Web Architecture Rules

The Web application shall not execute large simulations directly on the UI rendering thread.

Recommended flow:

```text
Blazor UI
   ↓
ASP.NET Core API or application service
   ↓
Simulation job
   ↓
ThermoCore.AWG
   ↓
ThermoCore.Core
```

For short calculations, direct request-response execution is acceptable.

For long simulations:

- Create a job.
- Return job identifier.
- Support status polling or streaming.
- Support cancellation.
- Store result or temporary result reference.

---

# 38. API Rules

API endpoints shall:

- Use versioned routes when public stability is required.
- Validate DTOs.
- Return structured errors.
- Avoid leaking stack traces.
- Use cancellation tokens.
- Apply request-size limits.
- Apply simulation resource limits.
- Use OpenAPI documentation.

Physics exceptions shall be translated to appropriate HTTP problem details.

---

# 39. Blazor Rules

Blazor components shall contain presentation and orchestration only.

Do not put physical formulas in:

```text
.razor files
code-behind files
JavaScript
chart configuration
```

Use typed view models.

Large result sets shall be:

- Downsampled for charts
- Paginated for tables
- Downloadable as full CSV or JSON

---

# 40. Persistence Rules

Persistence models shall not become Core physical models.

Use explicit mapping:

```text
Database entity
   ↔
Application model
   ↔
Core configuration/state
```

Store with each simulation:

```text
Configuration schema version
Model version
Application version
Simulation start time
Numerical settings
Component fidelity levels
Relevant parameter sources
```

Do not claim reproducibility without storing model metadata.

---

# 41. Security Rules

For web deployment:

- Validate all numeric ranges.
- Limit simulation duration.
- Limit timestep count.
- Limit parameter-sweep size.
- Limit uploaded file size.
- Do not execute user-supplied code.
- Do not deserialize arbitrary types.
- Protect authenticated data.
- Use anti-forgery protections where applicable.
- Use HTTPS.
- Keep secrets outside source control.

---

# 42. Testing Rules

Every physical module shall have:

```text
Unit tests
Boundary tests
Invalid-input tests
Conservation tests
Determinism tests
Integration tests where connected behavior matters
```

Recommended test framework:

```text
xUnit
```

Assertion library may be used if approved consistently.

Tests shall not depend on:

```text
Internet
Local timezone
Local culture
Execution order
Real current time
```

---

# 43. Test Naming

Recommended pattern:

```text
Method_Scenario_ExpectedResult
```

Examples:

```csharp
CreateFromHumidityRatio_SensibleHeating_DewPointUnchanged()
Evaluate_HotSideOverTemperature_ReturnsCriticalDiagnostic()
Mix_TwoMoistAirStreams_ConservesWaterAndEnthalpy()
```

---

# 44. Reference-Value Tests

Reference-value tests shall state:

- Source
- Input units
- Expected result
- Allowed tolerance
- Formula or model version

Do not use unexplained expected values.

---

# 45. Conservation Tests

Every relevant test shall assert both output and residual.

Example:

```csharp
result.CondensedWaterMassKg.Should().BeGreaterThan(0);
result.Balance.WaterMassResidualKg.Should().BeApproximately(0, tolerance);
result.Balance.EnergyResidualJ.Should().BeApproximately(0, tolerance);
```

A plausible output is not sufficient when conservation is violated.

---

# 46. Snapshot and Golden Tests

Golden result files may be used for full simulations.

Rules:

- Store input configuration beside result.
- Store model version.
- Use stable serialization.
- Review every golden-file update.
- Do not update snapshots automatically merely to make tests pass.

---

# 47. Integration Tests

Required integration paths include:

```text
Console → AWG → Core
API → AWG → Core
Web application service → AWG → Core
```

The same configuration shall produce equivalent results across hosts.

---

# 48. Documentation Rules

Every public type and member shall have XML documentation unless self-evident and internal policy allows omission.

Physical formulas shall include references in:

- Specification documents
- Code comments near non-obvious formulas
- Parameter metadata where applicable

Comments shall explain:

```text
Why
Assumptions
Units
Validity range
Source
```

Do not write comments that merely restate code.

---

# 49. XML Documentation Example

```csharp
/// <summary>
/// Calculates saturation vapor pressure over liquid water.
/// </summary>
/// <param name="temperatureK">
/// Absolute temperature in kelvin.
/// </param>
/// <returns>
/// Saturation vapor pressure in pascals.
/// </returns>
/// <exception cref="ArgumentOutOfRangeException">
/// Thrown when the temperature is outside the model validity range.
/// </exception>
public double CalculatePressurePa(double temperatureK);
```

---

# 50. Source References

Every empirical correlation shall identify:

```text
Reference title
Author or organization
Publication/version
Equation or table identifier if available
Validity range
Access date where relevant
```

Manufacturer data shall identify:

```text
Manufacturer
Part number
Datasheet revision
Reference temperature
```

Engineering estimates shall be explicitly marked as estimates.

---

# 51. Analyzer and Formatting Rules

Recommended:

```text
dotnet format
.NET analyzers
StyleCop or equivalent only if the team accepts the rule set
```

Avoid excessive analyzer packages that create noise without improving correctness.

Formatting shall be automated.

Do not manually align code with spaces in ways the formatter will undo.

---

# 52. Code Style

Use file-scoped namespaces where consistent.

Use braces for all control blocks.

Preferred:

```csharp
if (condition)
{
    Execute();
}
```

Avoid:

```csharp
if (condition)
    Execute();
```

Use expression-bodied members only when readability improves.

---

# 53. Null Handling

Nullable reference types shall be enabled.

Do not suppress nullability warnings with `!` unless:

- The invariant is proven.
- A comment explains why.
- The design cannot reasonably represent the value non-nullably.

Prefer constructor validation and required properties.

---

# 54. Dependency Injection

Use constructor injection in host and application layers.

Core mathematical services may be created directly when simple and immutable.

Avoid injecting large service containers into components.

Do not access `IServiceProvider` inside domain calculations.

---

# 55. External Libraries

Before adding a package, verify:

```text
Need
License
Maintenance status
Cross-platform compatibility
Security history
Transitive dependencies
AOT/WebAssembly implications if relevant
```

Do not add a package for a trivial calculation that can be implemented correctly and tested locally.

Use established libraries for:

```text
Charts
Persistence
Authentication
Logging
Testing
```

where they clearly reduce risk.

---

# 56. Database Rules

Database access shall be asynchronous.

Use migrations.

Do not store large timestep result arrays in a single unbounded JSON field without a deliberate design decision.

Consider:

```text
Run metadata table
Summary table
Compressed result file/object storage
Downsampled chart series
```

Retention policy shall be configurable.

---

# 57. Result Data Rules

Every result quantity shall identify:

```text
Name
Unit
Timestamp or step
Component or measurement point
Model version
```

Do not round authoritative stored results.

Round only for display.

---

# 58. Versioning

Use semantic versioning for public packages and APIs where appropriate.

Configuration documents shall include:

```json
{
  "schemaVersion": "1.0"
}
```

Breaking configuration changes require:

- New schema version
- Migration or clear error
- Documentation

---

# 59. Architecture Decision Records

Significant decisions shall be recorded under:

```text
docs/Decisions/
```

Examples:

```text
ADR-001 Port-Based Graph Architecture
ADR-002 ThermoCore as Generic Framework
ADR-003 Web-First User Interface
ADR-004 SI Units in Core
ADR-005 Fixed-Point Solver for Initial Cycles
```

Do not rely on chat history as the only record of architecture decisions.

---

# 60. AI Coding Rules

An AI coding system shall:

1. Read the relevant specification before coding.
2. Implement only the requested milestone.
3. Preserve existing public contracts unless change is required.
4. Add tests with the implementation.
5. Build the full solution.
6. Run all tests.
7. Report assumptions and deviations.
8. Avoid placeholder formulas unless explicitly requested.
9. Mark engineering estimates clearly.
10. Avoid introducing unrelated frameworks.
11. Avoid creating dozens of abstractions before concrete use exists.
12. Never put physics in UI code.
13. Never silently clamp physical inconsistencies.
14. Never delete failing tests to obtain a green build.
15. Never change expected reference values without explanation.
16. Prefer a partial correct implementation over invented precision.

---

# 61. AI Output Requirements

For each implementation task, the AI shall report:

```text
Files created
Files modified
Tests added
Commands run
Build result
Test result
Assumptions
Known limitations
Specification deviations
Recommended next task
```

Do not claim successful build or tests unless they were actually run.

---

# 62. Prohibited AI Behaviors

The AI shall not:

- Invent unavailable manufacturer data.
- Claim model validation without reference or measurement.
- Replace equations with arbitrary percentages.
- Add UI dependencies to Core.
- Generate fake repositories or package names.
- Swallow exceptions.
- Disable analyzers broadly.
- Mark unimplemented methods as successful.
- Return hard-coded output for tests.
- Use random values to make simulations look realistic.
- Ignore units.
- Duplicate physical logic across projects.

---

# 63. Commit and Pull Request Guidance

Recommended commit scope:

```text
One milestone or one coherent change
```

Commit messages should describe intent:

```text
Add humidity-ratio based moist-air state factory
Implement acyclic simulation graph validation
Add Peltier thermal balance tests
```

Pull requests shall include:

```text
Summary
Specification reference
Test evidence
Balance impact
API changes
Known limitations
```

---

# 64. Build Commands

Recommended baseline commands:

```bash
dotnet restore
dotnet build ThermoCore.sln --configuration Release
dotnet test ThermoCore.sln --configuration Release --no-build
dotnet format ThermoCore.sln --verify-no-changes
```

Host-specific projects may add:

```bash
dotnet run --project src/ThermoCore.Console
dotnet run --project src/ThermoCore.Api
dotnet run --project src/ThermoCore.Web
```

---

# 65. CI Requirements

Continuous integration shall:

1. Restore dependencies.
2. Build Release configuration.
3. Run unit and integration tests.
4. Verify formatting.
5. Publish test results.
6. Optionally collect code coverage.
7. Build Linux-compatible web artifacts.
8. Fail on warnings when configured.

CI shall not require proprietary local tools for the base build.

---

# 66. Code Coverage

Coverage is a diagnostic, not the goal.

Priority areas for high coverage:

```text
Psychrometrics
Conservation balances
Mixers and splitters
Peltier equations
Silica-gel mass balance
Condenser latent heat
Battery bounds
Graph validation
```

Do not write meaningless tests solely to increase percentage.

---

# 67. Review Checklist

Before merging physical-model code, verify:

```text
Units explicit
Inputs validated
No NaN/Infinity propagation
Conservation residual included
State ownership clear
No UI dependency
No hidden mutation
Formula source documented
Validity range documented
Boundary tests included
Determinism preserved
```

Before merging web code, verify:

```text
No physics duplicated
Resource limits applied
Cancellation supported
Structured errors
No secrets committed
Cross-platform build succeeds
```

---

# 68. Definition of Ready

A coding task is ready when it contains:

```text
Scope
Relevant specification links
Required public API
Inputs and outputs
Acceptance criteria
Required tests
Non-goals
Dependencies
```

If these are missing, the implementer shall state reasonable assumptions before coding.

---

# 69. Definition of Done

A coding task is done when:

1. Implementation is complete for stated scope.
2. Build succeeds.
3. Tests pass.
4. New behavior is tested.
5. Documentation is updated.
6. No new warning is introduced.
7. Units and validity ranges are explicit.
8. Conservation behavior is verified.
9. Cross-platform requirements are preserved.
10. Assumptions and limitations are reported.

---

# 70. Final Coding Principle

ThermoCore code shall make invalid physical states difficult to represent and impossible to hide.

The implementation shall favor:

```text
Explicit state
Explicit units
Explicit transfers
Explicit limits
Explicit residuals
Explicit assumptions
```

over convenience, hidden correction and apparent realism.

---

**End of Document**
