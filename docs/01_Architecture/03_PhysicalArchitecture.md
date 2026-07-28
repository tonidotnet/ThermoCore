# ThermoCore

## 03_PhysicalArchitecture.md

**Version:** 1.0
**Document Type:** Physical Architecture Specification
**Status:** Draft
**Applies To:** ThermoCore framework and ThermoCore.AWG implementation

---

# 1. Purpose

This document defines the physical architecture of the ThermoCore simulation framework.

ThermoCore is a general-purpose thermodynamic and mass-transfer simulation framework. It shall not contain Atmospheric Water Generator-specific assumptions in its core simulation engine.

The Atmospheric Water Generator shall be implemented as a separate module named:

```text
ThermoCore.AWG
```

The framework shall represent physical systems as directed graphs consisting of reusable physical components connected through explicitly typed ports.

The architecture shall support:

* Heat transfer
* Mass transfer
* Fluid flow
* Moist air
* Water vapor
* Liquid water
* Electrical energy
* Solar radiation
* Thermal storage
* Recirculation
* Parallel branches
* Feedback loops
* Stateful components
* Time-dependent simulation

---

# 2. Architectural Goals

The architecture shall satisfy the following goals.

## 2.1 Physical transparency

Every transfer of energy or mass shall occur through an explicitly defined port and connection.

Components shall not modify the internal state of unrelated components directly.

## 2.2 Reusability

A physical component shall be reusable in multiple simulations.

Examples:

* Solar collector
* Fan
* Heat exchanger
* Peltier module
* Condenser
* Battery
* Water tank
* Air duct

## 2.3 Extensibility

New components shall be addable without modifying the simulation engine.

## 2.4 Topology independence

The simulation engine shall not assume a fixed component order.

The following configurations shall all be supported:

```text
A → B → C
```

```text
A → B
  ↘ C
```

```text
A → B → C
    ↑   ↓
    └───┘
```

```text
A → B → D
    └→ C ┘
```

## 2.5 Conservation

Every component shall participate in one or more conservation balances:

* Energy balance
* Dry-air mass balance
* Water mass balance
* Electrical energy balance
* Total fluid mass balance

## 2.6 Determinism

Identical initial conditions, configuration, weather input and timestep sequence shall produce identical results.

---

# 3. Solution Architecture

The recommended initial solution structure is:

```text
ThermoCore.sln

src/
├── ThermoCore.Core
├── ThermoCore.Console
├── ThermoCore.UI
└── ThermoCore.AWG

tests/
├── ThermoCore.Core.Tests
└── ThermoCore.AWG.Tests

docs/
├── Requirements
├── Engineering
├── Mathematics
└── Modules
    └── AWG
```

---

# 4. Project Responsibilities

## 4.1 ThermoCore.Core

ThermoCore.Core shall contain:

* Simulation engine
* Directed simulation graph
* Component abstractions
* Port abstractions
* Connection abstractions
* Physical state definitions
* Conservation-balance infrastructure
* Numerical integration infrastructure
* Validation
* Diagnostics
* Simulation results
* Generic physical constants
* Generic psychrometric calculations

ThermoCore.Core shall not contain:

* Silica gel-specific behavior
* AWG operating modes
* AWG-specific configuration
* AWG-specific user interface
* Fixed component sequences
* Fixed device geometry

## 4.2 ThermoCore.AWG

ThermoCore.AWG shall contain:

* Atmospheric Water Generator topology
* Silica gel component
* AWG condenser component
* AWG operating controller
* Adsorption cycle
* Regeneration cycle
* Water-production calculations
* AWG-specific validation
* AWG-specific configuration
* AWG-specific result summaries

## 4.3 ThermoCore.Console

ThermoCore.Console shall:

* Load a simulation configuration
* Build a simulation graph
* Run a simulation
* Print summary results
* Export detailed results
* Support development and regression testing

## 4.4 ThermoCore.UI

ThermoCore.UI may initially be implemented using WPF.

It shall:

* Edit simulation parameters
* Load and save configurations
* Start and cancel simulations
* Display graphs
* Display conservation errors
* Display component states
* Display water and energy production summaries

The UI shall not contain physical calculations.

---

# 5. Core Physical Model

ThermoCore shall model a physical system as a directed graph.

```text
SimulationGraph
├── Components
├── Ports
├── Connections
└── State variables
```

Each component shall:

1. Receive physical states through input ports.
2. Read environmental and simulation context.
3. Calculate transfer rates.
4. Update its internal state.
5. Publish output states through output ports.
6. Report energy and mass balance information.

---

# 6. Component Model

Every simulation component shall implement a common interface.

Conceptual interface:

```csharp
public interface ISimulationComponent
{
    string Id { get; }

    IReadOnlyCollection<IPhysicalPort> Ports { get; }

    void Initialize(SimulationContext context);

    ComponentStepResult Evaluate(
        ComponentStepContext context);

    void Commit(
        ComponentStepResult result);

    ComponentDiagnostics GetDiagnostics();
}
```

The exact implementation may differ, but the following separation is mandatory:

* Evaluation shall calculate a tentative result.
* Commit shall apply the accepted result.
* Components shall not mutate global simulation state during evaluation.

This separation is required for:

* Iterative solvers
* Feedback loops
* Convergence checking
* Parallel evaluation
* Rollback
* Error diagnostics

---

# 7. Port Model

A port represents a physical connection point.

Every port shall have:

* Unique identifier
* Port type
* Direction
* Supported physical domain
* Current state
* Validation rules
* Optional flow constraints

Conceptual definition:

```csharp
public interface IPhysicalPort
{
    string Id { get; }

    string ComponentId { get; }

    PortDirection Direction { get; }

    PhysicalDomain Domain { get; }
}
```

---

# 8. Port Directions

Supported directions:

```text
Input
Output
Bidirectional
```

## Input

Receives state or flow from another component.

## Output

Publishes state or flow to another component.

## Bidirectional

Supports physical interaction in either direction.

Bidirectional ports shall initially be avoided unless required by the selected solver.

The first implementation should primarily use explicit input and output ports.

---

# 9. Physical Domains

ThermoCore shall define explicit physical domains.

Initial domains:

```text
MoistAir
DryAir
WaterVapor
LiquidWater
Heat
Electricity
SolarRadiation
MechanicalFlow
ControlSignal
```

Port compatibility shall be validated before simulation starts.

Examples:

```text
MoistAirOutput → MoistAirInput
```

Valid.

```text
ElectricPowerOutput → MoistAirInput
```

Invalid.

---

# 10. Moist-Air Port

A moist-air port shall carry at least:

* Dry-air mass flow
* Water-vapor mass flow
* Temperature
* Pressure
* Humidity ratio
* Relative humidity
* Specific enthalpy
* Dew-point temperature

Recommended state:

```csharp
public sealed record MoistAirState
{
    public double TemperatureK { get; init; }

    public double PressurePa { get; init; }

    public double DryAirMassFlowKgPerSecond { get; init; }

    public double WaterVaporMassFlowKgPerSecond { get; init; }

    public double HumidityRatioKgPerKgDryAir { get; init; }

    public double RelativeHumidityFraction { get; init; }

    public double SpecificEnthalpyJPerKgDryAir { get; init; }

    public double DewPointTemperatureK { get; init; }
}
```

Derived properties shall not be independently mutable.

For example:

* Relative humidity
* Dew point
* Specific enthalpy

shall be derived from a minimal authoritative state where practical.

This avoids physically inconsistent states.

---

# 11. Liquid-Water Port

A liquid-water port shall carry:

* Mass flow
* Temperature
* Pressure where relevant
* Specific enthalpy
* Optional dissolved-material concentration

Initial implementation:

```csharp
public sealed record LiquidWaterState
{
    public double MassFlowKgPerSecond { get; init; }

    public double TemperatureK { get; init; }

    public double SpecificEnthalpyJPerKg { get; init; }
}
```

---

# 12. Heat Port

A heat port shall represent a thermal-energy transfer rate.

```csharp
public sealed record HeatFlowState
{
    public double HeatFlowW { get; init; }

    public double BoundaryTemperatureK { get; init; }
}
```

Sign convention:

```text
Positive heat flow = energy entering the receiving component.
Negative heat flow = energy leaving the receiving component.
```

The sign convention shall be used consistently throughout the framework.

---

# 13. Electrical Port

An electrical port shall initially use a simplified power-flow model.

```csharp
public sealed record ElectricalPowerState
{
    public double VoltageV { get; init; }

    public double CurrentA { get; init; }

    public double PowerW { get; init; }
}
```

The first version shall not simulate:

* Transient circuit behavior
* Switching waveforms
* Electromagnetic effects
* Detailed converter electronics

Electrical devices shall be represented using average power over a simulation timestep.

---

# 14. Solar-Radiation Port

A solar-radiation port shall carry:

* Irradiance
* Effective projected area
* Incidence-angle modifier
* Optional diffuse and direct components

```csharp
public sealed record SolarRadiationState
{
    public double GlobalIrradianceWPerSquareMeter { get; init; }

    public double DirectIrradianceWPerSquareMeter { get; init; }

    public double DiffuseIrradianceWPerSquareMeter { get; init; }

    public double IncidenceAngleRadians { get; init; }
}
```

---

# 15. Control Port

A control port shall carry non-physical control commands.

Examples:

* Fan speed request
* Peltier power request
* Valve position
* Recirculation fraction
* Operating mode

```csharp
public sealed record ControlSignalState
{
    public double NormalizedValue { get; init; }

    public bool Enabled { get; init; }

    public string? Mode { get; init; }
}
```

Control signals shall not contain physical energy or mass.

---

# 16. Connections

A connection links compatible ports.

Conceptual model:

```csharp
public sealed record PhysicalConnection
{
    public string Id { get; init; }

    public string SourceComponentId { get; init; }

    public string SourcePortId { get; init; }

    public string TargetComponentId { get; init; }

    public string TargetPortId { get; init; }
}
```

A connection shall:

* Connect one output to one or more inputs where allowed.
* Validate physical-domain compatibility.
* Validate unit compatibility.
* Transfer state without hidden conversions.
* Report disconnected mandatory ports.
* Report illegal graph cycles where unsupported.

---

# 17. Splitters and Mixers

Flow branching shall be represented using explicit components.

## 17.1 Splitter

A splitter divides one input flow into multiple output flows.

Example:

```text
             → Exhaust
Air input → Splitter
             → Recirculation
```

The splitter shall preserve:

* Dry-air mass
* Water-vapor mass
* Energy, except configured heat loss
* Species composition

The user shall define split fractions.

The sum of all split fractions shall equal 1.0 within tolerance.

## 17.2 Mixer

A mixer combines multiple input flows.

Example:

```text
Fresh air ─────┐
               ├→ Mixed air
Recirculated ──┘
```

The mixer shall calculate:

* Combined dry-air mass flow
* Combined vapor mass flow
* Mixed enthalpy
* Mixed temperature
* Mixed humidity ratio

Mixing shall satisfy energy and mass conservation.

---

# 18. Sources and Sinks

Environmental boundaries shall be represented as explicit components.

## Sources

Examples:

* Ambient air source
* Solar-radiation source
* Electrical grid source
* Weather-data source

## Sinks

Examples:

* Exhaust-air sink
* Environment heat sink
* Water-consumption sink
* Electrical load sink

No mass or energy shall disappear implicitly.

All external transfers shall terminate at a source or sink.

---

# 19. Stateful Components

Some components store energy or mass over time.

Examples:

* Battery
* Water tank
* Silica gel
* Thermal storage
* Component thermal mass

A stateful component shall expose its state separately from its input/output ports.

Examples of internal state:

```text
Stored electrical energy
Stored liquid water
Adsorbed water
Component temperature
Thermal energy
Material saturation
```

Internal state shall be updated once per committed simulation step.

---

# 20. Conservation Balances

Every applicable component shall report balance terms.

## 20.1 Energy balance

For each timestep:

```text
Energy entering
- Energy leaving
- Change in stored energy
= Energy balance residual
```

## 20.2 Dry-air mass balance

```text
Dry air entering
- Dry air leaving
- Change in stored dry air
= Dry-air residual
```

## 20.3 Water mass balance

```text
Water vapor entering
+ Liquid water entering
- Water vapor leaving
- Liquid water leaving
- Change in stored water
= Water residual
```

## 20.4 Electrical balance

```text
Electrical energy entering
- Electrical energy consumed
- Change in stored electrical energy
- Electrical losses
= Electrical residual
```

---

# 21. Balance Result Model

Conceptual model:

```csharp
public sealed record ConservationBalance
{
    public double EnergyResidualJ { get; init; }

    public double DryAirMassResidualKg { get; init; }

    public double WaterMassResidualKg { get; init; }

    public double ElectricalEnergyResidualJ { get; init; }
}
```

The simulation engine shall aggregate component residuals into a system-level residual.

---

# 22. Simulation Graph

The simulation graph shall contain:

```csharp
public sealed class SimulationGraph
{
    public IReadOnlyCollection<ISimulationComponent> Components { get; }

    public IReadOnlyCollection<PhysicalConnection> Connections { get; }
}
```

The graph shall provide:

* Component lookup
* Port lookup
* Graph validation
* Dependency analysis
* Execution-order generation
* Cycle detection
* Connected-component analysis
* Diagnostic output

---

# 23. Graph Validation

Before simulation, the graph shall be validated.

Validation shall detect:

* Duplicate component identifiers
* Duplicate port identifiers
* Missing components
* Missing ports
* Incompatible port domains
* Invalid connection directions
* Required unconnected ports
* Invalid splitter fractions
* Unsupported cycles
* Missing sources
* Missing sinks
* Invalid initial states
* Impossible physical values

Simulation shall not start when critical graph errors exist.

---

# 24. Acyclic Graph Execution

For graphs without feedback loops, the engine may use topological sorting.

Example:

```text
AmbientAir
    ↓
Fan
    ↓
SolarCollector
    ↓
SilicaGel
    ↓
Condenser
    ↓
Exhaust
```

Execution order:

```text
AmbientAir
Fan
SolarCollector
SilicaGel
Condenser
Exhaust
```

---

# 25. Cyclic Graph Execution

Recirculation creates cycles.

Example:

```text
Mixer
  ↓
Collector
  ↓
SilicaGel
  ↓
Condenser
  ↓
Splitter
  ├→ Exhaust
  └→ Mixer
```

A cyclic graph cannot be solved using a single forward pass.

The initial cyclic solver shall use fixed-point iteration.

For every timestep:

1. Initialize loop states using previous timestep values.
2. Evaluate all components in loop order.
3. Compare new port values with previous iteration.
4. Repeat until convergence or maximum iteration count.
5. Commit the converged state.
6. Raise diagnostics if convergence fails.

Convergence variables shall include:

* Temperature
* Mass flow
* Humidity ratio
* Enthalpy
* Heat flow
* Electrical power where applicable

---

# 26. Convergence Criteria

Example default tolerances:

```text
Temperature: 0.01 K
Mass flow: 1 × 10⁻⁶ kg/s
Humidity ratio: 1 × 10⁻⁷ kg/kg dry air
Heat flow: 0.1 W
Electrical power: 0.1 W
```

Exact values shall be defined in the mathematical and numerical-method documents.

---

# 27. Simulation Context

The simulation context shall provide read-only global information.

```csharp
public sealed record SimulationContext
{
    public DateTimeOffset SimulationStart { get; init; }

    public TimeSpan TimeStep { get; init; }

    public TimeSpan ElapsedTime { get; init; }

    public int StepIndex { get; init; }

    public EnvironmentState Environment { get; init; }

    public NumericalSettings NumericalSettings { get; init; }
}
```

Components shall not use system time directly.

All time-dependent behavior shall use the simulation context.

---

# 28. Component Evaluation Context

A component evaluation context shall contain:

* Current timestep
* Current input-port states
* Previous internal state
* Environmental state
* Solver iteration number
* Numerical tolerances

Components shall not resolve dependencies directly through a service locator.

All required physical inputs shall arrive through ports or explicit simulation context.

---

# 29. Component Evaluation Result

A component evaluation result shall contain:

* Proposed output-port states
* Proposed new internal state
* Conservation balance
* Warnings
* Errors
* Convergence values
* Optional diagnostic measurements

Conceptual model:

```csharp
public sealed record ComponentStepResult
{
    public IReadOnlyDictionary<string, object> OutputStates { get; init; }

    public object? ProposedInternalState { get; init; }

    public ConservationBalance Balance { get; init; }

    public IReadOnlyCollection<SimulationDiagnostic> Diagnostics { get; init; }
}
```

---

# 30. Generic Component Categories

ThermoCore shall support the following generic categories.

## 30.1 Source components

Produce boundary conditions.

Examples:

* AmbientAirSource
* SolarRadiationSource
* ElectricalSource

## 30.2 Sink components

Receive final flows.

Examples:

* ExhaustSink
* EnvironmentHeatSink
* WaterSink

## 30.3 Transport components

Move physical flows.

Examples:

* Fan
* Pump
* AirDuct
* Pipe

## 30.4 Conversion components

Convert energy between domains.

Examples:

* Solar panel
* Peltier module
* Heater
* Compressor

## 30.5 Transfer components

Transfer energy or mass between streams.

Examples:

* Heat exchanger
* Condenser
* Evaporator
* Adsorbent bed

## 30.6 Storage components

Store mass or energy.

Examples:

* Battery
* Water tank
* Silica gel
* Thermal storage

## 30.7 Control components

Modify operating decisions.

Examples:

* Mode controller
* PID controller
* Schedule controller
* Safety controller

---

# 31. ThermoCore.AWG Topology

The initial AWG implementation shall use a topology similar to:

```text
Ambient Air Source
        ↓
Inlet Filter
        ↓
Main Fan
        ↓
Peltier Hot-Side Heat Exchanger
        ↓
Solar Collector
        ↓
Silica Gel Bed
        ↓
Condensation Chamber
        ↓
Flow Splitter
      ↙   ↘
Exhaust   Recirculation
              ↓
             Mixer
              ↑
       Ambient Make-Up Air
```

Liquid-water path:

```text
Condensation Chamber
        ↓
Drain Channel
        ↓
Water Tank
```

Electrical path:

```text
Solar Radiation
      ↓
Solar Panel
      ↓
Charge Controller
      ↓
Battery
   ↙      ↘
Fan      Peltier
```

Thermal path:

```text
Peltier Cold Side
      ↓
Condensation Chamber

Peltier Hot Side
      ↓
Incoming Air
```

---

# 32. AWG Operating Modes

ThermoCore.AWG shall initially define the following operating modes.

```text
Idle
Adsorption
Regeneration
Condensation
Recirculation
Shutdown
Fault
```

## 32.1 Idle

* Fans may be off.
* Peltier is off.
* No active regeneration.
* Internal states continue to be tracked.

## 32.2 Adsorption

* Ambient air passes through silica gel.
* Silica gel adsorbs water vapor.
* Peltier is normally off.
* Air exits to the environment.

## 32.3 Regeneration

* Solar collector heats air.
* Hot air passes through silica gel.
* Adsorbed water is released.
* Moist air flows toward the condenser.

## 32.4 Condensation

* Peltier cools the condensation surface.
* Water vapor condenses.
* Liquid water drains to the tank.
* Remaining moist air may be recirculated.

## 32.5 Recirculation

* A configurable part of the condenser outlet air returns to the process inlet.
* A configurable make-up air fraction is added.
* Mass and energy conservation must remain valid.

## 32.6 Shutdown

* Active loads are disabled.
* Residual heat and flow may be simulated.

## 32.7 Fault

* Unsafe conditions cause controlled load shutdown.
* Simulation continues where possible for diagnostics.

---

# 33. Recirculation Architecture

Recirculation shall not be implemented as hidden logic inside the condenser or fan.

It shall be represented explicitly:

```text
Condenser
    ↓
Splitter
 ┌──┴─────┐
 ↓        ↓
Exhaust  Recirculation Duct
             ↓
          Recirculation Fan
             ↓
            Mixer
             ↑
        Fresh Air Source
```

This enables calculation of:

* Water vapor lost in exhaust
* Heat lost in exhaust
* Recovered sensible heat
* Recovered latent potential
* Fresh-air requirement
* Recirculation ratio
* Accumulation of non-condensable air
* Loop convergence

---

# 34. Heat Recovery Architecture

Heat recovery may use two alternative configurations.

## 34.1 Direct recirculation

Part of the outlet air is returned directly.

Advantages:

* Recovers sensible heat
* Recovers uncondensed water vapor
* Simple topology

Risks:

* Increasing loop temperature
* Reduced condenser performance
* Accumulation of contaminants
* Solver feedback loop

## 34.2 Indirect heat exchanger

Outlet air transfers heat to inlet air without mixing.

Advantages:

* No moisture or contaminant mixing
* Controlled heat recovery
* Easier mass balance

Disadvantages:

* Does not recover remaining water vapor
* Causes pressure loss
* Requires additional exchanger area

Both configurations shall be representable by the same ThermoCore graph engine.

---

# 35. Peltier Component Architecture

The Peltier module shall be represented as a multi-domain component.

Ports:

```text
ElectricalPowerIn
ColdSideHeatIn
HotSideHeatOut
ControlIn
```

The Peltier component shall not directly model the condenser or hot-side air heat exchanger.

Those shall be separate components.

Example:

```text
Condensation Chamber
        ↓ Heat
Peltier Cold Side
        ↓
Peltier Module
        ↓ Heat
Hot-Side Heat Exchanger
        ↓
Incoming Air
```

This separation allows:

* Different heat sinks
* Multiple Peltier modules
* Alternative condensers
* Independent thermal-resistance models
* Realistic hot-side temperature calculation

---

# 36. Silica Gel Component Architecture

The silica gel bed shall be a stateful mass- and heat-transfer component.

Ports:

```text
MoistAirIn
MoistAirOut
HeatLossOut
OptionalControlIn
```

Internal state:

```text
Dry silica mass
Adsorbed water mass
Bed temperature
Water-loading fraction
Maximum equilibrium loading
Regeneration state
```

The component shall conserve:

* Dry air
* Water
* Energy

The component shall not generate or destroy water.

---

# 37. Condenser Component Architecture

The condenser shall have:

```text
MoistAirIn
MoistAirOut
LiquidWaterOut
HeatOut
```

The condenser shall calculate:

* Sensible cooling
* Condensation onset
* Condensed-water mass
* Latent heat release
* Remaining vapor content
* Outlet air state
* Drain-water state

The condenser shall not assume that all excess moisture condenses.

Condensation shall be limited by:

* Surface temperature
* Heat-transfer capacity
* Mass-transfer capacity
* Residence time
* Available cooling power
* Drainage efficiency

---

# 38. Water Tank Architecture

The water tank shall be a stateful liquid-water storage component.

Ports:

```text
LiquidWaterIn
OptionalLiquidWaterOut
HeatExchange
```

Internal state:

```text
Stored water mass
Water temperature
Maximum capacity
Overflow state
```

The water tank shall be physically isolated from electrical components in the AWG hardware design, but the software model shall primarily represent mass and thermal behavior.

---

# 39. Battery Architecture

The battery shall be a stateful electrical storage component.

Ports:

```text
ElectricalPowerIn
ElectricalPowerOut
HeatLossOut
```

Internal state:

```text
Stored energy
State of charge
Temperature
Charge limit
Discharge limit
Efficiency
```

The first model may use a simplified energy balance.

---

# 40. Control Architecture

The AWG controller shall operate above the physical components.

It shall read:

* Outdoor conditions
* Solar power
* Battery SOC
* Silica gel loading
* Collector temperature
* Condenser temperature
* Dew point
* Water tank level
* System faults

It shall command:

* Main fan speed
* Recirculation fan speed
* Peltier power
* Recirculation ratio
* Operating mode

The controller shall not directly alter physical component states.

It shall communicate using control ports.

---

# 41. Diagnostics

Every component shall support diagnostics.

Diagnostic levels:

```text
Information
Warning
Error
Critical
```

Examples:

* Relative humidity outside valid range
* Negative mass flow
* Condenser surface above dew point
* Peltier hot side overheating
* Battery depleted
* Silica gel capacity exceeded
* Water mass imbalance
* Solver convergence failure
* Unconnected required port

---

# 42. Calibration Points

ThermoCore.AWG shall expose virtual measurement points corresponding to future sensors.

Recommended points:

```text
Ambient-air temperature
Ambient relative humidity
Collector inlet temperature
Collector outlet temperature
Silica-gel inlet temperature
Silica-gel outlet temperature
Silica-gel outlet humidity
Condenser inlet temperature
Condenser inlet humidity
Condenser outlet temperature
Condenser outlet humidity
Cold-side temperature
Hot-side temperature
Main airflow
Recirculation airflow
Battery voltage
Battery current
Solar-panel power
Peltier power
Collected-water mass
```

Simulation results shall use stable identifiers for these points.

---

# 43. Physical Assumptions

Initial model assumptions:

* Air behaves as an ideal-gas mixture.
* Atmospheric pressure is uniform through the device except for calculated pressure drops.
* Air states are spatially uniform at each port.
* Each component is represented by a lumped-parameter model.
* Component temperatures are uniform unless explicitly divided into thermal nodes.
* Kinetic and potential energy changes of air are negligible.
* Dry air is conserved.
* Water is conserved across vapor, adsorbed and liquid phases.
* Solar radiation is externally provided.
* Air leakage is initially zero.
* Liquid-water re-evaporation may initially be neglected.
* Condensed water leaves the air stream without delay.
* Frost formation is outside the first implementation scope.

---

# 44. Architectural Limitations

The first implementation shall not provide:

* Computational fluid dynamics
* Finite-element thermal analysis
* Detailed surface wetting
* Droplet dynamics
* Distributed silica-gel diffusion
* Detailed electrical circuit simulation
* Electromagnetic models
* Structural mechanics
* Detailed radiation view-factor analysis
* Multiphase pressure-wave simulation

The architecture shall allow later replacement of lumped components with more detailed models.

---

# 45. Implementation Requirements

The implementation shall:

* Use SI units internally.
* Use `double` for physical calculations.
* Enable nullable reference types.
* Avoid static mutable state.
* Avoid direct component-to-component references for physical state transfer.
* Use immutable state records where practical.
* Separate evaluation from commit.
* Validate graph topology before execution.
* Record conservation residuals.
* Support deterministic execution.
* Support cancellation from UI.
* Support component-level unit testing.
* Support graph-level integration testing.

---

# 46. Acceptance Criteria

The architecture is accepted when:

1. A linear AWG topology can be built without engine-specific AWG code.
2. A recirculation loop can be represented explicitly.
3. A heat exchanger can be inserted without modifying existing components.
4. A second Peltier module can be added by graph configuration.
5. All physical transfers occur through typed ports.
6. Invalid port connections are rejected before simulation.
7. System energy and water balances are available after every timestep.
8. Core components contain no UI dependency.
9. ThermoCore.Core contains no AWG-specific operating logic.
10. Identical inputs produce identical results.

---

# 47. Initial Implementation Priority

The first implementation shall prioritize simplicity.

Recommended sequence:

```text
1. Physical state records
2. Port definitions
3. Component abstraction
4. Connection model
5. Acyclic graph validation
6. Acyclic execution
7. Conservation balances
8. Basic AWG components
9. Splitter and mixer
10. Fixed-point cyclic solver
11. Recirculation
12. UI integration
```

A general nonlinear equation solver is not required in the first version.

---

# 48. Relationship to Other Documents

This document defines the physical architecture.

Detailed formulas shall be defined in:

```text
04_MathematicalModel.md
```

Psychrometric formulas shall be defined in:

```text
05_Psychrometrics.md
```

Numerical solution methods shall be defined in:

```text
25_NumericalMethods.md
```

Units and conversions shall be defined in:

```text
27_Units.md
```

AWG-specific component behavior shall be defined under:

```text
Modules/AWG/
```

Coding conventions shall be defined in:

```text
18_CodingRules.md
```

---

# 49. Final Architectural Principle

ThermoCore shall treat a thermodynamic system as a graph of physically isolated components.

Each component shall communicate only through explicit ports carrying mass, energy, fluid or control state.

The simulation engine shall solve the graph without knowing the application-specific purpose of the components.

ThermoCore.AWG shall be one concrete system assembled from those generic capabilities.

---

**End of Document**
