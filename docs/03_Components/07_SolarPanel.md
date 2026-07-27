# ThermoCore
## 07_SolarPanel.md

**Version:** 1.0  
**Document Type:** Photovoltaic and Thermal Engineering Specification  
**Status:** Draft  
**Applies To:** ThermoCore.Core, ThermoCore.AWG and future photovoltaic modules  
**Primary implementation language:** C#  
**Internal unit system:** SI

---

# 1. Purpose

This document defines the mathematical and software model of a photovoltaic solar panel used by ThermoCore.

The photovoltaic component converts incident solar radiation into electrical power while the non-converted portion becomes heat or is reflected.

The model shall calculate:

- Incident solar power
- Reflected solar power
- Absorbed solar power
- DC electrical output power
- Cell or module temperature
- Temperature-dependent electrical efficiency
- Thermal loss to ambient air
- Thermal transfer to an optional rear airflow channel
- Stored thermal energy
- Maximum-power-point operating conditions at simplified fidelity levels
- Electrical and thermal balance residuals
- Overtemperature and invalid-operating diagnostics

The model shall remain independent from any specific user interface and from AWG-specific operating logic.

---

# 2. Scope

The initial implementation targets a rigid photovoltaic module with:

- Monocrystalline or polycrystalline silicon cells
- Transparent front cover
- Encapsulant
- Solar cells
- Rear sheet or rear glass
- Optional aluminum frame
- Optional rear air channel
- Optional thermal mass
- Connection to a simplified MPPT or battery model

The first implementation shall support:

- Variable plane-of-array irradiance
- Variable ambient temperature
- Variable wind speed
- Adjustable tilt and azimuth
- Temperature-dependent output power
- Dynamic module temperature
- Standalone operation
- Rear-air cooling
- Electrical power limitation by downstream equipment
- Zero-load and disconnected conditions

The first implementation shall not require:

- Cell-level electrical network simulation
- Partial-shading bypass-diode simulation
- Detailed I–V curve fitting
- Electroluminescence or degradation modelling
- Detailed spectral response
- Detailed semiconductor physics
- Structural or mechanical stress analysis
- Full inverter waveform simulation
- Detailed MPPT control transients

---

# 3. Architectural Placement

The generic photovoltaic model shall be implemented in:

```text
ThermoCore.Core
```

Recommended namespace:

```csharp
ThermoCore.Core.Components.Photovoltaics
```

AWG-specific panel configuration shall be implemented in:

```text
ThermoCore.AWG
```

The generic photovoltaic component shall not know:

- Whether a Peltier module is downstream
- Whether the device produces water
- Whether an AWG adsorption or regeneration cycle is active
- Which UI host runs the simulation
- Whether the generated power is consumed by a fan, battery, controller or another load

---

# 4. Component Classification

The photovoltaic panel is:

```text
Solar-energy conversion component
Electrical-energy source component
Thermal-storage component
Environmental heat-transfer component
Optional air-heating component
```

It converts:

```text
Solar radiation
        ↓
DC electrical power
        +
Thermal energy
        +
Reflected/transmitted radiation
```

The energy model shall explicitly account for all major output paths.

---

# 5. Ports

Recommended ports:

```text
SolarRadiationIn
ElectricalPowerOut
AmbientHeatOut
OptionalRearAirIn
OptionalRearAirOut
OptionalControlIn
```

Optional diagnostic ports:

```text
ModuleTemperatureMeasurement
CellTemperatureMeasurement
RearSurfaceTemperatureMeasurement
ElectricalOperatingPointMeasurement
```

---

# 6. Internal State

The panel shall be stateful when dynamic thermal behavior is enabled.

Recommended state:

```csharp
public sealed record SolarPanelState
{
    public required double ModuleTemperatureK { get; init; }

    public required double RearSurfaceTemperatureK { get; init; }

    public required double StoredThermalEnergyJ { get; init; }

    public required double LastElectricalPowerW { get; init; }

    public required double LastElectricalEfficiencyFraction { get; init; }

    public required double LastAbsorbedSolarPowerW { get; init; }

    public required bool IsElectricallyConnected { get; init; }
}
```

A simplified fidelity level may use only module temperature and electrical power.

---

# 7. Configuration Model

Recommended configuration:

```csharp
public sealed record SolarPanelParameters
{
    public required double ApertureAreaM2 { get; init; }

    public required double RatedPowerW { get; init; }

    public required double ReferenceIrradianceWPerM2 { get; init; }

    public required double ReferenceCellTemperatureK { get; init; }

    public required double ReferenceEfficiencyFraction { get; init; }

    public required double PowerTemperatureCoefficientPerK { get; init; }

    public required double SolarAbsorptanceFraction { get; init; }

    public required double FrontSurfaceEmissivityFraction { get; init; }

    public required double RearSurfaceEmissivityFraction { get; init; }

    public required double EffectiveThermalCapacityJPerK { get; init; }

    public required double FrontHeatTransferCoefficientWPerM2K { get; init; }

    public required double RearHeatTransferCoefficientWPerM2K { get; init; }

    public required double MaximumAllowedModuleTemperatureK { get; init; }

    public double TiltAngleRadians { get; init; }

    public double AzimuthAngleRadians { get; init; }

    public double OpticalReflectionFraction { get; init; }

    public double OpticalTransmissionFraction { get; init; }

    public double SoilingLossFraction { get; init; }

    public double WiringEfficiencyFraction { get; init; } = 1.0;

    public double MpptEfficiencyFraction { get; init; } = 1.0;

    public double RearAirHeatTransferAreaM2 { get; init; }

    public double RearAirPressureDropReferencePa { get; init; }

    public double RearAirReferenceFlowM3PerSecond { get; init; }

    public double NightRadiationEnabled { get; init; }
}
```

---

# 8. Required Inputs

The panel shall receive or derive:

```text
Plane-of-array solar irradiance
Ambient-air temperature
Ambient pressure
Wind speed
Sky temperature when radiative cooling is enabled
Electrical-load availability
Downstream voltage or power limit at higher fidelity
Optional rear-air inlet state
Optional rear-air mass flow
```

The minimum first-version input set is:

```text
Plane-of-array irradiance
Ambient temperature
Wind speed
Electrical connection state
```

---

# 9. Plane-of-Array Irradiance

The photovoltaic panel shall accept:

\[
G_{poa}
\]

where:

- \(G_{poa}\) is total irradiance on the panel plane
- Unit: W/m²

Solar-position and irradiance-transposition calculations should be performed by an external weather or solar-geometry service.

The photovoltaic component shall not calculate astronomical solar position in its initial implementation.

---

# 10. Incident Solar Power

Incident solar power is:

\[
P_{solar,incident}
=
G_{poa}A_{panel}
\]

where:

- \(A_{panel}\) is active aperture area
- Result is in watts

---

# 11. Optical Losses

The incident radiation may be separated into:

\[
P_{incident}
=
P_{reflected}
+
P_{transmitted}
+
P_{absorbed}
\]

Using configured fractions:

\[
P_{reflected}
=
f_{reflection}P_{incident}
\]

\[
P_{transmitted}
=
f_{transmission}P_{incident}
\]

\[
P_{absorbed}
=
P_{incident}
-
P_{reflected}
-
P_{transmitted}
\]

The fractions shall satisfy:

\[
0
\leq
f_{reflection}+f_{transmission}
\leq
1
\]

For an opaque module, transmitted power will normally be near zero.

---

# 12. Soiling Loss

Soiling may reduce effective irradiance:

\[
G_{effective}
=
G_{poa}
(1-f_{soiling})
\]

where:

\[
0\leq f_{soiling}<1
\]

The model shall define whether soiling is applied before or after reflection losses.

Recommended convention:

1. Reduce incident usable radiation by soiling.
2. Apply optical reflection and transmission.
3. Calculate electrical conversion and thermal absorption.

---

# 13. Reference Electrical Power

At standard reference conditions:

\[
P_{ref}
=
\eta_{ref}
G_{ref}
A_{panel}
\]

The configured values should satisfy this relation approximately.

If both `RatedPowerW` and `ReferenceEfficiencyFraction` are provided, validation shall detect significant inconsistency.

Relative inconsistency:

\[
\delta
=
\frac{
\left|
P_{rated}
-
\eta_{ref}G_{ref}A
\right|
}{
\max(P_{rated},P_{minimum})
}
\]

A warning shall be emitted when the difference exceeds configured tolerance.

---

# 14. Temperature-Dependent Power

A simplified temperature-corrected maximum-power model:

\[
P_{dc,raw}
=
P_{rated}
\frac{G_{effective}}{G_{ref}}
\left[
1+
\gamma_P
(T_{cell}-T_{ref})
\right]
\]

where:

- \(\gamma_P\) is the power temperature coefficient in 1/K
- It is usually negative for crystalline silicon
- \(T_{cell}\) and \(T_{ref}\) are in kelvin, but their difference equals the Celsius temperature difference numerically

The corrected factor shall not be allowed to become negative.

---

# 15. Electrical Efficiency

Electrical efficiency at current module temperature may be represented as:

\[
\eta_{el}(T)
=
\eta_{ref}
\left[
1+
\gamma_P
(T_{cell}-T_{ref})
\right]
\]

Then:

\[
P_{dc,raw}
=
\eta_{el}
G_{effective}
A_{panel}
\]

Efficiency shall be validated and physically bounded.

A normal passive photovoltaic panel shall satisfy:

\[
0\leq\eta_{el}\leq1
\]

---

# 16. MPPT and Wiring Losses

Delivered DC power:

\[
P_{dc,delivered}
=
P_{dc,raw}
\eta_{mppt}
\eta_{wiring}
\]

where:

\[
0\leq\eta_{mppt}\leq1
\]

\[
0\leq\eta_{wiring}\leq1
\]

Losses:

\[
P_{mppt,loss}
=
P_{dc,raw}
(1-\eta_{mppt})
\]

\[
P_{wiring,loss}
=
P_{dc,raw}
\eta_{mppt}
(1-\eta_{wiring})
\]

These losses shall be assigned to:

- Thermal losses to environment
- Converter heat
- Wiring heat
- A generic electrical-loss sink

They shall not disappear from the total energy balance.

---

# 17. Downstream Power Limitation

The electrical system may accept less power than the panel can produce.

Define:

\[
P_{accepted}
=
\min
(
P_{dc,available},
P_{load,max},
P_{charge,max}
)
\]

Curtailment power:

\[
P_{curtailed}
=
P_{dc,available}
-
P_{accepted}
\]

Curtailed power does not automatically become useful electrical output.

Depending on the electrical operating model, it may lead to:

- Different panel operating point
- Increased reflection or reduced charge extraction
- Additional panel heating
- No additional generated energy

The first simplified implementation may treat curtailed conversion potential as additional thermal load on the panel, but this assumption shall be explicit and configurable.

---

# 18. Disconnected or Open-Circuit Condition

When the panel is electrically disconnected:

\[
P_{electrical,out}=0
\]

The absorbed solar energy then becomes:

- Thermal energy
- Reflected radiation
- Transmitted radiation
- Minor internal electrical effects neglected in the first model

The panel may reach a higher temperature than under loaded operation.

The component shall distinguish:

```text
Connected and generating
Connected but curtailed
Disconnected
No irradiance
Faulted
```

---

# 19. Photovoltaic Thermal Balance

The general dynamic thermal balance is:

\[
C_{pv}
\frac{dT_{pv}}{dt}
=
P_{solar,absorbed}
-
P_{electrical,out}
-
Q_{front}
-
Q_{rear}
-
Q_{radiation}
-
Q_{rear-air}
\]

where:

- \(C_{pv}\) is effective thermal capacity
- \(Q_{front}\) is front convective heat loss
- \(Q_{rear}\) is rear convective or conductive heat loss
- \(Q_{radiation}\) is long-wave radiative heat transfer
- \(Q_{rear-air}\) is useful heat transferred to a rear airflow channel

---

# 20. Effective Thermal Capacity

\[
C_{pv}
=
\sum_i m_i c_{p,i}
\]

It may include:

- Glass
- Encapsulant
- Solar cells
- Rear sheet
- Aluminum frame
- Junction box
- A fraction of mounting hardware

The first implementation may use one effective calibrated value.

---

# 21. Front Convective Heat Loss

\[
Q_{front}
=
h_{front}A_{front}
(T_{pv}-T_{ambient})
\]

A wind-dependent coefficient may be used:

\[
h_{front}
=
a+bv_{wind}
\]

The coefficients shall be documented and treated as empirical parameters.

---

# 22. Rear Heat Loss without Air Channel

\[
Q_{rear}
=
h_{rear}A_{rear}
(T_{rear}-T_{ambient})
\]

If the panel is mounted close to another surface, rear heat transfer may differ significantly from open-rack operation.

The model shall expose a configurable rear heat-transfer coefficient.

---

# 23. Long-Wave Radiation

Net long-wave radiation to the surroundings:

\[
Q_{rad}
=
\varepsilon
\sigma
A
\left(
T_{surface}^4
-
T_{surroundings}^4
\right)
\]

Front and rear radiation may be calculated separately.

All temperatures shall be in kelvin.

---

# 24. Rear Air Channel

The AWG concept may route inlet air beneath the photovoltaic panel.

The rear air channel serves two functions:

```text
Cooling the photovoltaic panel
Preheating incoming process air
```

This airflow path shall be modelled separately from the solar air collector.

The panel rear channel is not itself a true solar collector unless it receives direct solar radiation through a transparent or exposed absorber arrangement.

---

# 25. Rear-Air Heat Transfer

Heat transferred from panel to rear airflow:

\[
Q_{rear-air}
=
h_{rear-air}
A_{rear-air}
(T_{rear}-T_{air,mean})
\]

Alternatively, using effectiveness:

\[
\varepsilon_{rear-air}
=
1-
\exp
\left(
-\frac{UA_{rear-air}}{C_{air}}
\right)
\]

\[
Q_{rear-air}
=
\varepsilon_{rear-air}
C_{air}
(T_{rear}-T_{air,in})
\]

Outlet temperature:

\[
T_{air,out}
=
T_{air,in}
+
\frac{Q_{rear-air}}{C_{air}}
\]

The outlet temperature shall not exceed the effective rear-surface temperature within tolerance.

---

# 26. Rear-Air Moisture Behavior

The panel rear channel is normally a sensible-heating component.

Therefore:

\[
W_{out}=W_{in}
\]

\[
\dot m_{v,out}
=
\dot m_{v,in}
\]

The dew point remains unchanged when pressure and humidity ratio remain unchanged.

Relative humidity normally decreases as air warms.

---

# 27. Rear-Air Condensation Risk

Condensation may occur on a cold rear surface during night operation or startup when:

\[
T_{rear}
<
T_{dp,in}
\]

The first model may:

- Emit a warning
- Disable the rear-air heat-transfer path
- Delegate phase change to an explicit condensation component

It shall not silently remove water.

---

# 28. Rear-Air Pressure Drop

A simple quadratic pressure-drop model:

\[
\Delta p
=
\Delta p_{ref}
\left(
\frac{\dot V}{\dot V_{ref}}
\right)^2
\]

The pressure drop shall be reported to the airflow network.

The photovoltaic component shall not independently set fan flow when a coupled pressure-flow solver is active.

---

# 29. Module Temperature Approximation

A simple empirical temperature model may use nominal operating cell temperature:

\[
T_{cell,C}
=
T_{ambient,C}
+
\frac{NOCT-20}{800}
G_{poa}
\]

This relation may be used only as a low-fidelity approximation.

It shall be corrected or replaced when:

- Rear airflow cooling is active
- Wind varies significantly
- Mounting is not open rack
- Dynamic thermal behavior matters
- High accuracy is required

The preferred engineering model is the dynamic thermal balance.

---

# 30. Dynamic Temperature Update

Explicit Euler:

\[
T_{pv,n+1}
=
T_{pv,n}
+
\frac{
P_{absorbed}
-
P_{electrical}
-
Q_{front}
-
Q_{rear}
-
Q_{rad}
-
Q_{rear-air}
}{
C_{pv}
}
\Delta t
\]

The timestep shall satisfy stability requirements.

---

# 31. Semi-Implicit Temperature Update

For linearized total thermal conductance:

\[
UA_{total}
=
UA_{front}
+
UA_{rear}
+
UA_{rear-air}
+
UA_{rad,linearized}
\]

A semi-implicit update:

\[
T_{n+1}
=
\frac{
C T_n/\Delta t
+
P_{absorbed}
-
P_{electrical}
+
UA_{front}T_{ambient}
+
UA_{rear}T_{ambient}
+
UA_{rear-air}T_{air,in}
+
UA_{rad}T_{sky}
}{
C/\Delta t
+
UA_{total}
}
\]

This model is recommended when the thermal time constant is short.

---

# 32. Thermal Time Constant

\[
\tau
=
\frac{C_{pv}}{UA_{total}}
\]

Explicit timestep guideline:

\[
\Delta t
\leq
\frac{\tau}{10}
\]

The model shall warn when the simulation timestep is too large.

---

# 33. Electrical–Thermal Coupling

Electrical power depends on temperature:

\[
P_{electrical}=f(T_{pv})
\]

Panel temperature depends on electrical power extraction:

\[
T_{pv}=g(P_{electrical})
\]

The model is therefore coupled.

Recommended first implementation:

1. Use current or previous panel temperature.
2. Calculate provisional electrical power.
3. Calculate proposed next panel temperature.
4. Optionally repeat once or until convergence.
5. Commit the converged or accepted state.

---

# 34. Fixed-Point Iteration

At each timestep:

```text
1. Guess module temperature.
2. Calculate electrical efficiency.
3. Calculate electrical output.
4. Calculate thermal source term.
5. Calculate updated module temperature.
6. Compare temperatures.
7. Repeat until convergence.
```

Relaxation may be used if required.

---

# 35. Energy Balance

For one timestep:

\[
E_{incident}
=
E_{reflected}
+
E_{transmitted}
+
E_{electrical}
+
E_{thermal,out}
+
\Delta E_{stored}
+
R_E
\]

Where thermal outflow includes:

```text
Front convection
Rear convection
Long-wave radiation
Rear-air useful heat
Electrical-conversion losses
```

The residual shall approach zero within tolerance.

---

# 36. Electrical Balance

\[
E_{electrical,raw}
=
E_{delivered}
+
E_{mppt,loss}
+
E_{wiring,loss}
+
E_{curtailed}
\]

The treatment of curtailed power shall be explicitly documented.

---

# 37. Solar Absorption and Electrical Conversion

Electrical output shall not exceed absorbed solar power:

\[
P_{electrical}
\leq
P_{solar,absorbed}
\]

Any result violating this condition shall be rejected.

---

# 38. Temperature Coefficient Convention

The power temperature coefficient shall use:

```text
fractional power change per kelvin
```

Example:

```text
−0.004 per K = −0.4% per K
```

Prohibited ambiguous input:

```text
−0.4
```

unless the property explicitly states percent per degree.

Recommended property name:

```csharp
PowerTemperatureCoefficientPerK
```

---

# 39. Rated Power Consistency

The model shall optionally derive reference efficiency:

\[
\eta_{ref,derived}
=
\frac{
P_{rated}
}{
G_{ref}A
}
\]

If a separate configured efficiency differs, diagnostics shall report:

```text
Rated-power-derived efficiency
Configured efficiency
Relative difference
Selected authoritative value
```

---

# 40. Maximum Allowed Temperature

The component shall enforce or monitor:

\[
T_{pv}
\leq
T_{max,allowed}
\]

When exceeded:

- Emit a critical diagnostic
- Optionally reduce electrical output
- Optionally force fan operation
- Optionally enter thermal-protection mode
- Do not silently clamp stored thermal energy

---

# 41. Low-Irradiance Cutoff

Below a configurable irradiance threshold:

\[
G_{poa}<G_{min}
\]

the panel may produce zero electrical output.

The thermal model shall still operate, including night cooling.

---

# 42. Night Cooling

At night:

\[
P_{solar,incident}=0
\]

The panel may lose heat through:

- Convection
- Long-wave radiation to sky
- Rear airflow
- Conduction to mounting structure

It may cool below ambient under clear-sky conditions.

This effect may be enabled at higher fidelity.

---

# 43. Reverse Electrical Flow

The first model shall not allow reverse current into the panel.

If the electrical network attempts reverse power:

- Reject the state
- Emit a diagnostic
- Model blocking-diode behavior at a higher level

---

# 44. Partial Shading

Detailed partial-shading behavior is outside the first implementation.

A simplified scalar shading factor may be supported:

\[
G_{effective}
=
f_{shade}G_{poa}
\]

where:

\[
0\leq f_{shade}\leq1
\]

This scalar approximation does not model mismatch losses or bypass diodes.

---

# 45. Incidence-Angle Modifier

The panel may receive an externally calculated optical modifier:

\[
K_\theta
\]

Then:

\[
G_{effective}
=
G_{poa}
K_\theta
(1-f_{soiling})
f_{shade}
\]

The modifier shall not be applied twice if already included in plane-of-array irradiance.

---

# 46. Orientation Requirement for AWG V3

For the AWG V3 concept:

- The photovoltaic panel and solar air collector shall be side by side.
- They shall share approximately equal tilt and azimuth.
- Neither shall shade the other under nominal operation.
- The panel shall have its own rear airflow channel.
- The rear channel shall discharge toward the true solar air collector or another defined process component.
- The collector shall remain directly exposed to sunlight.
- The panel shall not be mounted above the collector absorber.
- The entire assembly may use common adjustable support legs.

These are AWG configuration requirements, not generic photovoltaic equations.

---

# 47. Interaction with Peltier Hot Side

The current AWG topology may route air through:

```text
Ambient air
    ↓
Peltier hot-side heat exchanger
    ↓
Photovoltaic rear-air channel
    ↓
Solar air collector
```

The panel rear-air inlet state already includes Peltier hot-side heat.

The photovoltaic component shall add only heat transferred from the panel.

The solar air collector shall add only its own absorbed solar heat.

This prevents double counting.

---

# 48. Recommended Component Evaluation Sequence

```text
1. Read solar-radiation state.
2. Read ambient environment.
3. Read electrical-network constraints.
4. Read optional rear-air inlet state.
5. Validate parameters.
6. Calculate effective irradiance.
7. Calculate incident, reflected, transmitted and absorbed power.
8. Estimate or iterate module temperature.
9. Calculate temperature-corrected electrical power.
10. Apply MPPT, wiring and load limits.
11. Calculate thermal heat-transfer paths.
12. Calculate rear-air outlet state.
13. Calculate pressure drop.
14. Calculate proposed internal state.
15. Calculate electrical and thermal balance residuals.
16. Return diagnostics.
```

---

# 49. Evaluation–Commit Separation

During `Evaluate`:

- The component shall not mutate current state.
- It shall calculate proposed module temperature.
- It shall calculate proposed electrical output.
- It shall calculate proposed rear-air state.
- It shall calculate balance residuals.

During `Commit`:

- The proposed state becomes current.
- Accepted electrical and thermal results are stored.
- Diagnostics may be added to simulation history.

---

# 50. Proposed Result Model

```csharp
public sealed record SolarPanelStepResult
{
    public required SolarPanelState ProposedState { get; init; }

    public required ElectricalPowerState ElectricalOutput { get; init; }

    public MoistAirState? RearAirOutlet { get; init; }

    public required double IncidentSolarPowerW { get; init; }

    public required double ReflectedSolarPowerW { get; init; }

    public required double TransmittedSolarPowerW { get; init; }

    public required double AbsorbedSolarPowerW { get; init; }

    public required double RawElectricalPowerW { get; init; }

    public required double DeliveredElectricalPowerW { get; init; }

    public required double CurtailedPowerW { get; init; }

    public required double FrontHeatLossW { get; init; }

    public required double RearHeatLossW { get; init; }

    public required double RadiativeHeatLossW { get; init; }

    public required double RearAirUsefulHeatW { get; init; }

    public required double PressureDropPa { get; init; }

    public required ConservationBalance Balance { get; init; }
}
```

---

# 51. Simplified Fidelity Level 0

Ideal electrical source:

\[
P_{out}
=
P_{configured}
\]

Use cases:

- Graph testing
- Electrical-network testing
- UI development
- Battery integration tests

No thermal model is required.

---

# 52. Fidelity Level 1

Constant-efficiency photovoltaic model:

\[
P_{out}
=
\eta_{constant}
G_{poa}A
\]

Includes:

- Irradiance dependence
- No temperature correction
- No thermal inertia
- Optional fixed losses

---

# 53. Fidelity Level 2

Temperature-corrected static model:

Includes:

- Reference rated power
- Temperature coefficient
- Empirical module temperature estimate
- MPPT efficiency
- Wiring efficiency
- Load limitation
- No dynamic thermal state

---

# 54. Fidelity Level 3

Dynamic electrothermal model:

Includes:

- Thermal capacity
- Front and rear convection
- Long-wave radiation
- Temperature-dependent electrical output
- Rear-air cooling and heating
- Pressure drop
- Coupled temperature iteration

This is the recommended initial AWG engineering model.

---

# 55. Fidelity Level 4

Manufacturer-data calibrated model:

May include:

- Manufacturer temperature coefficients
- Measured module temperature
- Measured airflow cooling
- Detailed power curve
- Measured MPPT behavior
- Degradation factors
- Calibration against physical prototype data

---

# 56. Fidelity Level 5

Advanced electrical model:

May include:

- Single-diode equivalent circuit
- Full I–V curve
- Series and shunt resistance
- Partial shading
- Bypass diodes
- Cell mismatch
- Converter operating voltage

This level is outside the first implementation scope.

---

# 57. Initial AWG Prototype Parameter Ranges

Illustrative engineering ranges:

| Parameter | Initial range |
|---|---:|
| Panel area | 0.15–0.6 m² |
| Rated power | 20–120 W |
| Reference efficiency | 0.15–0.24 |
| Temperature coefficient | −0.0025 to −0.005 per K |
| Effective thermal capacity | 5,000–30,000 J/K |
| Front heat-transfer coefficient | 5–30 W/(m²·K) |
| Rear heat-transfer coefficient | 3–25 W/(m²·K) |
| Rear-air heat-transfer coefficient | 5–50 W/(m²·K) |
| Rear-air flow | 10–120 m³/h |
| Maximum module temperature | 348–373 K |
| MPPT efficiency | 0.90–0.99 |
| Wiring efficiency | 0.95–0.995 |

These values are placeholders for sensitivity analysis and shall not be treated as validated hardware data.

---

# 58. Example Electrical Calculation

Given:

```text
Rated power: 50 W
Reference irradiance: 1000 W/m²
Reference temperature: 25°C
Current irradiance: 900 W/m²
Module temperature: 55°C
Temperature coefficient: −0.004 per K
MPPT efficiency: 0.96
Wiring efficiency: 0.98
```

Temperature difference:

\[
\Delta T
=
55-25
=
30\ K
\]

Temperature factor:

\[
1+\gamma_P\Delta T
=
1-0.004\cdot30
=
0.88
\]

Raw electrical power:

\[
P_{raw}
=
50
\cdot
0.9
\cdot
0.88
=
39.6\ W
\]

Delivered power:

\[
P_{delivered}
=
39.6
\cdot
0.96
\cdot
0.98
\approx
37.25\ W
\]

---

# 59. Example Rear-Air Heating

Assume:

```text
Panel-to-air useful heat: 45 W
Dry-air mass flow: 0.015 kg/s
Humidity ratio: 0.010 kg/kg dry air
```

Air heat-capacity rate:

\[
C_{air}
=
0.015
(1006+0.010\cdot1860)
\]

\[
C_{air}
=
15.369\ W/K
\]

Temperature increase:

\[
\Delta T
=
\frac{45}{15.369}
\approx
2.93\ K
\]

This heat is useful as process-air preheating but is substantially smaller than what a dedicated directly irradiated solar air collector may provide.

---

# 60. Energy-Balance Example

Given:

```text
Incident solar power: 180 W
Reflected and transmitted: 20 W
Electrical output: 32 W
Front and rear thermal loss: 75 W
Rear-air useful heat: 40 W
Stored-energy increase: 13 W equivalent
```

Balance:

\[
180
-
20
-
32
-
75
-
40
-
13
=
0
\]

The component is balanced.

---

# 61. Optimization Objectives

The optimal panel operating condition is not necessarily the lowest temperature or the highest rear-air heat.

Possible objectives:

```text
Maximum electrical energy
Maximum total useful electrical plus thermal energy
Maximum AWG daily water production
Minimum energy consumption per liter
Maximum Peltier available power
Minimum battery cycling
Maximum panel lifetime
Maximum combined PV and collector efficiency
```

System optimization shall be performed outside the photovoltaic component.

---

# 62. Invalid Configuration Rules

Reject configuration when:

- Panel area is non-positive
- Rated power is negative
- Reference irradiance is non-positive
- Reference efficiency is outside 0–1
- Temperature coefficient is non-finite
- Solar absorptance is outside 0–1
- Reflection or transmission fractions are outside 0–1
- Their sum exceeds 1
- Thermal capacity is negative
- Heat-transfer coefficients are negative
- MPPT efficiency is outside 0–1
- Wiring efficiency is outside 0–1
- Maximum temperature is below the intended operating range
- Reference rear-air flow is non-positive when pressure-drop model is enabled
- Any required value is NaN or infinite

---

# 63. Runtime Diagnostics

Recommended diagnostics:

```text
Module approaching maximum temperature
Module overtemperature
Electrical output exceeds absorbed solar power
Reference rating inconsistent with area and efficiency
Rear airflow below cooling requirement
Rear-air outlet exceeds module temperature
Pressure drop exceeds fan capability
Temperature iteration failed to converge
Irradiance outside configured range
Negative electrical output requested
Reverse power blocked
Curtailed power is significant
Energy-balance residual above tolerance
Condensation risk in rear-air channel
Timestep may be thermally unstable
```

---

# 64. Required Unit Tests

## PV-001 Zero irradiance

Expected:

- Zero solar electrical output
- Zero absorbed solar input
- Thermal state may cool toward environment
- Energy balance remains valid

## PV-002 Reference conditions

At configured reference conditions:

- Raw electrical output approximately equals rated power
- Derived efficiency matches configured value within tolerance

## PV-003 Temperature effect

At equal irradiance:

- Higher module temperature reduces output for a negative coefficient

## PV-004 Rear-air cooling

With rear airflow enabled:

- Module temperature decreases compared with no-flow case
- Rear-air temperature increases
- Humidity ratio remains unchanged
- Dew point remains unchanged

## PV-005 Disconnected condition

Expected:

- Electrical output is zero
- Module temperature is higher or equal to loaded operation under equal conditions

## PV-006 Electrical balance

Expected:

\[
E_{raw}
=
E_{delivered}
+
E_{loss}
+
E_{curtailed}
\]

within tolerance.

## PV-007 Total energy balance

Expected:

\[
E_{incident}
=
E_{reflected}
+
E_{transmitted}
+
E_{electrical}
+
E_{thermal}
+
\Delta E_{stored}
\]

within tolerance.

## PV-008 Pressure-drop scaling

For a quadratic rear-channel model:

- Doubling airflow produces approximately four times pressure drop

## PV-009 State consistency

Expected:

- Rear-air output is psychrometrically consistent

## PV-010 Temperature limit

Expected:

- Overtemperature generates a diagnostic
- No silent energy loss occurs

---

# 65. Integration Tests

## PV-INT-001 Panel and battery

Expected:

- Panel electrical output is limited by battery charge capability
- Curtailment is reported
- Battery receives accepted power only

## PV-INT-002 Panel and Peltier

Expected:

- Peltier power consumption cannot exceed available electrical power unless battery support exists
- Electrical energy is conserved

## PV-INT-003 Panel rear air and collector

Expected:

- Rear-air outlet becomes solar-collector inlet
- Panel heat and collector heat are counted separately
- Humidity ratio remains unchanged through both components

## PV-INT-004 Web and console consistency

The same configuration shall produce identical results in:

```text
ThermoCore.Console
ThermoCore.Web
ThermoCore.Desktop
```

## PV-INT-005 Common tilt

Changing AWG assembly tilt shall affect both panel and collector plane-of-array irradiance consistently through the external solar-geometry service.

---

# 66. Web API Configuration Example

```json
{
  "apertureAreaM2": 0.32,
  "ratedPowerW": 50.0,
  "referenceIrradianceWPerM2": 1000.0,
  "referenceCellTemperatureC": 25.0,
  "referenceEfficiencyFraction": 0.195,
  "powerTemperatureCoefficientPerK": -0.004,
  "solarAbsorptanceFraction": 0.88,
  "frontSurfaceEmissivityFraction": 0.84,
  "rearSurfaceEmissivityFraction": 0.90,
  "effectiveThermalCapacityJPerK": 14000.0,
  "frontHeatTransferCoefficientWPerM2K": 12.0,
  "rearHeatTransferCoefficientWPerM2K": 7.0,
  "maximumAllowedModuleTemperatureC": 85.0,
  "tiltAngleDegrees": 35.0,
  "azimuthAngleDegrees": 180.0,
  "opticalReflectionFraction": 0.08,
  "opticalTransmissionFraction": 0.01,
  "soilingLossFraction": 0.03,
  "wiringEfficiencyFraction": 0.98,
  "mpptEfficiencyFraction": 0.96,
  "rearAirHeatTransferAreaM2": 0.30,
  "rearAirPressureDropReferencePa": 25.0,
  "rearAirReferenceFlowM3PerHour": 60.0
}
```

The API layer shall convert:

- Celsius to kelvin
- Degrees to radians
- m³/h to m³/s

before creating Core parameters.

---

# 67. Recommended C# Interface

```csharp
public interface ISolarPanelModel
{
    SolarPanelStepResult Evaluate(
        SolarRadiationState solarRadiation,
        EnvironmentState environment,
        ElectricalNetworkConstraint electricalConstraint,
        SolarPanelState currentState,
        SolarPanelParameters parameters,
        TimeSpan timeStep,
        MoistAirState? rearAirInlet = null);
}
```

---

# 68. Electrical Network Constraint

Recommended input model:

```csharp
public sealed record ElectricalNetworkConstraint
{
    public required bool IsConnected { get; init; }

    public required double MaximumAcceptedPowerW { get; init; }

    public double? RequestedVoltageV { get; init; }

    public double? RequestedCurrentA { get; init; }
}
```

The initial model may use only `IsConnected` and `MaximumAcceptedPowerW`.

---

# 69. Determinism and Thread Safety

The photovoltaic model shall:

- Be deterministic
- Avoid mutable static state
- Avoid dependence on system clock
- Avoid UI dependencies
- Support parallel scenario execution
- Return immutable results
- Use only supplied input state, parameters and simulation context

---

# 70. Calibration Requirements

Future prototype calibration should record:

```text
Plane-of-array irradiance
Ambient temperature
Wind speed
Module front temperature
Module rear temperature
Rear-air inlet temperature
Rear-air outlet temperature
Rear airflow
Panel voltage
Panel current
Electrical output power
Battery charge power
Peltier power
```

Calibration targets:

```text
Temperature coefficient
Effective thermal capacity
Front heat-transfer coefficient
Rear heat-transfer coefficient
Rear-air UA
MPPT efficiency
Wiring efficiency
Pressure-drop curve
```

---

# 71. Acceptance Criteria

The solar-panel module is accepted when:

1. It conserves total solar, electrical and thermal energy within tolerance.
2. Electrical output decreases with rising temperature for a negative coefficient.
3. Electrical output never exceeds absorbed solar power.
4. Rear airflow removes heat from the panel and heats the air consistently.
5. Rear-air humidity ratio and dew point remain unchanged during sensible heating.
6. Disconnected operation produces zero electrical output.
7. Load limitation and curtailment are explicit.
8. Overtemperature is detected.
9. The module has no AWG-specific control logic.
10. It supports at least fidelity levels 0–3.
11. It produces identical results in console, desktop and web hosts.
12. It does not treat the shaded rear airflow path as a directly irradiated solar collector.
13. It reports thermal and electrical residuals independently.
14. It supports common AWG tilt and azimuth through external orientation input.

---

# 72. Relationship to Other Documents

General conservation equations:

```text
04_MathematicalModel.md
```

Psychrometric calculations:

```text
05_Psychrometrics.md
```

Solar air collector:

```text
06_SolarCollector.md
```

Peltier electrical load and heat rejection:

```text
08_Peltier.md
```

Battery and charging:

```text
12_Battery.md
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

Web application architecture:

```text
Modules/Web/ThermoCore_WebArchitecture.md
```

---

# 73. Final Photovoltaic Principle

The photovoltaic panel shall split incident solar energy into explicit paths:

```text
Reflected radiation
Transmitted radiation
Delivered electrical power
Electrical-conversion losses
Useful rear-air heating
Environmental heat loss
Stored thermal energy
```

The total balance shall satisfy:

\[
E_{incident}
-
E_{reflected}
-
E_{transmitted}
-
E_{electrical}
-
E_{thermal,out}
-
\Delta E_{stored}
=
R_E
\]

where the residual shall approach zero within numerical tolerance.

The panel rear-air channel may cool the panel and preheat process air, but it shall not be treated as a substitute for a separate directly irradiated solar air collector.

---

**End of Document**
