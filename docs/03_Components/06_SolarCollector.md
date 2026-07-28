# ThermoCore



## 06_SolarCollector.md



**Version:** 1.0

**Document Type:** Solar Air Collector Engineering and Mathematical Specification

**Status:** Draft

**Applies To:** ThermoCore.Core, ThermoCore.AWG and future solar-thermal modules

**Primary implementation language:** C#

**Internal unit system:** SI



---



# 1. Purpose



This document defines the mathematical and software model of a solar air collector used by ThermoCore.



The collector converts incident solar radiation into thermal energy and transfers part of that energy to an airflow.



The model shall calculate:



* Incident solar power

* Absorbed solar power

* Collector thermal losses

* Absorber temperature

* Outlet-air temperature

* Useful thermal power

* Collector efficiency

* Thermal-storage change

* Pressure drop

* Fan interaction

* Condensation risk

* Stagnation conditions

* Energy-balance residual

* Dynamic response over time



The collector model shall be independent from Atmospheric Water Generator-specific control logic.



---



# 2. Scope



The initial implementation targets a flat-plate solar air collector consisting of:



* Transparent cover

* Air gap

* Black or selective absorber

* Air channel

* Insulated back and sides

* Inlet and outlet openings

* Optional internal fins or turbulence promoters

* Optional thermal mass



The model shall support:



* Forced airflow

* Zero-flow stagnation

* Variable solar irradiance

* Variable ambient temperature

* Variable wind speed

* Adjustable tilt

* Adjustable azimuth

* Thermal inertia

* Heat losses to ambient

* Configurable collector efficiency



The first implementation shall not require:



* Computational fluid dynamics

* Detailed two-dimensional conduction

* Detailed optical ray tracing

* Local absorber-temperature distribution

* Detailed natural-convection circulation

* Moisture storage in construction materials

* Structural deformation



---



# 3. Architectural Placement



The generic collector model shall be implemented in:



```text

ThermoCore.Core

```



Recommended namespace:



```csharp

ThermoCore.Core.Components.SolarThermal

```



AWG-specific collector configuration shall be implemented in:



```text

ThermoCore.AWG

```



The generic collector shall not know:



* Whether silica gel is downstream

* Whether a Peltier module is present

* Whether the device produces water

* Which operating mode the AWG controller selected



---



# 4. Component Classification



The solar air collector is:



```text

Energy-conversion component

Moist-air transport component

Thermal-storage component

Pressure-loss component

```



It converts:



```text

Solar radiation

&#x20;       ↓

Thermal energy

&#x20;       ↓

Moist-air enthalpy

```



Part of the absorbed energy is lost to:



```text

Ambient convection

Long-wave radiation

Back and side conduction

Air leakage

Thermal storage

```



---



# 5. Ports



Recommended ports:



```text

MoistAirIn

MoistAirOut

SolarRadiationIn

AmbientHeatOut

OptionalControlIn

```



Optional diagnostic ports:



```text

AbsorberTemperatureMeasurement

CoverTemperatureMeasurement

PressureDropMeasurement

```



---



# 6. Internal State



The collector shall be stateful when dynamic thermal mass is enabled.



Recommended internal state:



```csharp

public sealed record SolarCollectorState

{

&#x20;   public required double AbsorberTemperatureK { get; init; }



&#x20;   public required double CoverTemperatureK { get; init; }



&#x20;   public required double InternalAirTemperatureK { get; init; }



&#x20;   public required double StoredThermalEnergyJ { get; init; }



&#x20;   public required double LastUsefulThermalPowerW { get; init; }



&#x20;   public required double LastThermalEfficiencyFraction { get; init; }

}

```



A simplified fidelity level may use only absorber temperature.



---



# 7. Configuration Model



Recommended configuration:



```csharp

public sealed record SolarAirCollectorParameters

{

&#x20;   public required double ApertureAreaM2 { get; init; }



&#x20;   public required double AbsorberAreaM2 { get; init; }



&#x20;   public required double AirChannelCrossSectionM2 { get; init; }



&#x20;   public required double AirChannelLengthM { get; init; }



&#x20;   public required double HydraulicDiameterM { get; init; }



&#x20;   public required double OpticalEfficiencyFraction { get; init; }



&#x20;   public required double AbsorberSolarAbsorptanceFraction { get; init; }



&#x20;   public required double CoverSolarTransmittanceFraction { get; init; }



&#x20;   public required double AbsorberEmissivityFraction { get; init; }



&#x20;   public required double OverallLossCoefficientWPerM2K { get; init; }



&#x20;   public required double InternalHeatTransferCoefficientWPerM2K { get; init; }



&#x20;   public required double EffectiveThermalCapacityJPerK { get; init; }



&#x20;   public required double ReferencePressureDropPa { get; init; }



&#x20;   public required double ReferenceVolumetricFlowM3PerSecond { get; init; }



&#x20;   public double TiltAngleRadians { get; init; }



&#x20;   public double AzimuthAngleRadians { get; init; }



&#x20;   public double InternalFinAreaMultiplier { get; init; } = 1.0;



&#x20;   public double AirLeakageFraction { get; init; } = 0.0;



&#x20;   public double MinimumOperatingMassFlowKgPerSecond { get; init; }



&#x20;   public double MaximumAllowedAbsorberTemperatureK { get; init; }

}

```



---



# 8. Required Environmental Inputs



The collector shall receive or derive:



```text

Ambient-air temperature

Ambient pressure

Ambient wind speed

Global solar irradiance

Direct solar irradiance

Diffuse solar irradiance

Solar incidence angle

Sky temperature when radiative losses are modelled

Ground temperature when ground-reflected radiation is modelled

```



The minimum first-version input set is:



```text

Ambient temperature

Solar irradiance on collector plane

Wind speed

Inlet moist-air state

```



---



# 9. Solar Irradiance on Collector Plane



The preferred external weather or solar-position service shall calculate irradiance on the collector plane.



The collector shall accept:



[

G_{poa}

]



where:



* (G_{poa}) is total plane-of-array irradiance

* Unit: W/m²



If only horizontal irradiance and incidence angle are available, a simplified model may use:



[

G_{poa}

=======



G_{horizontal}K_\theta

]



This approximation shall not be treated as a detailed solar-transposition model.



---



# 10. Incidence-Angle Modifier



The incidence-angle modifier represents optical losses at non-normal incidence:



[

0 \leq K_\theta \leq 1

]



A simple approximation may use:



[

K_\theta

========



\max(0,\cos\theta)

]



where:



* (\theta) is the angle between incoming solar rays and surface normal



A more detailed empirical model may use:



[

K_\theta

========



1-b_0

\left(

\frac{1}{\cos\theta}-1

\right)

]



with the result limited to the valid physical range.



The active formula shall be configurable.



---



# 11. Incident Solar Power



Incident solar power on the collector aperture:



[

P_{solar,incident}

==================



G_{poa}A_{aperture}

]



where:



* (G_{poa}) is W/m²

* (A_{aperture}) is m²

* Result is W



---



# 12. Absorbed Solar Power



A transparent-cover collector may calculate absorbed power as:



[

P_{solar,absorbed}

==================



G_{poa}

A_{absorber}

\tau_{cover}

\alpha_{absorber}

K_\theta

]



where:



* (\tau_{cover}) is cover transmittance

* (\alpha_{absorber}) is absorber solar absorptance



A simplified model may use optical efficiency:



[

P_{solar,absorbed}

==================



G_{poa}

A_{aperture}

\eta_{optical}

K_\theta

]



The implementation shall use one model at a time and shall not multiply both formulations together accidentally.



---



# 13. Optical Efficiency



Optical efficiency shall satisfy:



[

0\leq\eta_{optical}\leq1

]



It may include:



* Cover transmittance

* Absorber absorptance

* Reflection loss

* Frame shading

* Internal obstruction

* Soiling

* Incidence-angle effects, when not modelled separately



The parameter definition shall state which losses are already included.



---



# 14. Collector Energy Balance



The general dynamic absorber energy balance is:



[

C_{collector}

\frac{dT_{abs}}{dt}

===================



## P_{solar,absorbed}



## Q_{air}



Q_{loss}

]



where:



* (C_{collector}) is effective thermal capacity in J/K

* (T_{abs}) is absorber temperature

* (Q_{air}) is heat transferred to airflow

* (Q_{loss}) is heat lost to environment



The timestep-integrated balance is:



[

\Delta E_{stored}

=================



\left(

P_{solar,absorbed}

------------------



## Q_{air}



Q_{loss}

\right)\Delta t

]



---



# 15. Thermal Capacity



Effective collector thermal capacity:



[

C_{collector}

=============



\sum_i m_i c_{p,i}

]



It may include:



* Absorber plate

* Internal fins

* Cover

* Frame

* A fraction of insulation

* Internal air



The first implementation may use one calibrated effective value.



---



# 16. Heat Transfer to Air



A lumped heat-transfer model may use:



[

Q_{air}

=======



h_{air}A_{effective}

\left(

T_{abs}-T_{air,mean}

\right)

]



where:



[

T_{air,mean}

\approx

\frac{T_{in}+T_{out}}{2}

]



Effective heat-transfer area:



[

A_{effective}

=============



A_{absorber}

M_{fin}

]



where (M_{fin}) is the internal fin-area multiplier.



---



# 17. Useful Thermal Power



Useful thermal power delivered to the airflow is:



[

Q_{useful}

==========



\dot m_{da}

\left(

h_{out}-h_{in}

\right)

]



For sensible heating without moisture transfer:



[

W_{out}=W_{in}

]



Therefore, an approximate sensible formulation is:



[

Q_{useful}

\approx

\dot m_{ma}c_{p,ma}

(T_{out}-T_{in})

]



The enthalpy-based formulation is preferred.



---



# 18. Collector Outlet Temperature



If useful thermal power is known:



[

h_{out}

=======



h_{in}

+

\frac{Q_{useful}}{\dot m_{da}}

]



Because humidity ratio remains unchanged:



[

W_{out}=W_{in}

]



The outlet temperature shall be calculated from:



[

T_{out}

=======



T(h_{out},W_{in})

]



using the psychrometric calculator.



---



# 19. Direct Outlet-Temperature Approximation



A simple first-fidelity model may calculate:



[

T_{out}

=======



T_{in}

+

\frac{

Q_{useful}

}{

\dot m_{ma}c_{p,ma}

}

]



This approximation is acceptable when:



* No condensation occurs

* No water is added

* Heat capacities are treated as constant

* Pressure change is small



---



# 20. Air Heat-Capacity Rate



Dry-air-based heat-capacity rate:



[

C_{air}

=======



\dot m_{da}

\left(

c_{p,da}

+

Wc_{p,v}

\right)

]



Unit:



```text

W/K

```



Then:



[

T_{out}

=======



T_{in}

+

\frac{Q_{useful}}{C_{air}}

]



---



# 21. Heat-Exchanger Effectiveness Formulation



The absorber-to-air transfer may alternatively use effectiveness:



[

\varepsilon_{abs-air}

=====================



1-

\exp

\left(

-\frac{UA_{abs-air}}{C_{air}}

\right)

]



Then:



[

Q_{air}

=======



\varepsilon_{abs-air}

C_{air}

(T_{abs}-T_{in})

]



and:



[

T_{out}

=======



T_{in}

+

\varepsilon_{abs-air}

(T_{abs}-T_{in})

]



This formulation is recommended for the dynamic collector model because it prevents outlet air from exceeding absorber temperature.



---



# 22. Overall Collector Heat Loss



A simplified loss model:



[

Q_{loss}

========



U_L A_{loss}

(T_{abs}-T_{ambient})

]



where:



* (U_L) is overall loss coefficient

* (A_{loss}) is effective loss area



If aperture and loss area are treated as equal:



[

Q_{loss}

========



U_L A_{aperture}

(T_{abs}-T_{ambient})

]



---



# 23. Detailed Loss Breakdown



Higher-fidelity models may calculate:



[

Q_{loss}

========



Q_{top}

+

Q_{back}

+

Q_{edge}

+

Q_{leak}

]



where:



```text

Q_top  = top convection and radiation

Q_back = conduction through back insulation

Q_edge = side and frame losses

Q_leak = enthalpy loss through air leakage

```



---



# 24. Top Convective Loss



A simple wind-dependent coefficient:



[

h_{wind}

========



a+bv_{wind}

]



Then:



[

Q_{conv,top}

============



h_{wind}A_{cover}

(T_{cover}-T_{ambient})

]



The values of (a) and (b) shall be documented and calibrated for the collector geometry.



---



# 25. Long-Wave Radiative Loss



Net radiation from cover or absorber to sky:



[

Q_{rad}

=======



\varepsilon

\sigma A

\left(

T_s^4-T_{sky}^4

\right)

]



All temperatures shall be in kelvin.



A simplified implementation may include radiative loss inside an effective overall loss coefficient.



---



# 26. Back Insulation Loss



[

Q_{back}

========



\frac{k_{ins}A_{back}}{L_{ins}}

(T_{back}-T_{ambient})

]



Equivalent resistance:



[

R_{back}

========



\frac{L_{ins}}{k_{ins}A_{back}}

]



Therefore:



[

Q_{back}

========



\frac{T_{back}-T_{ambient}}{R_{back}}

]



---



# 27. Side Loss



A simplified side-loss model:



[

Q_{edge}

========



U_{edge}

A_{edge}

(T_{abs}-T_{ambient})

]



This may initially be combined with the overall loss coefficient.



---



# 28. Air-Leakage Loss



If a fraction (f_{leak}) of airflow leaks out:



[

\dot m_{da,leak}

================



f_{leak}\dot m_{da,in}

]



The leaked enthalpy flow is:



[

Q_{leak}

========



\dot m_{da,leak}

(h_{internal}-h_{ambient})

]



Dry-air and water-vapor mass losses shall also be reported.



The first model may set:



[

f_{leak}=0

]



---



# 29. Steady-State Collector Model



When thermal storage is neglected:



[

P_{solar,absorbed}

==================



Q_{useful}

+

Q_{loss}

]



Useful power:



[

Q_{useful}

==========



## P_{solar,absorbed}



Q_{loss}

]



with:



[

Q_{useful}\geq0

]



unless the collector is permitted to cool the air at night.



---



# 30. Dynamic Collector Model



Explicit Euler update:



[

T_{abs,n+1}

===========



T_{abs,n}

+

\frac{

P_{solar,absorbed,n}

--------------------



## Q_{air,n}



Q_{loss,n}

}{

C_{collector}

}

\Delta t

]



The engine shall verify timestep stability.



A large timestep may cause:



* Temperature overshoot

* Negative temperatures

* Energy imbalance

* Oscillation



Internal substeps may be required.



---



# 31. Semi-Implicit Dynamic Update



A semi-implicit model may improve stability:



[

C

\frac{T_{n+1}-T_n}{\Delta t}

============================



## P_{solar}



## UA_{air}(T_{n+1}-T_{air})



UA_{loss}(T_{n+1}-T_{amb})

]



Solving:



[

T_{n+1}

=======



\frac{

CT_n/\Delta t

+

P_{solar}

+

UA_{air}T_{air}

+

UA_{loss}T_{amb}

}{

C/\Delta t

+

UA_{air}

+

UA_{loss}

}

]



This formulation is recommended when the collector time constant is short relative to the simulation timestep.



---



# 32. Thermal Time Constant



Approximate collector thermal time constant:



[

\tau

====



\frac{

C_{collector}

}{

UA_{air}+UA_{loss}

}

]



Recommended timestep guideline for explicit Euler:



[

\Delta t

\leq

\frac{\tau}{10}

]



A warning shall be generated when the configured timestep is too large.



---



# 33. Collector Efficiency



Instantaneous thermal efficiency:



[

\eta_{thermal}

==============



\frac{Q_{useful}}

{G_{poa}A_{aperture}}

]



when:



[

G_{poa}A_{aperture}>0

]



Otherwise:



[

\eta_{thermal}=0

]



The efficiency may be negative during night cooling if that mode is enabled, but normal reporting should distinguish night heat loss from solar efficiency.



---



# 34. Efficiency Curve Model



A common reduced-temperature form may be used:



[

\eta

====



## \eta_0



a_1

\frac{T_m-T_a}{G}

-----------------



a_2

\frac{(T_m-T_a)^2}{G}

]



For a simple air collector, the first implementation may use:



[

\eta

====



## \eta_0



a_1

\frac{T_{in}-T_a}{G}

]



The validity range and coefficients shall be explicit.



This formulation may serve as an alternative empirical fidelity level.



---



# 35. Useful Power from Efficiency Model



[

Q_{useful}

==========



\eta_{thermal}

G_{poa}

A_{aperture}

]



The calculated efficiency shall be physically bounded according to model configuration.



For a normal passive collector:



[

0\leq\eta_{thermal}\leq1

]



Negative values may indicate night cooling but shall not be silently clamped without diagnostics.



---



# 36. Zero-Flow Stagnation



When:



[

\dot m_{da}=0

]



then:



[

Q_{useful}=0

]



The absorber temperature rises until:



[

P_{solar,absorbed}

==================



Q_{loss}

]



Steady-state stagnation approximation:



[

T_{stag}

========



T_{ambient}

+

\frac{

P_{solar,absorbed}

}{

U_LA_{loss}

}

]



This is a simplified estimate.



The collector shall report an overtemperature warning when:



[

T_{abs}



>



T_{max,allowed}

]



---



# 37. Minimum Airflow



A minimum airflow may be required to prevent overheating.



If:



[

\dot m_{da}

<

\dot m_{da,min}

]



while irradiance exceeds a configured threshold, the collector shall produce a warning.



The control system may respond by:



* Increasing fan speed

* Reducing downstream restriction

* Disabling Peltier operation

* Opening a bypass

* Entering stagnation-safe mode



---



# 38. Maximum Outlet Temperature



The collector shall enforce:



[

T_{out}\leq T_{abs}

]



within numerical tolerance.



A configured component or material limit may also enforce:



[

T_{out}\leq T_{out,max}

]



Any rejected heat shall remain in absorber storage or leave through losses.



It shall not disappear.



---



# 39. Humidity Behavior



The collector is a sensible-heating component.



Therefore:



[

W_{out}=W_{in}

]



[

\dot m_{v,out}

==============



\dot m_{v,in}

]



assuming no leakage and no internal evaporation.



Relative humidity shall be recalculated from outlet temperature.



Dew point shall remain unchanged when pressure and humidity ratio remain unchanged.



---



# 40. Condensation inside Collector



Condensation is normally not expected during solar heating.



However, during startup or night operation, internal surfaces may be below inlet dew point.



Condensation is possible when:



[

T_{surface}<T_{dp,in}

]



The first implementation may:



* Reject operation in this condition

* Produce a warning

* Delegate condensation to an explicit internal-surface condensation component



It shall not remove water implicitly.



---



# 41. Pressure Drop



The collector causes airflow resistance.



A simple quadratic model:



[

\Delta p

========



\Delta p_{ref}

\left(

\frac{\dot V}{\dot V_{ref}}

\right)^2

]



where:



* (\Delta p_{ref}) is measured or estimated pressure drop

* (\dot V_{ref}) is reference volumetric flow



This model is recommended for initial implementation.



---



# 42. Darcy–Weisbach Model



A higher-fidelity channel model may use:



[

\Delta p

========



f

\frac{L}{D_h}

\frac{\rho v^2}{2}

+

\sum K_i

\frac{\rho v^2}{2}

]



where:



* (f) is Darcy friction factor

* (L) is channel length

* (D_h) is hydraulic diameter

* (K_i) are local loss coefficients



The velocity is:



[

v

=



\frac{\dot V}{A_{channel}}

]



---



# 43. Reynolds Number



[

Re

==



\frac{\rho vD_h}{\mu}

]



The flow regime affects:



* Friction factor

* Convective heat-transfer coefficient

* Pressure drop

* Collector effectiveness



Detailed air-property and convection calculations may be added at a higher fidelity level.



---



# 44. Hydraulic Diameter



For a non-circular channel:



[

D_h

===



\frac{4A_c}{P_w}

]



where:



* (A_c) is flow cross-sectional area

* (P_w) is wetted perimeter



---



# 45. Internal Heat-Transfer Coefficient



The initial model may use a configured constant:



[

h_{air}=constant

]



A higher-fidelity model shall calculate:



[

Nu

==



\frac{h_{air}D_h}{k_{air}}

]



Therefore:



[

h_{air}

=======



\frac{Nu,k_{air}}{D_h}

]



The Nusselt correlation shall depend on:



* Flow regime

* Channel geometry

* Boundary condition

* Internal fins or roughness



---



# 46. Internal Fins and Turbulence Promoters



Internal fins may increase:



* Heat-transfer area

* Convective coefficient

* Pressure drop

* Thermal mass



The initial model may represent them using:



```text

Effective-area multiplier

Pressure-drop multiplier

Thermal-capacity increment

```



These parameters shall be independent.



Increasing heat-transfer area shall not automatically increase pressure drop unless configured.



---



# 47. Fan Interaction



The collector shall report pressure drop but shall not independently select airflow when a fan-network solver is used.



Two supported modes:



## 47.1 Prescribed-flow mode



Input airflow is known.



Collector calculates pressure drop.



## 47.2 Coupled pressure-flow mode



Collector exposes:



[

\Delta p=f(\dot V)

]



The fan and network solver determine operating flow.



The first implementation may use prescribed-flow mode.



---



# 48. Wind Influence



Wind may affect:



* Top heat loss

* Side heat loss

* Air leakage

* Cover temperature

* Net efficiency



The initial model may modify overall loss coefficient:



[

U_L

===



U_{L,0}

+

k_{wind}v_{wind}

]



The coefficient shall be configurable and documented.



---



# 49. Collector Tilt



Tilt affects:



* Solar incidence

* Natural convection

* Drainage

* Wind exposure

* Structural orientation



In the first version, tilt shall directly affect solar irradiance only through the external solar-position model.



Optional empirical corrections may adjust:



* Loss coefficient

* Internal natural convection

* Wind coefficient



---



# 50. Collector Azimuth



Azimuth affects direct solar irradiance on the collector plane.



It shall be handled by the solar-position or weather-transposition service.



The thermal collector component shall not calculate astronomical solar position in its first implementation.



---



# 51. Orientation Requirement for AWG V3



For the AWG V3 physical concept:



* Solar panel and solar collector shall be located side by side.

* Both surfaces shall use approximately the same tilt and azimuth.

* Neither surface shall shade the other under nominal operation.

* The collector shall receive direct solar radiation.

* The collector shall not be placed beneath the photovoltaic panel.

* The panel-under-air-channel concept shall be treated as photovoltaic cooling, not as a solar collector.



These requirements belong to the AWG configuration, not the generic collector equation set.



---



# 52. Interaction with Peltier Hot Side



The current AWG topology may route inlet air through:



```text

Peltier hot-side heat exchanger

&#x20;       ↓

Solar collector

```



The collector inlet state shall therefore already include heat recovered from the Peltier hot side.



The collector shall not add Peltier heat again.



This prevents energy double counting.



---



# 53. Component Evaluation Sequence



Recommended collector evaluation:



```text

1. Read inlet moist-air state.

2. Read solar-radiation state.

3. Read ambient environment.

4. Validate airflow and parameters.

5. Calculate plane-of-array irradiance input.

6. Calculate absorbed solar power.

7. Calculate absorber-to-air heat transfer.

8. Calculate environmental heat loss.

9. Update absorber temperature.

10. Calculate useful heat delivered to air.

11. Create outlet moist-air state.

12. Calculate pressure drop.

13. Calculate energy residual.

14. Return diagnostics and proposed state.

```



---



# 54. Evaluation–Commit Separation



During `Evaluate`:



* The collector shall not mutate stored state.

* It shall calculate proposed absorber temperature.

* It shall calculate proposed output-port state.

* It shall calculate balance residuals.



During `Commit`:



* The accepted absorber temperature becomes current.

* The accepted thermal-storage value becomes current.

* Diagnostics may be appended to simulation history.



---



# 55. Proposed Result Model



```csharp

public sealed record SolarCollectorStepResult

{

&#x20;   public required MoistAirState OutletAir { get; init; }



&#x20;   public required SolarCollectorState ProposedState { get; init; }



&#x20;   public required double IncidentSolarPowerW { get; init; }



&#x20;   public required double AbsorbedSolarPowerW { get; init; }



&#x20;   public required double UsefulThermalPowerW { get; init; }



&#x20;   public required double EnvironmentalHeatLossW { get; init; }



&#x20;   public required double StoredEnergyRateW { get; init; }



&#x20;   public required double PressureDropPa { get; init; }



&#x20;   public required double ThermalEfficiencyFraction { get; init; }



&#x20;   public required ConservationBalance Balance { get; init; }

}

```



---



# 56. Energy Balance Residual



For a timestep:



[

R_E

===



## E_{solar,absorbed}



## E_{air,useful}



## E_{environment,loss}



\Delta E_{collector}

]



where:



[

E=P\Delta t

]



The collector shall report both:



* Absolute residual in joules

* Relative residual



---



# 57. Dry-Air Balance



Without leakage:



[

\dot m_{da,out}

===============



\dot m_{da,in}

]



With leakage:



[

\dot m_{da,in}

==============



\dot m_{da,out}

+

\dot m_{da,leak}

]



---



# 58. Water Balance



Without leakage or condensation:



[

\dot m_{v,out}

==============



\dot m_{v,in}

]



With leakage:



[

\dot m_{v,in}

=============



\dot m_{v,out}

+

\dot m_{v,leak}

]



The collector shall never generate water vapor solely because air temperature increased.



---



# 59. Simplified Fidelity Level 0



Ideal sensible heater:



Inputs:



```text

Inlet moist-air state

Configured useful thermal power

```



Equations:



[

W_{out}=W_{in}

]



[

h_{out}

=======



h_{in}

+

\frac{Q_{configured}}{\dot m_{da}}

]



Use case:



* Testing

* Graph validation

* Early application development



---



# 60. Fidelity Level 1



Constant-efficiency collector:



[

Q_{useful}

==========



\eta

G_{poa}A

]



with configured (\eta).



Includes:



* Sensible air heating

* No thermal inertia

* Optional simple pressure drop



---



# 61. Fidelity Level 2



Dynamic lumped collector:



Includes:



* Absorber thermal mass

* Optical absorption

* Absorber-to-air heat transfer

* Overall environmental heat loss

* Dynamic absorber temperature

* Pressure-drop curve

* Wind correction



This is the recommended initial AWG engineering model.



---



# 62. Fidelity Level 3



Calibrated empirical model:



Includes:



* Measured collector efficiency curve

* Measured pressure-drop curve

* Flow-dependent heat transfer

* Wind-dependent losses

* Measured thermal inertia

* Parameter fitting from prototype data



---



# 63. Fidelity Level 4



Distributed collector model:



May include:



* Multiple air-temperature nodes

* Multiple absorber-temperature nodes

* Axial heat-transfer distribution

* Local heat-loss coefficients

* Spatial wall conduction

* Distributed pressure drop



This level is outside the first implementation scope.



---



# 64. Initial AWG Prototype Parameters



Illustrative initial parameter ranges:



| Parameter                          | Initial engineering range |

| ---------------------------------- | ------------------------: |

| Aperture area                      |                0.2–0.5 m² |

| Optical efficiency                 |                 0.65–0.85 |

| Overall loss coefficient           |             4–10 W/(m²·K) |

| Effective thermal capacity         |          5,000–30,000 J/K |

| Airflow                            |               20–120 m³/h |

| Internal heat-transfer coefficient |            10–50 W/(m²·K) |

| Reference pressure drop            |                 10–150 Pa |

| Maximum absorber temperature       |                 373–423 K |



These are placeholders for sensitivity studies and shall not be treated as validated hardware values.



---



# 65. Example Steady-State Calculation



Given:



```text

Collector aperture area: 0.35 m²

Plane irradiance: 900 W/m²

Optical efficiency: 0.75

Overall loss coefficient: 6 W/(m²·K)

Absorber temperature: 353.15 K

Ambient temperature: 298.15 K

```



Absorbed solar power:



[

P_{absorbed}

============



# 900\cdot0.35\cdot0.75



236.25\ W

]



Heat loss:



[

Q_{loss}

========



# 6\cdot0.35\cdot(353.15-298.15)



115.5\ W

]



Estimated useful thermal power:



[

Q_{useful}

==========



# 236.25-115.5



120.75\ W

]



The actual outlet temperature also depends on airflow.



---



# 66. Example Outlet-Temperature Calculation



Assume:



```text

Dry-air mass flow: 0.02 kg/s

Humidity ratio: 0.010 kg/kg dry air

Useful thermal power: 120.75 W

```



Heat-capacity rate:



[

C_{air}

=======



0.02

\left(

1006+0.010\cdot1860

\right)

]



[

C_{air}

=======



20.492\ W/K

]



Temperature increase:



[

\Delta T

========



\frac{120.75}{20.492}

\approx

5.89\ K

]



This illustrates why outlet temperature depends strongly on airflow.



A low-flow collector can reach higher outlet temperatures but delivers heat to less air.



---



# 67. Collector Optimization Objective



The optimum collector operation is not always the highest outlet temperature.



Potential optimization targets:



```text

Maximum useful thermal energy

Maximum silica-gel desorption rate

Maximum daily condensed water

Minimum fan energy per liter

Maximum water recovery fraction

Maximum system energy efficiency

```



The AWG optimizer shall therefore evaluate the full system, not only collector temperature.



---



# 68. Invalid Configuration Rules



Reject configuration when:



* Aperture area is non-positive

* Absorber area is non-positive

* Optical efficiency is outside 0–1

* Absorptance is outside 0–1

* Transmittance is outside 0–1

* Heat-transfer coefficient is negative

* Thermal capacity is negative

* Hydraulic diameter is non-positive

* Reference flow is non-positive when pressure-drop model is enabled

* Maximum temperature is below ambient operating range

* Tilt or azimuth is non-finite

* Any required numeric input is NaN or infinite



---



# 69. Runtime Diagnostics



Recommended warnings:



```text

Collector airflow below minimum

Collector approaching stagnation

Absorber overtemperature

Outlet temperature exceeds absorber temperature

Negative useful heat during solar operation

Collector pressure drop exceeds fan capability

Thermal timestep may be unstable

Solar irradiance outside configured range

Condensation risk inside collector

Energy balance residual above tolerance

Air leakage causes significant water loss

```



---



# 70. Required Unit Tests



## SC-001 Zero irradiance



Expected:



* No absorbed solar power

* No solar useful power

* Collector may cool toward ambient

* Energy balance remains valid



## SC-002 Zero airflow



Expected:



* Useful airflow heating is zero

* Absorber temperature rises toward stagnation

* Overtemperature warning may occur



## SC-003 Sensible heating



Expected:



* Outlet humidity ratio equals inlet humidity ratio

* Dew point remains unchanged

* Outlet temperature increases

* Relative humidity decreases



## SC-004 Energy conservation



Expected:



[

E_{absorbed}

============



E_{air}

+

E_{loss}

+

\Delta E_{stored}

]



within tolerance.



## SC-005 Increased airflow



For equal absorbed power:



* Outlet temperature rise decreases

* Useful thermal power may increase or remain limited by transfer

* Pressure drop increases



## SC-006 Increased wind



Expected:



* Environmental loss increases

* Absorber temperature decreases

* Useful power normally decreases



## SC-007 Incidence-angle effect



Expected:



* Absorbed power decreases as incidence angle becomes unfavorable



## SC-008 Outlet-state consistency



Expected:



* All psychrometric properties are internally consistent



## SC-009 Pressure-drop scaling



For quadratic model:



* Doubling volumetric flow produces approximately four times pressure drop



## SC-010 Stagnation limit



Expected:



* Collector does not exceed configured physical limit without warning



---



# 71. Integration Tests



## SC-INT-001 Fan and collector



Given a fan airflow:



* Collector receives correct mass flow

* Collector returns pressure drop

* Fan electrical demand is evaluated separately



## SC-INT-002 Peltier hot side and collector



Expected:



* Peltier heat raises collector inlet enthalpy

* Collector adds only solar-derived heat

* No energy is double-counted



## SC-INT-003 Collector and silica gel



Expected:



* Collector outlet becomes silica-gel inlet

* Humidity ratio remains unchanged through collector

* Silica-gel module alone changes water loading and vapor flow



## SC-INT-004 Web and desktop consistency



The same configuration shall produce identical collector results in:



```text

ThermoCore.Console

ThermoCore.Web

ThermoCore.Desktop

```



---



# 72. Web API Configuration Example



```json

{

&#x20; "apertureAreaM2": 0.35,

&#x20; "absorberAreaM2": 0.33,

&#x20; "airChannelCrossSectionM2": 0.012,

&#x20; "airChannelLengthM": 0.8,

&#x20; "hydraulicDiameterM": 0.04,

&#x20; "opticalEfficiencyFraction": 0.75,

&#x20; "absorberSolarAbsorptanceFraction": 0.95,

&#x20; "coverSolarTransmittanceFraction": 0.88,

&#x20; "absorberEmissivityFraction": 0.90,

&#x20; "overallLossCoefficientWPerM2K": 6.0,

&#x20; "internalHeatTransferCoefficientWPerM2K": 25.0,

&#x20; "effectiveThermalCapacityJPerK": 12000.0,

&#x20; "referencePressureDropPa": 45.0,

&#x20; "referenceVolumetricFlowM3PerSecond": 0.02,

&#x20; "tiltAngleDegrees": 35.0,

&#x20; "azimuthAngleDegrees": 180.0,

&#x20; "internalFinAreaMultiplier": 2.0,

&#x20; "airLeakageFraction": 0.0,

&#x20; "maximumAllowedAbsorberTemperatureC": 120.0

}

```



The API layer shall convert degrees and Celsius to SI Core values.



---



# 73. Recommended C# Interface



```csharp

public interface ISolarAirCollectorModel

{

&#x20;   SolarCollectorStepResult Evaluate(

&#x20;       MoistAirState inletAir,

&#x20;       SolarRadiationState solarRadiation,

&#x20;       EnvironmentState environment,

&#x20;       SolarCollectorState currentState,

&#x20;       SolarAirCollectorParameters parameters,

&#x20;       TimeSpan timeStep);

}

```



---



# 74. Determinism and Thread Safety



The collector model shall:



* Be deterministic

* Avoid mutable static state

* Avoid dependence on system clock

* Avoid UI dependencies

* Support parallel scenario execution

* Return immutable results

* Use only supplied context and parameters



---



# 75. Acceptance Criteria



The solar collector module is accepted when:



1. It conserves dry air.

2. It conserves water vapor during sensible heating.

3. It conserves energy within configured tolerance.

4. It calculates outlet air from enthalpy rather than assigning temperature arbitrarily.

5. It accounts for solar input, heat loss and thermal storage.

6. It supports zero-flow stagnation.

7. It reports pressure drop.

8. It exposes no AWG-specific logic.

9. It supports at least fidelity levels 0–2.

10. It produces identical results in console, desktop and web hosts.

11. It detects unsafe absorber temperatures.

12. It never increases dew point through sensible heating alone.



---



# 76. Relationship to Other Documents



Psychrometric state calculations:



```text

05_Psychrometrics.md

```



General conservation equations:



```text

04_MathematicalModel.md

```



Photovoltaic model:



```text

07_SolarPanel.md

```



Peltier hot-side heat:



```text

08_Peltier.md

```



Silica-gel regeneration:



```text

09_SilicaGel.md

```



Pressure-flow solution:



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



AWG V3 physical topology:



```text

Modules/AWG/AWG_V3_SystemDesign.md

```



---



# 77. Final Solar Collector Principle



The collector shall convert explicitly measured or configured solar input into:



```text

Useful airflow heating

Environmental heat loss

Thermal-energy storage

```



The complete energy balance shall always satisfy:



[

E_{solar,absorbed}

------------------



## E_{air,useful}



## E_{environment,loss}



# \Delta E_{stored}



R_E

]



where the residual shall approach zero within numerical tolerance.



The collector shall increase air temperature and enthalpy, but shall not create water vapor or increase dew point unless an explicit water-transfer process is added.



---



**End of Document**



