# ThermoCore



## 04_MathematicalModel.md



**Version:** 1.0

**Document Type:** Mathematical Model Specification

**Status:** Draft

**Applies To:** ThermoCore.Core and all application modules

**Primary implementation language:** C#

**Internal unit system:** SI



---



# 1. Purpose



This document defines the common mathematical foundation of the ThermoCore simulation framework.



It specifies:



* Authoritative state variables

* Physical quantities and symbols

* Unit conventions

* Sign conventions

* Conservation equations

* Component balance equations

* Fluid-stream equations

* Heat-transfer abstractions

* Mass-transfer abstractions

* Electrical-energy equations

* Storage equations

* Simulation-step equations

* Residual definitions

* Convergence requirements

* Numerical validity rules



This document does not define the complete detailed model of every physical component.



Detailed equations shall be specified in component-specific documents, including:



```text

05_Psychrometrics.md

06_SolarCollector.md

07_SolarPanel.md

08_Peltier.md

09_SilicaGel.md

10_Condenser.md

11_HeatRecovery.md

12_Battery.md

```



---



# 2. Mathematical Design Principles



ThermoCore shall follow the principles below.



## 2.1 Conservation-first modelling



Every component model shall be based on one or more conservation laws:



* Conservation of total mass

* Conservation of dry-air mass

* Conservation of water mass

* Conservation of energy

* Conservation of electrical energy

* Conservation of chemical species where applicable



Empirical equations may determine transfer rates or efficiencies, but shall not override conservation.



## 2.2 Authoritative state variables



A physical state shall contain a minimal set of authoritative independent variables.



Dependent values shall be calculated from the authoritative variables.



For moist air, the preferred authoritative variables are:



```text

Temperature

Pressure

Humidity ratio

Dry-air mass flow

```



The following values shall normally be derived:



```text

Relative humidity

Vapor partial pressure

Dew-point temperature

Specific enthalpy

Water-vapor mass flow

Density

Specific volume

```



This rule prevents internally inconsistent states.



## 2.3 Explicit storage



Any accumulation of mass or energy shall be represented by an explicit stateful component or internal state variable.



Mass or energy shall not accumulate inside a stateless connection.



## 2.4 Explicit losses



All energy and mass losses shall be assigned to explicit sink terms.



Examples:



* Heat loss to environment

* Air leakage

* Exhausted water vapor

* Electrical conversion loss

* Drainage loss

* Battery loss



## 2.5 No hidden correction factors



Calibration factors shall be represented as named parameters.



They shall not be embedded as unexplained numerical multipliers.



## 2.6 Separation of physics and numerics



Physical equations shall define the continuous model.



Numerical methods shall define how that model is approximated in time.



The two shall be documented separately.



---



# 3. Symbol Conventions



## 3.1 General symbols



| Symbol        | Meaning                                     |       SI unit |

| ------------- | ------------------------------------------- | ------------: |

| (t)           | Time                                        |             s |

| (\Delta t)    | Simulation timestep                         |             s |

| (T)           | Absolute temperature                        |             K |

| (T_C)         | Celsius temperature                         |            °C |

| (p)           | Absolute pressure                           |            Pa |

| (m)           | Mass                                        |            kg |

| (\dot m)      | Mass-flow rate                              |          kg/s |

| (V)           | Volume                                      |            m³ |

| (\dot V)      | Volumetric-flow rate                        |          m³/s |

| (\rho)        | Density                                     |         kg/m³ |

| (E)           | Energy                                      |             J |

| (\dot Q)      | Heat-transfer rate                          |             W |

| (P)           | Electrical or mechanical power              |             W |

| (h)           | Specific enthalpy                           |          J/kg |

| (u)           | Specific internal energy                    |          J/kg |

| (c_p)         | Specific heat capacity at constant pressure |      J/(kg·K) |

| (c_v)         | Specific heat capacity at constant volume   |      J/(kg·K) |

| (A)           | Area                                        |            m² |

| (U)           | Overall heat-transfer coefficient           |      W/(m²·K) |

| (R_{th})      | Thermal resistance                          |           K/W |

| (\eta)        | Efficiency                                  | dimensionless |

| (\varepsilon) | Effectiveness                               | dimensionless |



## 3.2 Moist-air symbols



| Symbol        | Meaning                           |             SI unit |

| ------------- | --------------------------------- | ------------------: |

| (m_{da})      | Dry-air mass                      |                  kg |

| (m_v)         | Water-vapor mass                  |                  kg |

| (m_l)         | Liquid-water mass                 |                  kg |

| (\dot m_{da}) | Dry-air mass-flow rate            |                kg/s |

| (\dot m_v)    | Water-vapor mass-flow rate        |                kg/s |

| (p_v)         | Water-vapor partial pressure      |                  Pa |

| (p_{da})      | Dry-air partial pressure          |                  Pa |

| (p_{ws})      | Saturation vapor pressure         |                  Pa |

| (\phi)        | Relative humidity fraction        |                 0–1 |

| (W)           | Humidity ratio                    | kg water/kg dry air |

| (T_{dp})      | Dew-point temperature             |                   K |

| (h_{ma})      | Moist-air enthalpy per kg dry air |        J/kg dry air |

| (v_{ma})      | Specific volume per kg dry air    |       m³/kg dry air |



## 3.3 Water-storage symbols



| Symbol     | Meaning                       |                   SI unit |

| ---------- | ----------------------------- | ------------------------: |

| (m_{ads})  | Adsorbed water mass           |                        kg |

| (m_{cond}) | Condensed-water mass          |                        kg |

| (m_{tank}) | Water mass stored in tank     |                        kg |

| (X)        | Adsorbent water loading       | kg water/kg dry adsorbent |

| (X_{eq})   | Equilibrium adsorbent loading |                     kg/kg |

| (X_{max})  | Maximum configured loading    |                     kg/kg |



## 3.4 Electrical symbols



| Symbol  | Meaning                              |       SI unit |

| ------- | ------------------------------------ | ------------: |

| (V_e)   | Electrical voltage                   |             V |

| (I_e)   | Electrical current                   |             A |

| (P_e)   | Electrical power                     |             W |

| (E_e)   | Electrical energy                    |             J |

| (SOC)   | Battery state of charge              |           0–1 |

| (Q_c)   | Peltier cold-side heat-transfer rate |             W |

| (Q_h)   | Peltier hot-side heat-transfer rate  |             W |

| (COP_c) | Cooling coefficient of performance   | dimensionless |



---



# 4. Naming Convention



Code identifiers shall use descriptive English names.



Recommended mappings:



| Symbol   | Recommended C# name               |

| -------- | --------------------------------- |

| (T)      | `TemperatureK`                    |

| (p)      | `PressurePa`                      |

| (\dot m) | `MassFlowKgPerSecond`             |

| (\dot Q) | `HeatFlowW`                       |

| (W)      | `HumidityRatioKgPerKgDryAir`      |

| (\phi)   | `RelativeHumidityFraction`        |

| (T_{dp}) | `DewPointTemperatureK`            |

| (h_{ma}) | `SpecificEnthalpyJPerKgDryAir`    |

| (X)      | `WaterLoadingKgPerKgDryAdsorbent` |

| (SOC)    | `StateOfChargeFraction`           |



Single-character variable names may be used inside short mathematical functions, but not as public API names.



---



# 5. Unit System



## 5.1 Internal units



All internal calculations shall use SI units.



Mandatory internal units:



```text

Temperature: kelvin

Pressure: pascal

Time: second

Mass: kilogram

Length: meter

Area: square meter

Volume: cubic meter

Energy: joule

Power and heat-flow rate: watt

Mass flow: kilogram per second

Volumetric flow: cubic meter per second

Relative humidity: fraction between 0 and 1

Angles: radians

```



## 5.2 Display units



The console and UI may display:



```text

Temperature: °C

Pressure: Pa, kPa or hPa

Time: seconds, minutes or hours

Mass: g or kg

Water quantity: mL or L

Airflow: m³/h

Relative humidity: %

Energy: Wh or kWh

```



Conversions shall occur only at input and output boundaries.



## 5.3 Temperature conversion



[

T_K = T_C + 273.15

]



[

T_C = T_K - 273.15

]



Temperature differences in kelvin and Celsius have the same numerical magnitude:



[

\Delta T_K = \Delta T_C

]



---



# 6. Sign Conventions



## 6.1 Mass flow



A mass-flow rate shall be positive when entering the component through an input port and positive when leaving through an output port.



The balance equation shall explicitly subtract output flows.



## 6.2 Heat flow



For a receiving component:



```text

Positive heat flow = heat entering the component

Negative heat flow = heat leaving the component

```



A connection shall transfer the same heat flow with opposite signs at its two boundaries.



## 6.3 Electrical power



```text

Positive electrical power = electrical power entering the component

Negative electrical power = electrical power produced by the component

```



For reporting, generators may also expose a positive generated-power property, but the conservation equation shall use the defined signed convention.



## 6.4 Stored quantities



A positive storage derivative means accumulation:



[

\frac{dm}{dt} > 0

]



means increasing stored mass.



[

\frac{dE}{dt} > 0

]



means increasing stored energy.



## 6.5 Component residual



The common residual convention shall be:



[

R = \text{inputs} - \text{outputs} - \text{accumulation}

]



A perfectly balanced component has:



[

R = 0

]



---



# 7. General Mass Conservation



For a control volume containing one or more material species:



[

\frac{dm_{CV}}{dt}

==================



## \sum_i \dot m_{in,i}



\sum_j \dot m_{out,j}

+

\dot m_{generation}

-------------------



\dot m_{consumption}

]



For ordinary physical components that do not perform nuclear reactions:



[

\dot m_{generation}

===================



# \dot m_{consumption}



0

]



Therefore:



[

\frac{dm_{CV}}{dt}

==================



## \sum_i \dot m_{in,i}



\sum_j \dot m_{out,j}

]



For a steady-state, non-storage component:



[

\sum_i \dot m_{in,i}

====================



\sum_j \dot m_{out,j}

]



---



# 8. Species Conservation



Each conserved material species shall have an independent balance.



For species (k):



[

\frac{dm_k}{dt}

===============



## \sum_i \dot m_{k,in,i}



\sum_j \dot m_{k,out,j}

+

\dot m_{k,phase}

]



The phase-transfer term is internal to the total system and shall cancel when all phases are included.



For water:



[

m_{water,total}

===============



m_v + m_l + m_{ads} + m_{ice}

]



In the first ThermoCore.AWG version:



[

m_{ice}=0

]



unless frost modelling is explicitly enabled.



---



# 9. Dry-Air Mass Balance



Dry air does not condense or adsorb in the initial AWG model.



For a component:



[

\frac{dm_{da,stored}}{dt}

=========================



## \sum_i \dot m_{da,in,i}



\sum_j \dot m_{da,out,j}

]



For most airflow components:



[

\frac{dm_{da,stored}}{dt}=0

]



therefore:



[

\sum_i \dot m_{da,in,i}

=======================



\sum_j \dot m_{da,out,j}

]



Any deviation shall be reported as a dry-air mass residual.



---



# 10. Water Mass Balance



For a generic component:



[

\frac{d}{dt}

\left(

m_v+m_l+m_{ads}

\right)

=======



\sum_i

\left(

\dot m_{v,in,i}

+

\dot m_{l,in,i}

\right)

-------



\sum_j

\left(

\dot m_{v,out,j}

+

\dot m_{l,out,j}

\right)

]



For a silica-gel bed:



[

\frac{dm_{ads}}{dt}

===================



## \dot m_{v,in}



## \dot m_{v,out}



\dot m_{l,out}

]



Normally:



[

\dot m_{l,out}=0

]



unless liquid carryover or deliquescence is modelled.



For a condenser:



[

\dot m_{cond}

=============



## \dot m_{v,in}



## \dot m_{v,out}



\frac{dm_{v,stored}}{dt}

]



For a quasi-steady condenser with negligible vapor storage:



[

\dot m_{cond}

=============



## \dot m_{v,in}



\dot m_{v,out}

]



---



# 11. Humidity Ratio



Humidity ratio is defined as:



[

W=

\frac{m_v}{m_{da}}

]



For a flowing moist-air stream:



[

W=

\frac{\dot m_v}{\dot m_{da}}

]



Therefore:



[

\dot m_v

========



W\dot m_{da}

]



Humidity ratio shall not be confused with:



* Relative humidity

* Specific humidity

* Absolute humidity in kg/m³



ThermoCore shall use humidity ratio as the primary moisture-composition variable for moist-air streams.



---



# 12. Total Moist-Air Mass Flow



The total moist-air mass-flow rate is:



[

\dot m_{ma}

===========



\dot m_{da}

+

\dot m_v

]



Using humidity ratio:



[

\dot m_{ma}

===========



\dot m_{da}(1+W)

]



Therefore:



[

\dot m_{da}

===========



\frac{\dot m_{ma}}{1+W}

]



---



# 13. Partial Pressures



The total pressure of moist air is:



[

p = p_{da}+p_v

]



Relative humidity is defined as:



[

\phi

====



\frac{p_v}{p_{ws}(T)}

]



Therefore:



[

p_v

===



\phi p_{ws}(T)

]



The humidity ratio shall be calculated from vapor partial pressure as:



[

W

=



\epsilon

\frac{p_v}{p-p_v}

]



where:



[

\epsilon

========



\frac{M_w}{M_{da}}

\approx 0.621945

]



The exact constant used by the implementation shall be defined in `26_Constants.md`.



ASHRAE treats humidity ratio, enthalpy, relative humidity, dew point and related properties as the standard coordinates and properties of moist-air calculations.



---



# 14. Saturation Condition



A moist-air state is unsaturated when:



[

p_v < p_{ws}(T)

]



or equivalently:



[

\phi < 1

]



It is saturated when:



[

p_v = p_{ws}(T)

]



and:



[

\phi = 1

]



A calculated state with:



[

\phi > 1

]



is physically supersaturated and shall trigger one of the following actions:



1. Condense the excess water using an explicit condensation calculation.

2. Mark the state invalid if condensation is outside the component's responsibility.

3. Permit temporary supersaturation only inside an iterative solver with diagnostics.



The saturation-pressure implementation shall be based on a documented formulation. For ordinary liquid water, IAPWS-95 is the authoritative scientific formulation, while simpler fitted equations may be used over limited temperature ranges when their validity and error are documented.



---



# 15. Dew-Point Temperature



The dew-point temperature is defined implicitly by:



[

p_{ws}(T_{dp})=p_v

]



The implementation may calculate (T_{dp}) using:



* An analytical inverse of the selected saturation-pressure approximation

* Numerical root finding

* A documented fitted formula



The calculated dew point shall satisfy:



[

\left|

p_{ws}(T_{dp})-p_v

\right|

\leq \varepsilon_p

]



where (\varepsilon_p) is the configured pressure tolerance.



For unsaturated air:



[

T_{dp} \leq T

]



At saturation:



[

T_{dp}=T

]



---



# 16. Relative Humidity after Sensible Heating



If moist air is heated without adding or removing water and pressure remains approximately constant:



[

W_{out}=W_{in}

]



and:



[

p_{v,out}=p_{v,in}

]



approximately.



The relative humidity changes because saturation pressure changes:



[

\phi_{out}

==========



\frac{p_v}{p_{ws}(T_{out})}

]



The dew point remains unchanged if:



* Water content remains unchanged

* Total pressure remains unchanged



Therefore, sensible heating alone shall not increase dew-point temperature.



---



# 17. General Energy Conservation



For an open control volume:



[

\frac{dE_{CV}}{dt}

==================



\sum_i \dot m_{in,i}

\left(

h_i+\frac{v_i^2}{2}+gz_i

\right)

-------



\sum_j \dot m_{out,j}

\left(

h_j+\frac{v_j^2}{2}+gz_j

\right)

+

\sum \dot Q

+

\sum P

]



For ThermoCore's initial lumped models:



* Kinetic-energy changes are neglected.

* Potential-energy changes are neglected.

* Shaft and electrical work are represented as signed power terms.



Therefore:



[

\frac{dE_{stored}}{dt}

======================



## \sum_i \dot m_{in,i}h_i



\sum_j \dot m_{out,j}h_j

+

\sum_k \dot Q_k

+

\sum_l P_l

]



---



# 18. Component Energy Balance



For one simulation component:



[

R_E

===



## \sum_i \dot H_{in,i}



\sum_j \dot H_{out,j}

+

\sum_k \dot Q_k

+

\sum_l P_l

----------



\frac{dE_{stored}}{dt}

]



where:



[

\dot H = \dot m h

]



For a balanced model:



[

R_E=0

]



The numerical implementation shall report:



```text

Energy input

Energy output

Stored-energy change

Energy residual

Relative energy residual

```



---



# 19. Relative Energy Residual



The relative residual shall be calculated using a protected denominator:



[

R_{E,rel}

=========



\frac{|R_E|}

{\max(E_{scale},E_{minimum})}

]



where:



[

E_{scale}

=========



\sum |\dot H_{in}|

+

\sum |\dot H_{out}|

+

\sum |\dot Q|

+

\sum |P|

+

\left|

\frac{dE_{stored}}{dt}

\right|

]



`E_minimum` prevents division by zero for inactive components.



---



# 20. Sensible Heat



For a material with approximately constant specific heat:



[

\Delta h_{sens}

===============



c_p(T_2-T_1)

]



The corresponding heat-transfer rate is:



[

\dot Q_{sens}

=============



\dot m c_p(T_2-T_1)

]



For a finite thermal mass:



[

E_{thermal}

===========



m c_p T

]



Using a reference temperature (T_{ref}):



[

E_{thermal,rel}

===============



m c_p(T-T_{ref})

]



---



# 21. Temperature-Dependent Heat Capacity



When (c_p) varies significantly with temperature:



[

\Delta h

========



\int_{T_1}^{T_2}c_p(T),dT

]



The implementation may use:



* Analytical integration of a polynomial correlation

* Numerical integration

* Tabulated-property interpolation

* Constant-property approximation within a documented range



NIST publishes temperature-dependent heat-capacity correlations for water and other substances; detailed water-property implementations may also use IAPWS formulations.



---



# 22. Latent Heat and Phase Change



For condensation:



[

\dot Q_{latent}

===============



\dot m_{cond} h_{fg}(T)

]



where:



* (h_{fg}) is the latent heat of vaporization or condensation.

* The sign depends on the component energy convention.



From the condenser's perspective, condensation releases heat that must be removed:



[

\dot Q_{remove}

===============



\dot Q_{sens}

+

\dot m_{cond}h_{fg}

]



The latent heat shall preferably be temperature dependent.



A fixed approximation may be used only if:



* Its value is named.

* Its reference temperature is documented.

* Its valid temperature range is documented.

* Its effect on results is covered by sensitivity analysis.



---



# 23. Thermal Storage



For a lumped component with temperature-dependent total heat capacity (C(T)):



[

\frac{dE_{stored}}{dt}

======================



C(T)\frac{dT}{dt}

]



For constant heat capacity:



[

C=mc_p

]



and:



[

\frac{dT}{dt}

=============



\frac{\dot Q_{net}}{mc_p}

]



Explicit Euler update:



[

T_{n+1}

=======



T_n

+

\frac{\dot Q_{net,n}}{mc_p}\Delta t

]



Higher-order methods may replace this update without changing the physical model.



---



# 24. Conductive Heat Transfer



One-dimensional conductive heat transfer may be represented as:



[

\dot Q

======



\frac{kA}{L}

(T_{hot}-T_{cold})

]



The equivalent thermal resistance is:



[

R_{cond}

========



\frac{L}{kA}

]



Therefore:



[

\dot Q

======



\frac{T_{hot}-T_{cold}}{R_{cond}}

]



---



# 25. Convective Heat Transfer



Convective heat transfer is:



[

\dot Q

======



h_c A

(T_s-T_f)

]



where:



* (h_c) is the convective heat-transfer coefficient.

* (T_s) is surface temperature.

* (T_f) is bulk-fluid temperature.



The convective thermal resistance is:



[

R_{conv}

========



\frac{1}{h_cA}

]



Therefore:



[

\dot Q

======



\frac{T_s-T_f}{R_{conv}}

]



Detailed convection correlations shall be defined in the relevant component document.



---



# 26. Overall Heat Transfer



For multiple thermal resistances in series:



[

R_{total}

=========



\sum_i R_i

]



and:



[

\dot Q

======



\frac{\Delta T}{R_{total}}

]



Using an overall heat-transfer coefficient:



[

\dot Q

======



UA\Delta T

]



where:



[

UA

==



\frac{1}{R_{total}}

]



---



# 27. Environmental Heat Loss



A first-order environmental loss model may use:



[

\dot Q_{loss}

=============



UA_{env}

(T_{component}-T_{ambient})

]



For outdoor components, a more detailed model may include:



[

\dot Q_{loss,total}

===================



\dot Q_{conv}

+

\dot Q_{rad}

+

\dot Q_{cond}

]



The initial implementation may combine these into an effective (UA) value.



---



# 28. Radiative Heat Transfer



Net long-wave radiative heat transfer between a surface and a large surrounding environment may be represented as:



[

\dot Q_{rad}

============



\varepsilon_s \sigma A

\left(

T_s^4-T_{sur}^4

\right)

]



where:



* (\varepsilon_s) is surface emissivity.

* (\sigma) is the Stefan–Boltzmann constant.

* (T_s) and (T_{sur}) are absolute temperatures.



Solar short-wave absorption shall be modelled separately from long-wave thermal radiation.



---



# 29. Solar-Energy Input



Incident solar power on a surface is:



[

P_{solar,incident}

==================



G A_{proj}

]



where:



* (G) is irradiance.

* (A_{proj}) is effective projected area.



With an incidence-angle modifier:



[

P_{solar,incident}

==================



G A K_{\theta}

]



Absorbed solar power is:



[

P_{solar,absorbed}

==================



\alpha G A K_{\theta}

]



where (\alpha) is solar absorptance.



---



# 30. Generic Conversion Efficiency



For an energy-conversion component:



[

P_{useful}

==========



\eta P_{input}

]



Loss power is:



[

P_{loss}

========



(1-\eta)P_{input}

]



The complete balance is:



[

P_{input}

=========



P_{useful}

+

P_{loss}

]



Efficiency shall normally satisfy:



[

0\leq\eta\leq1

]



unless a value is explicitly not an efficiency, such as coefficient of performance.



---



# 31. Coefficient of Performance



For a cooling device:



[

COP_c

=====



\frac{Q_c}{P_e}

]



For a heating device:



[

COP_h

=====



\frac{Q_h}{P_e}

]



Energy conservation requires:



[

Q_h

===



Q_c+P_e

]



For a Peltier module, the detailed values of (Q_c), (Q_h), current, thermal conductance and Seebeck coefficient shall be defined in `08_Peltier.md`. Standard thermoelectric models treat the module as a coupled electrical and thermal device rather than a fixed-temperature source.



---



# 32. Generic Mass Transfer



A generic mass-transfer rate may be represented as:



[

\dot m_{transfer}

=================



k_m A

(C_{bulk}-C_{surface})

]



or using vapor pressure:



[

\dot m_{transfer}

=================



K_p A

(p_{v,bulk}-p_{v,surface})

]



For adsorption:



[

\frac{dX}{dt}

=============



k_{ads}(X_{eq}-X)

]



For desorption:



[

\frac{dX}{dt}

=============



-k_{des}(X-X_{eq})

]



These equations are placeholders for a linear-driving-force model.



The final silica-gel equations, equilibrium isotherm and temperature dependence shall be specified in `09_SilicaGel.md`.



---



# 33. Adsorbent Water Storage



For dry adsorbent mass (m_{ads,dry}):



[

m_{adsorbed\ water}

===================



X m_{ads,dry}

]



Therefore:



[

\frac{dm_{ads}}{dt}

===================



m_{ads,dry}\frac{dX}{dt}

]



The outgoing air-water balance is:



[

\dot m_{v,out}

==============



## \dot m_{v,in}



\frac{dm_{ads}}{dt}

]



During desorption:



[

\frac{dm_{ads}}{dt}<0

]



therefore:



[

\dot m_{v,out}>\dot m_{v,in}

]



---



# 34. Heat of Adsorption



Adsorption or desorption may exchange heat with the adsorbent bed.



A general formulation is:



[

\dot Q_{ads}

============



\Delta h_{ads}

\frac{dm_{ads}}{dt}

]



The sign convention shall be documented carefully.



If adsorption is exothermic:



* Increasing adsorbed mass releases heat.

* Desorption requires heat input.



The silica-gel component shall include this term in its energy balance.



---



# 35. Condensation Criterion



Condensation is thermodynamically possible when the air contacts a surface satisfying:



[

T_s<T_{dp,in}

]



Equivalent vapor-pressure criterion:



[

p_{v,in}>p_{ws}(T_s)

]



The maximum equilibrium outlet humidity ratio at surface temperature is:



[

W_{sat,s}

=========



\epsilon

\frac{p_{ws}(T_s)}

{p-p_{ws}(T_s)}

]



The theoretical maximum condensable water rate is:



[

\dot m_{cond,max}

=================



\dot m_{da}

\max

\left(

0,

W_{in}-W_{sat,s}

\right)

]



Actual condensation shall also be limited by:



* Available cooling power

* Heat-transfer coefficient

* Mass-transfer coefficient

* Residence time

* Condenser effectiveness

* Drainage efficiency



Therefore:



[

0

\leq

\dot m_{cond}

\leq

\dot m_{cond,max}

]



---



# 36. Condenser Cooling-Power Limit



The available condenser cooling power shall satisfy:



[

Q_{available}

\geq

Q_{sensible}+Q_{latent}

]



with:



[

Q_{sensible}

============



\dot m_{ma}c_{p,ma}

(T_{in}-T_{out})

]



and:



[

Q_{latent}

==========



\dot m_{cond}h_{fg}

]



A cooling-limited condensation estimate is:



[

\dot m_{cond,power}

===================



\max

\left(

0,

\frac{

Q_{available}-Q_{sensible}

}{

h_{fg}

}

\right)

]



The final condensation rate may be selected as:



[

\dot m_{cond}

=============



\min

\left(

\dot m_{cond,max},

\dot m_{cond,power},

\dot m_{cond,transfer}

\right)

]



---



# 37. Mixer Equations



For a mixer with (N) moist-air inputs:



Dry-air mass balance:



[

\dot m_{da,out}

===============



\sum_{i=1}^{N}\dot m_{da,i}

]



Water-vapor mass balance:



[

\dot m_{v,out}

==============



\sum_{i=1}^{N}\dot m_{v,i}

]



Humidity ratio:



[

W_{out}

=======



\frac{

\sum_i\dot m_{da,i}W_i

}{

\sum_i\dot m_{da,i}

}

]



Enthalpy balance:



[

\dot m_{da,out}h_{out}

======================



\sum_i

\dot m_{da,i}h_i

+

\dot Q_{mixer}

]



For an adiabatic mixer:



[

\dot Q_{mixer}=0

]



The output temperature shall be solved from:



[

h(T_{out},W_{out})=h_{out}

]



---



# 38. Splitter Equations



For one inlet and (N) outlets with split fractions (f_i):



[

0\leq f_i\leq1

]



[

\sum_{i=1}^{N}f_i=1

]



Dry-air flow:



[

\dot m_{da,out,i}

=================



f_i\dot m_{da,in}

]



Water-vapor flow:



[

\dot m_{v,out,i}

================



f_i\dot m_{v,in}

]



For an ideal splitter:



[

T_{out,i}=T_{in}

]



[

p_{out,i}=p_{in}

]



[

W_{out,i}=W_{in}

]



[

h_{out,i}=h_{in}

]



Pressure losses may be represented by a separate duct or restriction component.



---



# 39. Heat-Exchanger Effectiveness



For a sensible heat exchanger:



[

\varepsilon

===========



\frac{\dot Q_{actual}}

{\dot Q_{max}}

]



where:



[

\dot Q_{max}

============



C_{min}

(T_{hot,in}-T_{cold,in})

]



and:



[

C

=



\dot m c_p

]



Therefore:



[

\dot Q_{actual}

===============



\varepsilon C_{min}

(T_{hot,in}-T_{cold,in})

]



The outlet temperatures follow from energy balances.



A detailed (\varepsilon)-NTU implementation shall be defined in `11_HeatRecovery.md`.



---



# 40. Recirculation Mass Balance



Let (r) be the recirculation fraction:



[

0\leq r<1

]



For condenser-outlet dry-air flow (\dot m_{da,out}):



[

\dot m_{da,recirc}

==================



r\dot m_{da,out}

]



[

\dot m_{da,exhaust}

===================



(1-r)\dot m_{da,out}

]



At steady dry-air inventory, fresh-air make-up shall satisfy:



[

\dot m_{da,fresh}

=================



\dot m_{da,exhaust}

]



unless the device intentionally changes internal dry-air inventory.



---



# 41. Recirculated Water-Vapor Balance



Water vapor returned:



[

\dot m_{v,recirc}

=================



r\dot m_{v,out}

]



Water vapor exhausted:



[

\dot m_{v,exhaust}

==================



(1-r)\dot m_{v,out}

]



The exhausted vapor represents real water loss from the AWG cycle and shall be reported separately.



Water-recovery fraction:



[

\eta_{water,recovery}

=====================



\frac{\dot m_{cond}}

{

\dot m_{released}+\dot m_{v,fresh}

}

]



The exact denominator shall be chosen based on the reporting boundary and documented in the AWG module.



---



# 42. Fan and Flow Power



A simplified fan-air power relation is:



[

P_{air}

=======



\Delta p\dot V

]



Electrical fan power is:



[

P_{fan,e}

=========



\frac{\Delta p\dot V}{\eta_{fan}}

]



Alternatively, a measured fan curve may directly define:



[

\dot V=f(\Delta p,n)

]



and:



[

P_{fan,e}=g(\Delta p,n)

]



where (n) is speed or normalized control input.



---



# 43. Pressure Drop



A generic pressure-drop model may use:



[

\Delta p

========



K\frac{\rho v^2}{2}

]



For a straight duct:



[

\Delta p

========



f\frac{L}{D_h}

\frac{\rho v^2}{2}

]



Detailed friction-factor and porous-bed equations shall be defined in component documents.



Pressure-drop calculations shall not alter mass flow unless the graph includes a fan or pressure-flow solver capable of resolving the coupled system.



---



# 44. Battery Energy Balance



Stored battery energy:



[

E_{bat}=SOC\cdot E_{capacity}

]



General battery balance:



[

\frac{dE_{bat}}{dt}

===================



## \eta_{charge}P_{charge}



## \frac{P_{discharge}}{\eta_{discharge}}



P_{selfloss}

]



Discrete update:



[

E_{bat,n+1}

===========



E_{bat,n}

+

\left[

\eta_{charge}P_{charge}

-----------------------



## \frac{P_{discharge}}{\eta_{discharge}}



P_{selfloss}

\right]\Delta t

]



Bounds:



[

0

\leq

E_{bat}

\leq

E_{capacity}

]



and:



[

0\leq SOC\leq1

]



---



# 45. Water-Tank Balance



The tank mass balance is:



[

\frac{dm_{tank}}{dt}

====================



## \sum_i\dot m_{water,in,i}



## \sum_j\dot m_{water,out,j}



## \dot m_{overflow}



## \dot m_{evap}



\dot m_{leak}

]



Initial model:



[

\dot m_{evap}=0

]



[

\dot m_{leak}=0

]



unless explicitly configured.



Tank capacity constraint:



[

0

\leq

m_{tank}

\leq

m_{tank,max}

]



Overflow:



[

\dot m_{overflow}>0

]



when the unconstrained next state exceeds capacity.



---



# 46. Generic Stateful Differential Equation



A stateful component may be represented as:



[

\frac{d\mathbf{x}}{dt}

======================



\mathbf{f}

\left(

t,

\mathbf{x},

\mathbf{u},

\mathbf{p}

\right)

]



where:



* (\mathbf{x}) is internal state.

* (\mathbf{u}) is input-port state.

* (\mathbf{p}) is the parameter vector.



Output:



[

\mathbf{y}

==========



\mathbf{g}

\left(

t,

\mathbf{x},

\mathbf{u},

\mathbf{p}

\right)

]



The numerical solver computes:



[

\mathbf{x}_{n+1}

================



\mathcal{I}

\left(

\mathbf{x}_n,

\mathbf{f},

\Delta t

\right)

]



where (\mathcal{I}) is the selected integration method.



---



# 47. Explicit Euler Method



The initial implementation may use explicit Euler for slowly changing storage states:



[

\mathbf{x}_{n+1}

================



\mathbf{x}_n

+

\Delta t,

\mathbf{f}

\left(

t_n,

\mathbf{x}_n,

\mathbf{u}_n

\right)

]



Explicit Euler shall not be used without timestep-sensitivity testing.



Modules with fast dynamics may require:



* Smaller internal substeps

* Semi-implicit update

* Runge–Kutta integration

* Algebraic steady-state approximation



Detailed solver rules shall be defined in `25_NumericalMethods.md`.



---



# 48. Algebraic Components



A stateless or quasi-steady component is represented as:



[

\mathbf{y}

==========



\mathbf{g}

\left(

\mathbf{u},

\mathbf{p}

\right)

]



Examples:



* Ideal splitter

* Ideal mixer

* Unit converter

* Static efficiency block

* Simplified solar panel

* Simplified heat exchanger



Such components do not retain state between timesteps.



---



# 49. Differential-Algebraic Nature



A ThermoCore graph may contain:



* Differential state equations

* Algebraic component equations

* Algebraic connection constraints

* Feedback loops



The complete model may therefore behave as a differential-algebraic system.



The first implementation is not required to include a general DAE solver.



Instead:



* Storage components shall use explicit state updates.

* Algebraic loops shall use fixed-point iteration.

* Components shall expose convergence values.

* The engine shall report non-convergence.



---



# 50. Fixed-Point Iteration



For a loop state (\mathbf{z}):



[

\mathbf{z}^{(k+1)}

==================



\mathbf{F}

\left(

\mathbf{z}^{(k)}

\right)

]



Convergence occurs when:



[

\left|

\mathbf{z}^{(k+1)}

------------------



\mathbf{z}^{(k)}

\right|

\leq

\boldsymbol{\varepsilon}

]



Optional relaxation:



[

\mathbf{z}^{(k+1)}_{relaxed}

============================



\lambda

\mathbf{F}

\left(

\mathbf{z}^{(k)}

\right)

+

(1-\lambda)

\mathbf{z}^{(k)}

]



where:



[

0<\lambda\leq1

]



---



# 51. Convergence Variables



The initial solver shall evaluate at least:



```text

Temperature

Pressure

Dry-air mass flow

Humidity ratio

Specific enthalpy

Liquid-water mass flow

Heat flow

Electrical power

```



Each variable shall have:



* Absolute tolerance

* Relative tolerance

* Scaling value

* Validation range



---



# 52. Generic Convergence Test



For a variable (x):



[

|x_{new}-x_{old}|

\leq

\varepsilon_{abs}

+

\varepsilon_{rel}

\max

\left(

|x_{new}|,

|x_{old}|

\right)

]



The test shall avoid using only relative tolerance near zero.



---



# 53. Proposed Initial Tolerances



Initial default values:



| Quantity         | Absolute tolerance | Relative tolerance |

| ---------------- | -----------------: | -----------------: |

| Temperature      |             0.01 K |          (10^{-6}) |

| Pressure         |               1 Pa |          (10^{-6}) |

| Mass flow        |     (10^{-7}) kg/s |          (10^{-5}) |

| Humidity ratio   |    (10^{-8}) kg/kg |          (10^{-5}) |

| Enthalpy         |             1 J/kg |          (10^{-6}) |

| Heat flow        |              0.1 W |          (10^{-5}) |

| Electrical power |              0.1 W |          (10^{-5}) |

| Stored water     |       (10^{-8}) kg |          (10^{-6}) |



These values are provisional.



They shall be validated using realistic component scales and documented in `25_NumericalMethods.md`.



---



# 54. Physical Validation Rules



All models shall enforce or validate the following.



## Temperature



[

T>0\ \text{K}

]



Configured practical model ranges may be narrower.



## Pressure



[

p>0

]



For moist air:



[

p_v<p

]



## Relative humidity



Normal valid range:



[

0\leq\phi\leq1

]



Temporary solver states above 1 may be allowed only under controlled conditions.



## Humidity ratio



[

W\geq0

]



## Mass and mass flow



[

m\geq0

]



Flow direction may be represented by signed flow in future bidirectional models, but the initial directed-port implementation shall use non-negative flow values.



## Efficiency



[

0\leq\eta\leq1

]



## Heat-exchanger effectiveness



[

0\leq\varepsilon\leq1

]



## Battery state of charge



[

0\leq SOC\leq1

]



## Adsorbent loading



[

0\leq X\leq X_{max}

]



---



# 55. Invalid-State Handling



An invalid state shall not silently propagate.



The component shall return a diagnostic containing:



* Component identifier

* Port identifier where applicable

* Invalid quantity

* Invalid value

* Expected range

* Severity

* Timestep

* Solver iteration

* Recommended action



Critical invalid states shall abort the timestep or simulation according to configuration.



---



# 56. Clamping Policy



Clamping shall not be used as a general substitute for correct physics.



Clamping may be used only when:



1. It represents a genuine physical constraint.

2. The unclamped value is retained in diagnostics.

3. The clamp creates a balance term where required.

4. The event is reported.



Examples:



* Battery SOC limited to 1, with excess charging power rejected.

* Water tank limited to capacity, with excess becoming overflow.

* Adsorbent loading limited to maximum capacity.

* Condensation limited to available water vapor.



---



# 57. Balance Residual Data Model



Each component result shall include balance information equivalent to:



```csharp

public sealed record ConservationBalance

{

&#x20;   public double DryAirMassInputKg { get; init; }



&#x20;   public double DryAirMassOutputKg { get; init; }



&#x20;   public double DryAirMassStorageChangeKg { get; init; }



&#x20;   public double DryAirMassResidualKg { get; init; }



&#x20;   public double WaterMassInputKg { get; init; }



&#x20;   public double WaterMassOutputKg { get; init; }



&#x20;   public double WaterMassStorageChangeKg { get; init; }



&#x20;   public double WaterMassResidualKg { get; init; }



&#x20;   public double EnergyInputJ { get; init; }



&#x20;   public double EnergyOutputJ { get; init; }



&#x20;   public double StoredEnergyChangeJ { get; init; }



&#x20;   public double EnergyResidualJ { get; init; }

}

```



Electrical energy may be either:



* Included in total energy with explicit conversion terms

* Also reported separately for easier diagnostics



The same electrical energy shall not be double-counted.



---



# 58. Timestep Balance Conversion



Continuous rates shall be converted to timestep quantities as:



[

\Delta m

========



\dot m\Delta t

]



[

\Delta E

========



P\Delta t

]



[

\Delta Q

========



\dot Q\Delta t

]



Balances shall preferably be reported as both:



* Instantaneous rates

* Integrated timestep quantities



---



# 59. System-Level Balance



The system balance shall be the sum of component balances.



Internal connection transfers shall cancel.



System water balance:



[

R_{water,system}

================



## m_{water,external\ in}



## m_{water,external\ out}



\Delta m_{water,stored}

]



System energy balance:



[

R_{energy,system}

=================



## E_{external\ in}



## E_{external\ out}



\Delta E_{stored}

]



A non-zero system residual indicates:



* Component imbalance

* Connection-transfer error

* Double counting

* Missing source or sink

* Numerical error



---



# 60. Simulation Result Requirements



Every timestep result shall contain enough data to reproduce and audit:



* Environmental boundary state

* All component input-port states

* All component output-port states

* Internal state before commit

* Internal state after commit

* Heat and mass transfer rates

* Electrical power flows

* Water-transfer rates

* Component residuals

* System residuals

* Solver iteration count

* Convergence status

* Diagnostics



Reduced-output modes may omit detailed storage after the model is validated.



---



# 61. Reference States



Enthalpy values depend on reference-state definitions.



ThermoCore shall use one documented reference convention consistently.



For the initial HVAC-style moist-air model, the recommended convention is:



* Dry-air enthalpy reference at (0^\circ C)

* Liquid-water enthalpy reference at (0^\circ C)

* Water-vapor enthalpy includes latent contribution relative to liquid water at the reference state



The exact coefficients and reference definitions shall be specified in `05_Psychrometrics.md`.



Absolute enthalpy values from different reference conventions shall not be mixed.



Only enthalpy differences are physically relevant to balances when a consistent reference is used.



---



# 62. Model Fidelity Levels



Each component may support multiple fidelity levels.



```text

Level 0: Ideal

Level 1: Constant-efficiency lumped model

Level 2: Temperature- and flow-dependent empirical model

Level 3: Manufacturer-data or calibrated model

Level 4: Distributed or high-resolution model

```



Every simulation result shall record the fidelity level used by each component.



---



# 63. Parameter Source Classification



Every physical parameter shall include a source classification.



```text

PhysicalConstant

PublishedCorrelation

ManufacturerData

MeasuredPrototypeData

CalibratedParameter

EngineeringEstimate

UserInput

```



Engineering estimates shall be clearly distinguishable from measured or authoritative values.



---



# 64. Uncertainty Metadata



Future versions should support uncertainty metadata:



```text

Nominal value

Minimum value

Maximum value

Standard uncertainty

Probability distribution

Source

Calibration date

```



The initial deterministic engine is not required to propagate uncertainty, but the data model should not prevent later support.



---



# 65. Sensitivity Analysis



The simulator should support repeated runs in which one or more parameters are varied.



Recommended sensitivity targets:



* Solar irradiance

* Collector area

* Collector heat-loss coefficient

* Air mass flow

* Silica-gel mass

* Silica-gel equilibrium capacity

* Desorption rate coefficient

* Peltier electrical power

* Peltier COP

* Condenser thermal resistance

* Recirculation fraction

* Heat-recovery effectiveness

* Ambient temperature

* Ambient humidity



Sensitivity analysis shall be implemented outside individual component equations.



---



# 66. Prohibited Implementations



The following implementation patterns are prohibited:



* Storing Celsius values in properties named `TemperatureK`

* Storing RH percentage values in fraction properties

* Recalculating humidity ratio from rounded relative humidity

* Independently mutating all psychrometric properties

* Silently forcing relative humidity to 100%

* Generating condensed water without a latent-heat term

* Desorbing more water than the adsorbent contains

* Producing Peltier hot-side heat lower than cold-side heat plus electrical input

* Losing exhaust vapor without reporting it

* Treating battery efficiency as greater than 1

* Mixing signed and unsigned flow conventions

* Using volumetric flow as mass flow without density conversion

* Applying constants without units or source documentation



---



# 67. Minimum Mathematical Acceptance Tests



The implementation shall pass the following model-level tests.



## MM-001 Dry-air conservation



For a stateless airflow component:



[

m_{da,in}=m_{da,out}

]



within tolerance.



## MM-002 Sensible heating



Heating moist air without water transfer shall preserve humidity ratio and dew point within tolerance.



## MM-003 Ideal mixer



The mixer shall conserve dry air, water and enthalpy.



## MM-004 Ideal splitter



The sum of all outlet dry-air and water-vapor flows shall equal inlet flows.



## MM-005 Condenser water balance



Condensed water plus outlet vapor shall equal inlet vapor.



## MM-006 Condenser energy balance



Sensible and latent heat removal shall match cooling energy within tolerance.



## MM-007 Silica-gel adsorption



Adsorbed-water increase shall equal inlet-vapor minus outlet-vapor difference.



## MM-008 Silica-gel desorption



Adsorbed-water decrease shall equal additional outlet-vapor mass.



## MM-009 Peltier energy balance



[

Q_h=Q_c+P_e

]



within tolerance.



## MM-010 Battery bounds



[

0\leq SOC\leq1

]



for every timestep.



## MM-011 Tank balance



Tank mass increase shall equal inlet water minus outflow, overflow, leakage and evaporation.



## MM-012 Recirculation balance



Exhaust plus recirculation flow shall equal splitter inlet flow.



## MM-013 System balance



All internal connection transfers shall cancel from the system-level balance.



---



# 68. Numerical Precision



All physical calculations shall use IEEE 754 double-precision floating point through the C# `double` type.



The implementation shall:



* Check `double.IsFinite`.

* Reject NaN values from committed states.

* Reject positive and negative infinity.

* Use protected denominators.

* Avoid subtracting nearly equal large quantities where possible.

* Use logarithm-safe and root-safe input ranges.

* Avoid exact floating-point equality checks.

* Use configured tolerances.



---



# 69. Determinism



A simulation shall be deterministic when:



* Input configuration is identical.

* Initial states are identical.

* Weather time series is identical.

* Numerical settings are identical.

* Component order is identical.

* No random calibration or uncertainty sampling is enabled.



Components shall not use:



* Current wall-clock time

* Unseeded random numbers

* Non-deterministic shared mutable state



---



# 70. C# Implementation Guidance



Recommended common mathematical service interfaces:



```csharp

public interface IPhysicalPropertyProvider

{

&#x20;   double GetSpecificHeatCapacity(

&#x20;       MaterialId material,

&#x20;       double temperatureK,

&#x20;       double pressurePa);



&#x20;   double GetSpecificEnthalpy(

&#x20;       MaterialId material,

&#x20;       double temperatureK,

&#x20;       double pressurePa);

}

```



```csharp

public interface IPsychrometricCalculator

{

&#x20;   MoistAirState CreateFromTemperaturePressureAndHumidityRatio(

&#x20;       double temperatureK,

&#x20;       double pressurePa,

&#x20;       double humidityRatioKgPerKgDryAir,

&#x20;       double dryAirMassFlowKgPerSecond);



&#x20;   double CalculateSaturationPressurePa(

&#x20;       double temperatureK);



&#x20;   double CalculateDewPointTemperatureK(

&#x20;       double vaporPressurePa);

}

```



```csharp

public interface IConservationValidator

{

&#x20;   BalanceValidationResult Validate(

&#x20;       ConservationBalance balance,

&#x20;       BalanceTolerance tolerance);

}

```



---



# 71. Mathematical Dependency Order



Recommended implementation order:



```text

1. Units and constants

2. Saturation vapor pressure

3. Moist-air composition

4. Dew point

5. Moist-air enthalpy

6. Moist-air state factory

7. Mass balances

8. Energy balances

9. Mixer and splitter

10. Thermal resistance

11. Storage equations

12. Condensation limits

13. Electrical balances

14. Fixed-point convergence

15. System-level residual aggregation

```



---



# 72. Relationship to ThermoCore Architecture



The mathematical model does not define graph topology.



The graph architecture determines:



* Which components are connected

* Which streams enter a component

* Which streams leave a component

* Which loops require iteration



This mathematical document determines:



* How state quantities are represented

* How balances are calculated

* How physical consistency is checked

* How storage is updated

* How residuals are measured



---



# 73. Relationship to AWG Module



ThermoCore.AWG shall apply this framework to:



* Ambient moist air

* Solar-heated airflow

* Silica-gel adsorption

* Silica-gel regeneration

* Peltier heat transfer

* Condensation

* Water collection

* Exhaust

* Recirculation

* Battery-powered operation



AWG-specific empirical parameters shall not be placed in ThermoCore.Core.



---



# 74. Initial Model Boundaries



For the first AWG implementation, the system boundary shall include:



```text

Air inside the device

Silica gel and adsorbed water

Peltier module

Solar collector

Solar panel

Fans

Battery

Condenser

Liquid-water drainage

Water tank

Recirculation path

```



External boundaries:



```text

Ambient air

Solar radiation

Environment heat sink

Exhaust air

Collected-water output

```



---



# 75. Required Follow-Up Documents



The following documents shall refine this specification:



```text

05_Psychrometrics.md

```



Defines moist-air property equations and reference conventions.



```text

06_SolarCollector.md

```



Defines solar absorption, losses, airflow heating and thermal inertia.



```text

07_SolarPanel.md

```



Defines photovoltaic power and temperature effects.



```text

08_Peltier.md

```



Defines thermoelectric equations, thermal resistances and operating limits.



```text

09_SilicaGel.md

```



Defines isotherms, adsorption and desorption kinetics, and heat of adsorption.



```text

10_Condenser.md

```



Defines coupled sensible cooling, latent cooling and condensation limits.



```text

11_HeatRecovery.md

```



Defines sensible and optional latent recovery.



```text

12_Battery.md

```



Defines electrical storage and load allocation.



```text

25_NumericalMethods.md

```



Defines integration, iteration, relaxation and convergence methods.



```text

26_Constants.md

```



Defines all shared physical constants.



```text

27_Units.md

```



Defines unit types and conversion policy.



---



# 76. References



The detailed component documents shall provide equation-level references.



Primary reference families for the initial model include:



* ASHRAE psychrometric definitions, properties and charts for moist-air calculations.

* IAPWS formulations for thermodynamic and saturation properties of ordinary water.

* NIST thermochemical and thermophysical property data for water and related materials.

* Manufacturer or coupled electrothermal equations for Peltier-device models.



---



# 77. Final Mathematical Principle



Every ThermoCore component shall satisfy:



```text

Inputs

− Outputs

− Accumulation

= Residual

```



for every applicable conserved quantity.



An empirical component model is acceptable only when:



* Its inputs and outputs are explicitly defined.

* Its validity range is documented.

* Its energy and mass balances are preserved.

* Its uncertainty and source are identifiable.

* It can be replaced without changing the simulation engine.



---



**End of Document**



