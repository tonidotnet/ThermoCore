# ThermoCore
## 09_SilicaGel.md

**Version:** 1.0  
**Document Type:** Adsorbent Engineering and Mathematical Specification  
**Status:** Draft  
**Applies To:** ThermoCore.Core, ThermoCore.AWG and future adsorption modules  
**Primary implementation language:** C#  
**Internal unit system:** SI

---

# 1. Purpose

This document defines the mathematical and software model of a silica-gel water-vapor adsorption bed used by ThermoCore.

The component shall model:

- Water adsorption from moist air
- Water desorption during thermal regeneration
- Equilibrium water loading
- Adsorption and desorption kinetics
- Adsorbent thermal mass
- Heat of adsorption
- Sensible heating and cooling of the bed
- Water-vapor and dry-air mass balances
- Moist-air outlet state
- Bed saturation
- Regeneration progress
- Pressure drop
- Airflow limitations
- Dynamic operation over time
- Model calibration
- Conservation residuals
- Invalid-state and operating diagnostics

The component shall not assign an arbitrary outlet relative humidity. Outlet humidity shall be derived from the water mass balance, energy balance, pressure and outlet temperature.

---

# 2. Scope

The initial implementation targets a fixed bed containing bead or granular silica gel.

The model shall support:

- Adsorption from ambient air
- Desorption using heated regeneration air
- Cyclic adsorption and regeneration
- Variable bed temperature
- Variable air temperature and humidity
- Variable airflow
- Configurable equilibrium-isotherm models
- Configurable kinetic models
- Configurable heat of adsorption
- Hysteresis support at higher fidelity
- Bed pressure drop
- Water-loading limits
- Prototype calibration

The first implementation shall not require:

- Detailed pore-network simulation
- Molecular-dynamics simulation
- Distributed particle diffusion
- Full three-dimensional bed temperature field
- Detailed bead fracture or attrition
- Chemical aging
- Contaminant adsorption
- Liquid-water flooding
- Calcium-chloride deliquescence
- Microbiological modelling

---

# 3. Architectural Placement

The generic adsorption infrastructure should be implemented in:

```text
ThermoCore.Core
```

Recommended namespace:

```csharp
ThermoCore.Core.Components.Adsorption
```

Silica-gel-specific equilibrium, kinetic and thermal models may initially be implemented in:

```text
ThermoCore.AWG
```

Recommended namespace:

```csharp
ThermoCore.AWG.Components.SilicaGel
```

The separation should allow future replacement by:

- Zeolite
- Activated alumina
- Metal-organic frameworks
- Composite silica gel
- Salt-impregnated porous media
- Other solid desiccants

---

# 4. Component Classification

The silica-gel bed is:

```text
Stateful water-storage component
Moist-air mass-transfer component
Thermal-storage component
Heat-release and heat-consumption component
Pressure-loss component
```

During adsorption:

```text
Moist air
    ↓
Water vapor transferred to silica gel
    ↓
Drier outlet air
    +
Adsorption heat released
```

During desorption:

```text
Hot regeneration air
    ↓
Water released from silica gel
    ↓
More humid outlet air
    +
Heat consumed
```

---

# 5. Ports

Recommended ports:

```text
MoistAirIn
MoistAirOut
AmbientHeatOut
OptionalControlIn
```

Optional ports:

```text
ExternalHeatIn
LiquidWaterOut
BedTemperatureMeasurement
BedLoadingMeasurement
PressureDropMeasurement
```

`LiquidWaterOut` is normally unused for plain silica gel and shall remain zero unless an explicit liquid carryover model is enabled.

---

# 6. Internal State

Recommended state:

```csharp
public sealed record SilicaGelState
{
    public required double DryAdsorbentMassKg { get; init; }

    public required double AdsorbedWaterMassKg { get; init; }

    public required double WaterLoadingKgPerKgDryAdsorbent { get; init; }

    public required double BedTemperatureK { get; init; }

    public required double StoredThermalEnergyJ { get; init; }

    public required double EquilibriumLoadingKgPerKgDryAdsorbent { get; init; }

    public required double LoadingFraction { get; init; }

    public required double LastWaterTransferRateKgPerSecond { get; init; }

    public required double LastAdsorptionHeatW { get; init; }

    public required SilicaGelOperatingRegime OperatingRegime { get; init; }

    public required bool HasReachedEquilibrium { get; init; }
}
```

Recommended operating regimes:

```csharp
public enum SilicaGelOperatingRegime
{
    Idle,
    Adsorption,
    Desorption,
    NearEquilibrium,
    Saturated,
    Regenerated,
    Invalid
}
```

---

# 7. Configuration Model

Recommended configuration:

```csharp
public sealed record SilicaGelParameters
{
    public required double DryAdsorbentMassKg { get; init; }

    public required double MaximumWaterLoadingKgPerKgDryAdsorbent { get; init; }

    public required double MinimumRegeneratedLoadingKgPerKgDryAdsorbent { get; init; }

    public required double EffectiveSpecificHeatJPerKgK { get; init; }

    public required double BedHousingThermalCapacityJPerK { get; init; }

    public required double EffectiveHeatOfAdsorptionJPerKgWater { get; init; }

    public required double BedHeatLossCoefficientWPerK { get; init; }

    public required double ReferenceMassTransferCoefficientPerSecond { get; init; }

    public required double ActivationEnergyJPerMol { get; init; }

    public required double ReferenceKineticTemperatureK { get; init; }

    public required double ReferencePressureDropPa { get; init; }

    public required double ReferenceVolumetricFlowM3PerSecond { get; init; }

    public required double BedVoidFraction { get; init; }

    public required double BedCrossSectionAreaM2 { get; init; }

    public required double BedLengthM { get; init; }

    public required double ParticleDiameterM { get; init; }

    public required SilicaGelIsothermModelType IsothermModelType { get; init; }

    public required SilicaGelKineticModelType KineticModelType { get; init; }

    public required IReadOnlyDictionary<string, double> IsothermParameters { get; init; }

    public required IReadOnlyDictionary<string, double> KineticParameters { get; init; }

    public bool EnableAdsorptionDesorptionHysteresis { get; init; }

    public bool EnableTemperatureDependentHeatOfAdsorption { get; init; }

    public bool EnableDetailedPressureDrop { get; init; }
}
```

---

# 8. Required Inputs

The component shall receive or derive:

```text
Inlet moist-air state
Current silica-gel state
Ambient temperature
Simulation timestep
Optional operating-mode request
Optional external heat input
Current pressure
Airflow
Selected isotherm and kinetic model
```

The component shall calculate its physical regime from state and driving force.

A controller may request adsorption or regeneration operation, but the component shall not transfer water against the selected physical equilibrium without an explicit powered process model.

---

# 9. Water Loading

Water loading is:

\[
X
=
\frac{
m_{water,adsorbed}
}{
m_{adsorbent,dry}
}
\]

Unit:

```text
kg water / kg dry silica gel
```

Therefore:

\[
m_{water,adsorbed}
=
Xm_{adsorbent,dry}
\]

Bounds:

\[
0
\leq
X
\leq
X_{max}
\]

A configured regenerated minimum may be:

\[
X_{min,regen}>0
\]

because practical regeneration normally does not remove all adsorbed water.

---

# 10. Loading Fraction

A normalized reporting value:

\[
S_X
=
\frac{
X-X_{min}
}{
X_{max}-X_{min}
}
\]

with a physical reporting range:

\[
0\leq S_X\leq1
\]

This loading fraction is not an equilibrium equation and shall not replace the actual loading state.

---

# 11. Water Activity and Relative Pressure

For water-vapor adsorption:

\[
a_w
=
\frac{p_v}{p_{ws}(T_{bed})}
\]

This is also commonly called relative pressure:

\[
r_p
=
\frac{p_v}{p_{ws}(T_{bed})}
\]

For equilibrium gas states:

\[
0\leq r_p\leq1
\]

The equilibrium loading generally depends on:

\[
X_{eq}=f(T_{bed},r_p)
\]

The relative humidity of the bulk air at its own temperature is not necessarily identical to the relative pressure evaluated at the bed temperature.

---

# 12. Equilibrium Loading

The equilibrium loading is the water loading toward which the adsorbent approaches for a given:

```text
Bed temperature
Water-vapor partial pressure
Adsorbent type
Adsorption or desorption branch
```

General form:

\[
X_{eq}
=
f_{iso}(T_{bed},p_v,\mathbf{p}_{iso})
\]

where:

- \(\mathbf{p}_{iso}\) is the isotherm parameter set

The implementation shall expose the isotherm behind an interface.

---

# 13. Isotherm Interface

Recommended interface:

```csharp
public interface ISilicaGelIsotherm
{
    double CalculateEquilibriumLoadingKgPerKg(
        double bedTemperatureK,
        double vaporPressurePa,
        double saturationPressurePa,
        SilicaGelIsothermContext context);

    SilicaGelIsothermMetadata Metadata { get; }
}
```

Metadata shall include:

```text
Model name
Reference
Parameter source
Temperature range
Relative-pressure range
Adsorbent type
Adsorption or desorption branch
Expected error
```

---

# 14. Initial Isotherm Strategy

The first implementation shall not embed one universal silica-gel isotherm as if all commercial gels behaved identically.

Recommended approach:

1. Provide a simple generic engineering isotherm.
2. Mark its parameters as provisional.
3. Allow manufacturer- or experiment-specific parameter sets.
4. Store the selected model and parameter version with simulation results.
5. Support future replacement without changing the component interface.

---

# 15. Generic Polynomial Isotherm

A low-fidelity normalized isotherm may use:

\[
X_{eq}
=
X_{max}
\sum_{i=1}^{n}
a_i r_p^i
\]

with:

\[
0\leq r_p\leq1
\]

The result shall be constrained by explicit physical capacity:

\[
0\leq X_{eq}\leq X_{max}
\]

This model is suitable only for fitted data over a documented range.

It is not a universal physical law.

---

# 16. Freundlich-Type Isotherm

An empirical model may use:

\[
X_{eq}
=
K_F(T)
r_p^{n(T)}
\]

This is simple but may not represent saturation correctly over the entire range.

It shall only be used over a calibrated operating interval.

---

# 17. Langmuir-Type Isotherm

A Langmuir-type model:

\[
X_{eq}
=
X_m
\frac{
b(T)p_v
}{
1+b(T)p_v
}
\]

This model represents a finite monolayer capacity but may not describe capillary condensation in mesoporous silica gel accurately.

It may be used as a low-fidelity option.

---

# 18. Toth or Sips-Type Isotherm

A more flexible empirical model may use:

\[
X_{eq}
=
X_m
\frac{
(b p_v)^n
}{
1+(b p_v)^n
}
\]

or a Toth-type variation.

All parameter definitions and validity ranges shall be explicit.

---

# 19. Dubinin-Type Models

Higher-fidelity adsorption models may use adsorption-potential formulations:

\[
A
=
RT
\ln
\left(
\frac{p_{ws}}{p_v}
\right)
\]

with loading represented as a function of adsorption potential.

These models may be appropriate for calibrated silica-gel/water data but shall not be implemented without a specific reference and parameter set.

---

# 20. Hysteresis

Silica-gel water adsorption may exhibit different adsorption and desorption paths.

If hysteresis is enabled:

\[
X_{eq,ads}
\neq
X_{eq,des}
\]

The branch may be selected using:

- Sign of recent loading change
- Operating mode
- State-machine history
- Scanning-curve model at higher fidelity

The first implementation may use separate adsorption and desorption parameter sets.

---

# 21. Mass-Transfer Driving Force

The equilibrium driving force is:

\[
\Delta X
=
X_{eq}-X
\]

Interpretation:

```text
ΔX > 0: adsorption is thermodynamically favored
ΔX < 0: desorption is thermodynamically favored
ΔX ≈ 0: bed is near equilibrium
```

The controller's requested mode shall not override the sign of the physical driving force without an explicit additional mechanism.

---

# 22. Linear Driving Force Model

The recommended initial kinetic model is:

\[
\frac{dX}{dt}
=
k_{LDF}
\left(
X_{eq}-X
\right)
\]

where:

- \(k_{LDF}\) is an effective mass-transfer coefficient in 1/s

Water-transfer rate:

\[
\dot m_{water,ads}
=
m_{adsorbent,dry}
\frac{dX}{dt}
\]

Sign convention:

```text
Positive transfer rate = water moves from air to silica gel
Negative transfer rate = water moves from silica gel to air
```

The LDF model is computationally efficient and is widely used as an approximation for adsorption kinetics, but its coefficient must be calibrated for adsorbent, particle size, temperature, airflow and bed configuration.

---

# 23. Temperature-Dependent Kinetic Coefficient

An Arrhenius-type relation may be used:

\[
k(T)
=
k_{ref}
\exp
\left[
-\frac{E_a}{R}
\left(
\frac{1}{T}
-
\frac{1}{T_{ref}}
\right)
\right]
\]

where:

- \(E_a\) is effective activation energy
- \(R\) is universal gas constant
- \(T\) is bed temperature
- \(T_{ref}\) is reference temperature

The sign and parameter interpretation shall be validated against calibration data.

---

# 24. Flow-Dependent Kinetic Correction

Bed kinetics may also depend on external film transfer.

A simplified correction:

\[
k_{effective}
=
k_{internal}
f_{flow}
\]

Example:

\[
f_{flow}
=
\left(
\frac{\dot m_{da}}
{\dot m_{da,ref}}
\right)^n
\]

within configured bounds.

This is empirical and shall be calibrated.

---

# 25. Combined Film and Particle Resistance

A resistance-in-series approximation:

\[
\frac{1}{k_{effective}}
=
\frac{1}{k_{film}}
+
\frac{1}{k_{particle}}
\]

This may be used at higher fidelity when separate coefficients are available.

---

# 26. Explicit LDF Update

Explicit Euler:

\[
X_{n+1}
=
X_n
+
k_{LDF}
(X_{eq,n}-X_n)
\Delta t
\]

The result shall be limited by:

- Available inlet water vapor
- Stored water during desorption
- Maximum loading
- Minimum loading
- Energy availability
- Timestep stability

---

# 27. Exact LDF Timestep Update

For constant \(X_{eq}\) and \(k\) over the timestep, the exact update is:

\[
X_{n+1}
=
X_{eq}
+
(X_n-X_{eq})
e^{-k\Delta t}
\]

This update is preferred over explicit Euler for numerical stability.

Transferred loading:

\[
\Delta X
=
X_{n+1}-X_n
\]

Transferred water mass:

\[
\Delta m_{water}
=
m_{adsorbent,dry}\Delta X
\]

---

# 28. Adsorption Water Availability Limit

Maximum water that can be removed from the air during a timestep:

\[
\Delta m_{v,available}
=
\dot m_{v,in}\Delta t
+
m_{v,stored,in\ component}
\]

Normally, component gas storage is neglected:

\[
m_{v,stored,in\ component}=0
\]

Therefore:

\[
\Delta m_{ads}
\leq
\dot m_{v,in}\Delta t
\]

The outlet vapor mass shall never become negative.

---

# 29. Desorption Storage Limit

During desorption:

\[
-\Delta m_{ads}
\leq
m_{water,adsorbed}
-
m_{water,min}
\]

The bed shall never release more water than it stores.

---

# 30. Capacity Limit

After transfer:

\[
X_{min}
\leq
X_{n+1}
\leq
X_{max}
\]

When a kinetic update exceeds capacity, the transfer shall be reduced and the limiting event reported.

---

# 31. Dry-Air Balance

Silica gel does not consume dry air.

Without leakage:

\[
\dot m_{da,out}
=
\dot m_{da,in}
\]

Any configured leakage shall use an explicit leak stream.

---

# 32. Water-Vapor Balance

For one timestep:

\[
\Delta m_{ads}
=
m_{water,ads,n+1}
-
m_{water,ads,n}
\]

Air-vapor balance:

\[
m_{v,out}
=
m_{v,in}
-
\Delta m_{ads}
\]

Using flow rates:

\[
\dot m_{v,out}
=
\dot m_{v,in}
-
\frac{\Delta m_{ads}}{\Delta t}
\]

During desorption, \(\Delta m_{ads}<0\), so outlet vapor flow increases.

---

# 33. Outlet Humidity Ratio

\[
W_{out}
=
\frac{
\dot m_{v,out}
}{
\dot m_{da,out}
}
\]

Relative humidity and dew point shall then be derived from:

```text
Outlet temperature
Outlet pressure
Outlet humidity ratio
```

The component shall never directly set outlet RH to a target such as 95%.

---

# 34. Energy Balance

The silica-gel bed energy balance shall include:

```text
Inlet moist-air enthalpy
Outlet moist-air enthalpy
Adsorbent sensible energy
Adsorbed-water sensible energy
Housing thermal mass
Heat of adsorption/desorption
Environmental heat loss
External heat input if configured
```

General balance:

\[
\frac{dE_{bed}}{dt}
=
\dot H_{air,in}
-
\dot H_{air,out}
+
Q_{external}
-
Q_{loss}
+
Q_{adsorption}
\]

The sign of adsorption heat shall be defined consistently.

---

# 35. Heat of Adsorption

Let \(h_{ads}>0\) be the magnitude of heat released per kilogram of water adsorbed.

Then:

\[
Q_{adsorption}
=
h_{ads}
\dot m_{water,ads}
\]

With the selected transfer sign:

```text
Positive water transfer = adsorption
Negative water transfer = desorption
```

Therefore:

- Adsorption produces positive heat release.
- Desorption produces negative heat contribution, representing required heat input.

---

# 36. Effective Heat of Adsorption

The heat of adsorption may be greater than the latent heat of vaporization because of adsorbate–surface interactions.

Published and experimental values vary with:

- Silica-gel type
- Water loading
- Temperature
- Pore structure
- Measurement method

The initial implementation shall use a configurable effective value, not a universal hard-coded constant.

The parameter source classification shall be stored as:

```text
Engineering estimate
Published experiment
Manufacturer data
Prototype calibration
```

---

# 37. Temperature-Dependent Heat of Adsorption

At higher fidelity:

\[
h_{ads}
=
h_{ads}(X,T)
\]

Possible representations:

- Constant value
- Piecewise loading-dependent value
- Polynomial fit
- Table interpolation
- Isosteric-heat model

The selected model shall remain replaceable.

---

# 38. Bed Thermal Capacity

Approximate effective heat capacity:

\[
C_{bed}
=
m_{dry}c_{p,silica}
+
m_{ads}c_{p,water,effective}
+
C_{housing}
\]

Unit:

```text
J/K
```

The water contribution may differ from bulk-liquid-water heat capacity and shall be calibrated at higher fidelity.

---

# 39. Bed Temperature Update

Lumped update:

\[
C_{bed}
\frac{dT_{bed}}{dt}
=
\dot H_{air,in}
-
\dot H_{air,out}
+
Q_{external}
-
Q_{loss}
+
Q_{adsorption}
\]

Because outlet enthalpy depends on bed temperature and water transfer, the model is coupled and may require iteration.

---

# 40. Environmental Heat Loss

\[
Q_{loss}
=
UA_{bed}
(T_{bed}-T_{ambient})
\]

where:

\[
UA_{bed}
=
\text{BedHeatLossCoefficientWPerK}
\]

Negative \(Q_{loss}\) means the environment heats a colder bed if signed using the component input convention.

The implementation shall use one documented sign convention consistently.

---

# 41. Air-to-Bed Sensible Heat Transfer

A heat-exchanger effectiveness model may use:

\[
\varepsilon_{air-bed}
=
1-
\exp
\left(
-\frac{UA_{air-bed}}{C_{air}}
\right)
\]

Then:

\[
Q_{sens}
=
\varepsilon_{air-bed}
C_{air}
(T_{air,in}-T_{bed})
\]

This term influences both outlet-air and bed temperature.

The initial model may combine air-to-bed heat transfer into the enthalpy balance.

---

# 42. Outlet-Air Temperature

A simple quasi-steady approach:

\[
T_{out}
=
T_{in}
-
\frac{
Q_{to,bed}
}{
C_{air}
}
\]

A more consistent implementation shall solve outlet enthalpy from:

\[
\dot m_{da}h_{out}
=
\dot m_{da}h_{in}
-
Q_{air\rightarrow bed}
+
Q_{water,phase}
\]

while preserving the selected moist-air enthalpy reference.

---

# 43. Adsorption Regime

Adsorption is favored when:

\[
X<X_{eq}
\]

Typical behavior:

- Water leaves the air.
- Bed loading increases.
- Adsorption heat is released.
- Bed temperature may rise.
- Outlet humidity ratio falls.
- Adsorption rate decreases near equilibrium.

---

# 44. Desorption Regime

Desorption is favored when:

\[
X>X_{eq}
\]

Typical behavior:

- Water enters the air.
- Bed loading decreases.
- Heat is consumed.
- Outlet humidity ratio rises.
- Bed temperature may fall if heat input is insufficient.
- Desorption rate decreases near equilibrium.

---

# 45. Regeneration Requirement

Heating alone does not guarantee useful desorption.

The equilibrium loading must decrease sufficiently:

\[
X_{eq}(T_{hot},p_v)
<
X
\]

The model shall therefore calculate equilibrium from actual bed temperature and vapor pressure.

A requested regeneration mode shall not automatically force water release.

---

# 46. Regeneration Airflow Trade-Off

Increasing airflow may:

- Increase external mass transfer
- Supply more sensible heat
- Carry away more released vapor
- Reduce outlet vapor concentration
- Increase fan power
- Increase pressure drop

Therefore, maximum desorption rate and maximum outlet dew point may occur at different airflow values.

System optimization belongs outside the component.

---

# 47. Pressure Drop

The first implementation may use:

\[
\Delta p
=
\Delta p_{ref}
\left(
\frac{\dot V}{\dot V_{ref}}
\right)^2
\]

A higher-fidelity packed-bed model may use the Ergun equation.

---

# 48. Ergun Equation

For a packed bed:

\[
\frac{\Delta p}{L}
=
\frac{
150\mu(1-\varepsilon_b)^2
}{
\varepsilon_b^3d_p^2
}
v_s
+
\frac{
1.75\rho(1-\varepsilon_b)
}{
\varepsilon_b^3d_p
}
v_s^2
\]

where:

- \(\varepsilon_b\) is bed void fraction
- \(d_p\) is particle diameter
- \(v_s\) is superficial velocity
- \(\mu\) is dynamic viscosity
- \(\rho\) is moist-air density

This model shall be used only when geometry and flow properties are available.

---

# 49. Superficial Velocity

\[
v_s
=
\frac{
\dot V
}{
A_{bed}
}
\]

The actual interstitial velocity is approximately:

\[
v_i
=
\frac{v_s}{\varepsilon_b}
\]

---

# 50. Residence Time

Approximate gas residence time:

\[
t_{res}
=
\frac{
\varepsilon_bV_{bed}
}{
\dot V
}
\]

where:

\[
V_{bed}=A_{bed}L_{bed}
\]

Residence time may be used for diagnostics and empirical kinetic corrections.

---

# 51. Dynamic Solver Sequence

Recommended evaluation sequence:

```text
1. Read inlet moist-air state.
2. Read current bed state.
3. Calculate inlet vapor pressure.
4. Calculate saturation pressure at bed temperature.
5. Calculate relative pressure at bed temperature.
6. Calculate equilibrium loading.
7. Calculate kinetic coefficient.
8. Calculate unconstrained water transfer.
9. Apply air-vapor availability limit.
10. Apply stored-water and capacity limits.
11. Calculate adsorption/desorption heat.
12. Solve coupled bed and outlet-air temperatures.
13. Calculate outlet vapor flow and humidity ratio.
14. Create psychrometrically consistent outlet state.
15. Calculate pressure drop.
16. Calculate water, dry-air and energy residuals.
17. Return proposed state and diagnostics.
```

---

# 52. Coupled Iteration

The following values are mutually dependent:

```text
Bed temperature
Equilibrium loading
Water-transfer rate
Adsorption heat
Outlet-air temperature
Outlet-air vapor pressure
```

Recommended fixed-point iteration:

```text
1. Initialize bed temperature from current state.
2. Calculate equilibrium loading.
3. Calculate water transfer.
4. Calculate adsorption heat.
5. Calculate outlet air and bed temperature.
6. Recalculate equilibrium loading.
7. Apply relaxation.
8. Test convergence.
9. Repeat.
```

---

# 53. Convergence Variables

Check:

```text
Bed temperature
Outlet temperature
Equilibrium loading
Water-transfer rate
Outlet humidity ratio
Adsorption heat
```

Suggested initial tolerances:

| Quantity | Absolute tolerance |
|---|---:|
| Temperature | 0.01 K |
| Loading | \(10^{-7}\) kg/kg |
| Water-transfer rate | \(10^{-8}\) kg/s |
| Humidity ratio | \(10^{-8}\) kg/kg |
| Heat flow | 0.1 W |

Final tolerances shall be defined in `25_NumericalMethods.md`.

---

# 54. Evaluation–Commit Separation

During `Evaluate`:

- Do not mutate current loading.
- Do not mutate current bed temperature.
- Return proposed internal state.
- Return proposed outlet air.
- Return balance residuals.
- Return limiting conditions.

During `Commit`:

- Apply the accepted loading and temperature once.
- Do not repeat the physical calculation.
- Do not perform external I/O.

---

# 55. Proposed Result Model

```csharp
public sealed record SilicaGelStepResult
{
    public required MoistAirState OutletAir { get; init; }

    public required SilicaGelState ProposedState { get; init; }

    public required double EquilibriumLoadingKgPerKgDryAdsorbent { get; init; }

    public required double WaterTransferRateKgPerSecond { get; init; }

    public required double WaterTransferredThisStepKg { get; init; }

    public required double AdsorptionHeatW { get; init; }

    public required double EnvironmentalHeatLossW { get; init; }

    public required double ExternalHeatInputW { get; init; }

    public required double PressureDropPa { get; init; }

    public required ConservationBalance Balance { get; init; }

    public required IReadOnlyCollection<SimulationDiagnostic> Diagnostics { get; init; }
}
```

---

# 56. Simplified Fidelity Level 0

Configured water-transfer component:

```text
Configured water-transfer rate
Configured heat of adsorption
Capacity limits
Water balance
```

Use cases:

- Graph testing
- UI development
- Condenser integration
- Early end-to-end simulation

This level shall not be presented as predictive.

---

# 57. Fidelity Level 1

Equilibrium-limited ideal bed:

- Outlet approaches equilibrium immediately.
- Water transfer limited by available vapor and capacity.
- No kinetic delay.
- Configured heat of adsorption.
- Lumped bed temperature.

Useful for upper-bound estimates.

---

# 58. Fidelity Level 2

LDF dynamic model:

- Configurable isotherm
- Exact LDF timestep update
- Temperature-dependent kinetic coefficient
- Water and energy balance
- Lumped bed temperature
- Simple pressure drop

This is the recommended initial engineering model.

---

# 59. Fidelity Level 3

Calibrated fixed-bed model:

- Adsorption/desorption hysteresis
- Flow-dependent external transfer
- Particle-size effects
- Ergun pressure drop
- Loading-dependent heat of adsorption
- Calibrated thermal losses
- Manufacturer- or experiment-specific isotherm

---

# 60. Fidelity Level 4

Distributed bed model:

May include:

- Multiple axial gas nodes
- Multiple solid-temperature nodes
- Axial humidity profile
- Distributed mass-transfer zone
- Axial pressure drop
- Local equilibrium and kinetics

This level is outside the first implementation scope.

---

# 61. Initial Engineering Parameter Ranges

Illustrative ranges only:

| Parameter | Initial exploratory range |
|---|---:|
| Dry silica-gel mass | 0.5–5 kg |
| Maximum loading | 0.15–0.40 kg/kg |
| Practical cyclic loading swing | 0.03–0.20 kg/kg |
| Effective heat of adsorption | 2.4–3.2 MJ/kg water |
| Bed specific heat | 700–1,200 J/(kg·K) |
| LDF coefficient | \(10^{-5}\)–\(10^{-2}\) 1/s |
| Particle diameter | 1–5 mm |
| Bed void fraction | 0.30–0.50 |
| Regeneration temperature | 50–120°C |
| Airflow | 10–120 m³/h |

These ranges shall not be treated as validated values for a specific silica gel.

---

# 62. Example Adsorption Calculation

Given:

```text
Dry silica mass: 1.0 kg
Current loading: 0.10 kg/kg
Equilibrium loading: 0.20 kg/kg
LDF coefficient: 0.001 1/s
Timestep: 60 s
```

Exact update:

\[
X_{n+1}
=
0.20
+
(0.10-0.20)
e^{-0.001\cdot60}
\]

\[
X_{n+1}
\approx
0.10582
\]

Transferred water:

\[
\Delta m
=
1.0
(0.10582-0.10)
\]

\[
\Delta m
\approx
0.00582\ kg
\]

approximately:

```text
5.82 g
```

This result must still be limited by inlet vapor availability.

---

# 63. Example Desorption Calculation

Given:

```text
Dry silica mass: 1.0 kg
Current loading: 0.18 kg/kg
Hot-condition equilibrium loading: 0.08 kg/kg
LDF coefficient: 0.0015 1/s
Timestep: 60 s
```

\[
X_{n+1}
=
0.08
+
(0.18-0.08)
e^{-0.0015\cdot60}
\]

\[
X_{n+1}
\approx
0.17139
\]

Released water:

\[
-\Delta m
=
1.0
(0.18-0.17139)
\]

\[
-\Delta m
\approx
0.00861\ kg
\]

approximately:

```text
8.61 g
```

The required desorption heat and outlet-air capacity must also be checked.

---

# 64. Example Heat Requirement

For:

```text
Released water: 8.61 g in 60 s
Effective adsorption heat: 2.8 MJ/kg
```

Average thermal requirement:

\[
Q
=
\frac{
0.00861
\cdot
2.8\times10^6
}{
60
}
\]

\[
Q
\approx
401.8\ W
\]

This illustrates that rapid desorption can demand substantially more heat than a small portable collector can provide.

Therefore, the water-transfer rate shall also be energy-limited.

---

# 65. Energy-Limited Desorption

Maximum desorption rate supported by available heat:

\[
\dot m_{des,max,energy}
=
\frac{
Q_{available,desorption}
}{
h_{ads}
}
\]

The actual desorption magnitude shall satisfy:

\[
|\dot m_{des}|
\leq
\dot m_{des,max,energy}
\]

after accounting for:

- Sensible heating of air
- Sensible heating of silica
- Housing thermal mass
- Environmental losses

---

# 66. Actual Water-Transfer Limit

For adsorption:

\[
\dot m_{transfer}
=
\min
\left(
\dot m_{kinetic},
\dot m_{vapor,available},
\dot m_{capacity}
\right)
\]

For desorption magnitude:

\[
|\dot m_{transfer}|
=
\min
\left(
|\dot m_{kinetic}|,
\dot m_{stored,available},
\dot m_{energy,available},
\dot m_{air,carrying}
\right)
\]

---

# 67. Air Carrying Capacity during Desorption

At outlet temperature and pressure, maximum unsaturated vapor flow:

\[
\dot m_{v,max}
=
W_s(T_{out},p)
\dot m_{da}
\]

If desorption would exceed this value:

- Outlet becomes a supersaturated candidate.
- Condensation may occur inside or immediately after the bed.
- The model shall limit vapor transfer or route the state through an explicit phase-equilibrium calculation.

It shall not silently set RH to 100%.

---

# 68. Adsorption Heat and Bed Temperature Feedback

Adsorption heat raises bed temperature.

Higher bed temperature may reduce equilibrium loading.

This creates negative feedback:

```text
Adsorption
    ↓
Heat release
    ↓
Bed temperature rises
    ↓
Equilibrium loading may fall
    ↓
Adsorption rate decreases
```

The iterative model shall preserve this coupling.

---

# 69. Desorption Cooling Feedback

Desorption consumes heat.

If heat supply is inadequate:

```text
Desorption
    ↓
Bed cools
    ↓
Equilibrium loading may rise
    ↓
Desorption rate decreases
```

The component shall not maintain a fixed hot bed temperature without an explicit heat source capable of supplying the required energy.

---

# 70. AWG Operating Cycle

Recommended high-level cycle:

## Adsorption phase

```text
Cooler, more humid ambient air
    ↓
Silica-gel bed
    ↓
Drier exhaust air
```

## Regeneration phase

```text
Peltier hot-side preheated air
    ↓
PV rear-air channel
    ↓
Solar air collector
    ↓
Silica-gel bed
    ↓
Hot humid air
    ↓
Condenser
```

The same physical bed state persists across phases.

---

# 71. Recirculation Interaction

When humid condenser exhaust is recirculated toward the collector and bed:

- Inlet vapor pressure rises.
- Desorption driving force may decrease.
- Less water is wasted to exhaust.
- Heat may be recovered.
- Bed regeneration may slow.
- Loop convergence becomes necessary.

The silica-gel equations shall not change. Only inlet state and equilibrium conditions change.

---

# 72. Invalid Configuration Rules

Reject configuration when:

- Dry adsorbent mass is non-positive
- Maximum loading is non-positive
- Minimum loading is negative
- Minimum loading exceeds maximum loading
- Heat capacity is negative
- Heat of adsorption is non-positive
- Heat-loss coefficient is negative
- Kinetic coefficient is negative
- Reference temperature is invalid
- Activation energy is negative when the selected model requires positive value
- Bed void fraction is not between 0 and 1
- Bed area or length is non-positive
- Particle diameter is non-positive
- Reference flow is non-positive when pressure-drop model is enabled
- Required isotherm parameters are missing
- Any required numeric value is NaN or infinite

---

# 73. Runtime Diagnostics

Recommended diagnostics:

```text
Bed loading reached maximum capacity
Bed reached regenerated minimum loading
Insufficient inlet vapor for requested adsorption
Insufficient stored water for requested desorption
Insufficient thermal energy for calculated desorption
Outlet state would be supersaturated
Bed temperature outside calibrated range
Relative pressure outside isotherm range
Kinetic model outside calibrated range
Pressure drop exceeds fan capability
Bed heat loss is significant
Solver failed to converge
Water-balance residual above tolerance
Energy-balance residual above tolerance
Adsorption/desorption branch switched
Engineering-estimate parameters are active
```

---

# 74. Required Unit Tests

## SG-001 Zero driving force

When:

\[
X=X_{eq}
\]

Expected:

- Zero water transfer
- Near-equilibrium regime

## SG-002 Adsorption

When:

\[
X<X_{eq}
\]

Expected:

- Loading increases
- Outlet vapor decreases
- Adsorption heat is released
- Water balance closes

## SG-003 Desorption

When:

\[
X>X_{eq}
\]

Expected:

- Loading decreases
- Outlet vapor increases
- Heat is consumed
- Water balance closes

## SG-004 Capacity limit

Expected:

- Loading does not exceed maximum
- Limited transfer is reported

## SG-005 Minimum loading

Expected:

- Desorption does not reduce loading below minimum

## SG-006 Inlet-vapor limit

Expected:

- Adsorption cannot remove more vapor than enters

## SG-007 Stored-water limit

Expected:

- Desorption cannot release more water than stored

## SG-008 Exact LDF update

Expected:

- Numerical result matches analytical exponential solution

## SG-009 Energy-limited desorption

Expected:

- Desorption rate decreases when available heat is insufficient

## SG-010 Sensible state consistency

Expected:

- Outlet air is psychrometrically consistent

## SG-011 Dry-air conservation

Expected:

\[
m_{da,in}=m_{da,out}
\]

within tolerance.

## SG-012 Water conservation

Expected:

\[
m_{v,in}
=
m_{v,out}
+
\Delta m_{ads}
\]

within tolerance.

## SG-013 Energy conservation

Expected:

- Air enthalpy, bed storage, adsorption heat, external heat and losses balance

## SG-014 Determinism

Identical inputs produce identical outputs.

## SG-015 Timestep sensitivity

Reducing timestep shall approach a stable result for the same scenario.

---

# 75. Integration Tests

## SG-INT-001 Collector and silica gel

Expected:

- Collector outlet becomes bed inlet
- Higher regeneration temperature lowers equilibrium loading where the selected isotherm predicts it
- Desorption remains energy-limited

## SG-INT-002 Silica gel and condenser

Expected:

- Released vapor enters condenser
- Condenser water output never exceeds released plus inlet vapor
- Latent heat is included downstream

## SG-INT-003 Full adsorption cycle

Expected:

- Bed loading increases over time
- Exhaust humidity decreases
- Total captured water equals bed storage increase

## SG-INT-004 Full regeneration cycle

Expected:

- Bed loading decreases
- Humid outlet rises
- Released-water total equals bed storage decrease

## SG-INT-005 Recirculation

Expected:

- Recirculated vapor affects bed equilibrium through inlet state
- No hidden water source or sink appears

## SG-INT-006 Web and console consistency

The same configuration shall produce identical results in:

```text
ThermoCore.Console
ThermoCore.Web
ThermoCore.Desktop
```

---

# 76. Web API Configuration Example

```json
{
  "dryAdsorbentMassKg": 2.0,
  "maximumWaterLoadingKgPerKgDryAdsorbent": 0.30,
  "minimumRegeneratedLoadingKgPerKgDryAdsorbent": 0.05,
  "effectiveSpecificHeatJPerKgK": 920.0,
  "bedHousingThermalCapacityJPerK": 3500.0,
  "effectiveHeatOfAdsorptionJPerKgWater": 2800000.0,
  "bedHeatLossCoefficientWPerK": 3.5,
  "referenceMassTransferCoefficientPerSecond": 0.0005,
  "activationEnergyJPerMol": 18000.0,
  "referenceKineticTemperatureC": 25.0,
  "referencePressureDropPa": 80.0,
  "referenceVolumetricFlowM3PerHour": 60.0,
  "bedVoidFraction": 0.40,
  "bedCrossSectionAreaM2": 0.04,
  "bedLengthM": 0.15,
  "particleDiameterMm": 3.0,
  "isothermModelType": "GenericPolynomial",
  "kineticModelType": "LinearDrivingForce",
  "enableAdsorptionDesorptionHysteresis": false,
  "enableTemperatureDependentHeatOfAdsorption": false,
  "enableDetailedPressureDrop": true,
  "isothermParameters": {
    "a1": 0.12,
    "a2": 0.55,
    "a3": 0.33
  },
  "kineticParameters": {
    "flowExponent": 0.4
  }
}
```

These numerical values are illustrative placeholders and shall not be treated as validated silica-gel data.

The API layer shall convert:

- Celsius to kelvin
- m³/h to m³/s
- mm to m

before creating Core parameters.

---

# 77. Recommended C# Interfaces

```csharp
public interface ISilicaGelBedModel
{
    SilicaGelStepResult Evaluate(
        MoistAirState inletAir,
        EnvironmentState environment,
        SilicaGelState currentState,
        SilicaGelParameters parameters,
        SilicaGelControlRequest control,
        TimeSpan timeStep);
}
```

```csharp
public interface ISilicaGelKineticModel
{
    double CalculateWaterTransferRateKgPerSecond(
        double currentLoadingKgPerKg,
        double equilibriumLoadingKgPerKg,
        double bedTemperatureK,
        double dryAdsorbentMassKg,
        MoistAirState inletAir,
        SilicaGelKineticContext context);
}
```

---

# 78. Control Request Model

```csharp
public sealed record SilicaGelControlRequest
{
    public required bool Enabled { get; init; }

    public required SilicaGelRequestedMode RequestedMode { get; init; }

    public double ExternalHeatInputW { get; init; }

    public double? MaximumAllowedWaterTransferRateKgPerSecond { get; init; }
}
```

```csharp
public enum SilicaGelRequestedMode
{
    Automatic,
    AdsorptionPreferred,
    RegenerationPreferred,
    Hold
}
```

`AdsorptionPreferred` and `RegenerationPreferred` may affect airflow, heat input or selected isotherm branch, but shall not violate mass and equilibrium constraints.

---

# 79. Determinism and Thread Safety

The silica-gel model shall:

- Be deterministic
- Avoid mutable static state
- Avoid system-clock dependence
- Avoid UI dependencies
- Support parallel scenarios
- Return immutable results
- Use supplied parameters and state only
- Avoid hidden online data lookup
- Record the active isotherm and kinetic model metadata

---

# 80. Calibration Requirements

Prototype calibration should record:

```text
Inlet air temperature
Inlet RH
Inlet pressure
Outlet air temperature
Outlet RH
Dry-air mass flow
Bed temperature at multiple locations
Initial dry adsorbent mass
Initial loaded bed mass
Final loaded bed mass
Elapsed time
Solar collector heat input
Environmental temperature
Pressure drop
```

Calibration targets:

```text
Equilibrium-isotherm parameters
Adsorption branch
Desorption branch
LDF coefficient
Flow dependence
Activation energy
Effective heat of adsorption
Bed thermal capacity
Heat-loss coefficient
Pressure-drop parameters
```

Gravimetric before/after measurements are strongly recommended because outlet humidity alone can accumulate sensor error.

---

# 81. Parameter Provenance

Every parameter set shall store:

```text
Adsorbent manufacturer
Product name
Particle size
Pore type if available
Batch identifier if measured
Measurement source
Temperature range
Relative-pressure range
Fit date
Fitting method
Fit error
Document or datasheet reference
```

A generic parameter set shall be clearly marked:

```text
Not validated for a specific commercial silica gel
```

---

# 82. Acceptance Criteria

The silica-gel module is accepted when:

1. It conserves dry air.
2. It conserves water across air and adsorbent storage.
3. It conserves energy within configured tolerance.
4. Outlet RH is derived, not assigned.
5. Adsorption increases loading only when physically favored.
6. Desorption decreases loading only when physically favored.
7. Transfer is limited by vapor availability, storage, capacity and energy.
8. Adsorption heat and desorption heat demand are included.
9. Bed temperature is dynamic at engineering fidelity.
10. Pressure drop is reported.
11. Isotherm and kinetic models are replaceable.
12. Parameter provenance is retained.
13. The module has no AWG-specific UI logic.
14. It supports at least fidelity levels 0–2.
15. It produces identical results across console, web and desktop hosts.
16. It supports future prototype calibration.
17. It never claims a universal silica-gel capacity without a parameter source.

---

# 83. Relationship to Other Documents

General conservation equations:

```text
04_MathematicalModel.md
```

Psychrometric calculations:

```text
05_Psychrometrics.md
```

Solar collector regeneration heat:

```text
06_SolarCollector.md
```

Photovoltaic rear-air preheating:

```text
07_SolarPanel.md
```

Peltier hot-side heat recovery:

```text
08_Peltier.md
```

Condenser:

```text
10_Condenser.md
```

Heat recovery:

```text
11_HeatRecovery.md
```

Numerical methods:

```text
25_NumericalMethods.md
```

Constants:

```text
26_Constants.md
```

Units:

```text
27_Units.md
```

AWG V3 topology:

```text
Modules/AWG/AWG_V3_SystemDesign.md
```

---

# 84. References and Model Notes

Primary research and engineering literature should be used to calibrate a specific implementation.

Relevant model families and experimental topics include:

- Direct experimental kinetics of water adsorption on RD silica gel over operating temperatures relevant to adsorption systems.
- Linear Driving Force approximations and their differences from Fickian diffusion models.
- Fixed-bed water-vapor adsorption experiments.
- Isosteric and differential heats of water adsorption on silica.
- Adsorption/desorption hysteresis of mesoporous silica gels.

Suggested starting references:

1. Aristov, Y. I. et al., “Kinetics of water adsorption on silica Fuji Davison RD,” experimental adsorption kinetics over approximately 30–80°C.
2. El-Sharkawy, I. I. et al., work comparing Linear Driving Force and Fickian diffusion models for silica-gel/water adsorption.
3. Experimental studies of silica-based porous desiccant kinetics and fixed-bed water-vapor adsorption.
4. Experimental and theoretical studies of isosteric heat for water adsorption on silica structures.
5. Product-specific manufacturer sorption curves where available.

The implementation shall cite the exact paper, equation and fitted parameter set used.

---

# 85. Final Silica-Gel Principle

The silica-gel bed shall transfer water according to:

```text
Actual air-vapor content
Actual stored-water loading
Bed temperature
Water-vapor partial pressure
Equilibrium isotherm
Transfer kinetics
Available heat
Available airflow
Physical capacity
```

It shall always satisfy:

\[
m_{water,in}
-
m_{water,out}
-
\Delta m_{water,stored}
=
R_{water}
\]

and:

\[
E_{in}
-
E_{out}
-
\Delta E_{stored}
=
R_E
\]

where both residuals shall approach zero within numerical tolerance.

The bed shall never create water, release more water than it stores, or produce a target relative humidity without a supporting mass and energy balance.

---

**End of Document**
