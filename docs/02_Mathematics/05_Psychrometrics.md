# ThermoCore



## 05_Psychrometrics.md



**Version:** 1.0

**Document Type:** Psychrometric Mathematical Specification

**Status:** Draft

**Applies To:** ThermoCore.Core and all modules using moist-air states

**Primary implementation language:** C#

**Internal unit system:** SI

**Required numerical type:** `double`



---



# 1. Purpose



This document defines the psychrometric calculations used by ThermoCore.



Psychrometrics describes the thermodynamic properties of moist air, treated as a mixture of:



* Dry air

* Water vapor

* Optionally suspended or condensed liquid water



This document specifies:



* The authoritative moist-air state

* Saturation vapor pressure

* Water-vapor partial pressure

* Humidity ratio

* Relative humidity

* Dew-point temperature

* Moist-air specific enthalpy

* Specific volume

* Density

* Water-vapor mass flow

* Sensible heating and cooling

* Humidification

* Dehumidification

* Condensation limits

* Mixing of moist-air streams

* Validation ranges

* Numerical algorithms

* C# API requirements

* Unit and acceptance tests



The calculations defined here shall be usable without dependencies on:



* WPF

* Blazor

* ASP.NET Core

* Windows APIs

* Browser APIs

* Database systems

* Application-specific UI code



---



# 2. Architectural Requirement



Psychrometric calculations shall be implemented in:



```text

ThermoCore.Core

```



They shall be exposed through a deterministic, stateless calculation service.



Recommended namespace:



```csharp

ThermoCore.Core.Psychrometrics

```



The same implementation shall be usable by:



```text

ThermoCore.Console

ThermoCore.Desktop

ThermoCore.Web

ThermoCore.Api

ThermoCore.AWG

ThermoCore.Tests

```



No duplicate psychrometric implementations shall exist in UI or application projects.



---



# 3. Physical Model



Moist air shall be modelled as an ideal-gas mixture of:



* Dry air

* Water vapor



The total pressure is:



[

p = p_{da} + p_v

]



where:



* (p) is total moist-air pressure

* (p_{da}) is dry-air partial pressure

* (p_v) is water-vapor partial pressure



The initial implementation assumes:



* Thermodynamic equilibrium at every air port

* Uniform bulk temperature

* Uniform bulk composition

* No suspended liquid droplets in a normal `MoistAirState`

* No frost or ice

* No chemical reactions

* Negligible pressure variation within a component unless explicitly modelled

* Ideal-gas behavior within the configured operating range



---



# 4. Authoritative Moist-Air State



The preferred independent variables are:



```text

Dry-bulb temperature

Total pressure

Humidity ratio

Dry-air mass-flow rate

```



Recommended immutable state:



```csharp

public sealed record MoistAirState

{

&#x20;   public required double TemperatureK { get; init; }



&#x20;   public required double PressurePa { get; init; }



&#x20;   public required double HumidityRatioKgPerKgDryAir { get; init; }



&#x20;   public required double DryAirMassFlowKgPerSecond { get; init; }



&#x20;   public required double VaporPressurePa { get; init; }



&#x20;   public required double RelativeHumidityFraction { get; init; }



&#x20;   public required double DewPointTemperatureK { get; init; }



&#x20;   public required double SpecificEnthalpyJPerKgDryAir { get; init; }



&#x20;   public required double SpecificVolumeM3PerKgDryAir { get; init; }



&#x20;   public required double MoistAirDensityKgPerM3 { get; init; }



&#x20;   public required double WaterVaporMassFlowKgPerSecond { get; init; }



&#x20;   public required MoistAirPhaseState PhaseState { get; init; }

}

```



Although derived values are stored for efficient reporting, clients shall not construct this record directly.



All states shall be created through a validated factory or calculator.



---



# 5. Phase-State Enumeration



Recommended enumeration:



```csharp

public enum MoistAirPhaseState

{

&#x20;   Unsaturated,

&#x20;   Saturated,

&#x20;   SupersaturatedCandidate

}

```



`SupersaturatedCandidate` means that the requested thermodynamic state requires condensation before it can be committed as a normal equilibrium moist-air state.



A supersaturated state shall not silently become a valid state through relative-humidity clamping.



---



# 6. Required State Factories



The calculator shall support state creation from at least the following independent property pairs.



## 6.1 Temperature, pressure and relative humidity



```csharp

MoistAirState CreateFromRelativeHumidity(

&#x20;   double temperatureK,

&#x20;   double pressurePa,

&#x20;   double relativeHumidityFraction,

&#x20;   double dryAirMassFlowKgPerSecond);

```



## 6.2 Temperature, pressure and humidity ratio



```csharp

MoistAirState CreateFromHumidityRatio(

&#x20;   double temperatureK,

&#x20;   double pressurePa,

&#x20;   double humidityRatioKgPerKgDryAir,

&#x20;   double dryAirMassFlowKgPerSecond);

```



## 6.3 Temperature, pressure and dew point



```csharp

MoistAirState CreateFromDewPoint(

&#x20;   double temperatureK,

&#x20;   double pressurePa,

&#x20;   double dewPointTemperatureK,

&#x20;   double dryAirMassFlowKgPerSecond);

```



## 6.4 Enthalpy, pressure and humidity ratio



This factory may be added after the initial implementation:



```csharp

MoistAirState CreateFromEnthalpyAndHumidityRatio(

&#x20;   double specificEnthalpyJPerKgDryAir,

&#x20;   double pressurePa,

&#x20;   double humidityRatioKgPerKgDryAir,

&#x20;   double dryAirMassFlowKgPerSecond);

```



---



# 7. Required Constants



The exact values shall be centralized in `26_Constants.md` and in one code location.



Initial recommended constants:



| Constant                        |     Symbol |    Initial value |

| ------------------------------- | ---------: | ---------------: |

| Dry-air gas constant            |   (R_{da}) | 287.055 J/(kg·K) |

| Water-vapor gas constant        |      (R_v) |  461.52 J/(kg·K) |

| Molecular-mass ratio            | (\epsilon) |         0.621945 |

| Dry-air heat capacity           | (c_{p,da}) |    1006 J/(kg·K) |

| Water-vapor heat capacity       |  (c_{p,v}) |    1860 J/(kg·K) |

| Liquid-water heat capacity      |  (c_{p,l}) |    4180 J/(kg·K) |

| Reference vaporization enthalpy | (h_{fg,0}) |   2,501,000 J/kg |

| Celsius offset                  |          — |         273.15 K |



These are initial engineering values.



A later high-fidelity implementation may replace constant heat capacities with temperature-dependent correlations.



---



# 8. Temperature Definitions



Dry-bulb temperature:



[

T_{db}=T

]



Celsius temperature:



[

T_C = T_K - 273.15

]



All public Core APIs shall accept and return kelvin unless explicitly named otherwise.



A method named `CalculateSaturationPressurePa` shall receive temperature in kelvin.



---



# 9. Saturation Vapor Pressure



Saturation vapor pressure is the equilibrium vapor pressure of water at a given temperature.



Symbol:



[

p_{ws}(T)

]



The initial implementation shall support liquid-water saturation for:



```text

0°C to 100°C

```



Recommended extended engineering range:



```text

-45°C to 100°C

```



Below (0^\circ C), the implementation shall clearly document whether saturation is calculated over:



* Supercooled liquid water

* Ice



The first AWG implementation may use saturation over liquid water throughout its configured non-freezing operating range.



---



# 10. Selected Initial Saturation Formula



For the first implementation, the Buck equation may be used over liquid water:



[

p_{ws}

======



611.21

\exp

\left[

\left(

18.678-\frac{T_C}{234.5}

\right)

\left(

\frac{T_C}{257.14+T_C}

\right)

\right]

]



where:



* (T_C) is temperature in degrees Celsius

* (p_{ws}) is returned in pascals



Implementation:



```csharp

public static double CalculateSaturationPressurePa(

&#x20;   double temperatureK)

{

&#x20;   double temperatureC = temperatureK - 273.15;



&#x20;   double exponent =

&#x20;       (18.678 - temperatureC / 234.5) *

&#x20;       (temperatureC / (257.14 + temperatureC));



&#x20;   return 611.21 * Math.Exp(exponent);

}

```



The implementation shall validate the configured formula range.



A future high-fidelity provider may use an IAPWS or ASHRAE saturation formulation behind the same interface.



---



# 11. Saturation-Pressure Provider Abstraction



Recommended interface:



```csharp

public interface ISaturationPressureProvider

{

&#x20;   double CalculatePressurePa(double temperatureK);



&#x20;   SaturationPressureModelInfo ModelInfo { get; }

}

```



Recommended model metadata:



```csharp

public sealed record SaturationPressureModelInfo

{

&#x20;   public required string ModelName { get; init; }



&#x20;   public required double MinimumTemperatureK { get; init; }



&#x20;   public required double MaximumTemperatureK { get; init; }



&#x20;   public required string Reference { get; init; }



&#x20;   public required string PhaseBasis { get; init; }

}

```



This allows the simulator to replace the initial approximation without changing component APIs.



---



# 12. Relative Humidity



Relative humidity is:



[

\phi

====



\frac{p_v}{p_{ws}(T)}

]



where:



[

0 \leq \phi \leq 1

]



for an equilibrium unsaturated or saturated gas state.



From temperature and relative humidity:



[

p_v

===



\phi p_{ws}(T)

]



Implementation:



```csharp

public double CalculateVaporPressurePa(

&#x20;   double temperatureK,

&#x20;   double relativeHumidityFraction)

{

&#x20;   ValidateRelativeHumidity(relativeHumidityFraction);



&#x20;   return relativeHumidityFraction *

&#x20;          saturationPressureProvider.CalculatePressurePa(

&#x20;              temperatureK);

}

```



---



# 13. Relative-Humidity Input Convention



Core APIs shall use:



```text

0.0 to 1.0

```



Examples:



```text

0.30 = 30%

0.50 = 50%

0.95 = 95%

1.00 = 100%

```



UI and JSON boundaries may expose percentages, but conversion shall be explicit.



Prohibited:



```csharp

RelativeHumidityFraction = 95;

```



Correct:



```csharp

RelativeHumidityFraction = 0.95;

```



---



# 14. Vapor Partial Pressure from Humidity Ratio



Humidity ratio is:



[

W

=



\epsilon

\frac{p_v}{p-p_v}

]



Solving for vapor pressure:



[

p_v

===



\frac{Wp}{\epsilon+W}

]



Implementation:



```csharp

public static double CalculateVaporPressurePa(

&#x20;   double pressurePa,

&#x20;   double humidityRatioKgPerKgDryAir)

{

&#x20;   return

&#x20;       humidityRatioKgPerKgDryAir * pressurePa /

&#x20;       (PsychrometricConstants.MolecularMassRatio +

&#x20;        humidityRatioKgPerKgDryAir);

}

```



Required condition:



[

0 \leq p_v < p

]



---



# 15. Humidity Ratio from Vapor Pressure



[

W

=



\epsilon

\frac{p_v}{p-p_v}

]



Implementation:



```csharp

public static double CalculateHumidityRatio(

&#x20;   double pressurePa,

&#x20;   double vaporPressurePa)

{

&#x20;   if (vaporPressurePa < 0.0 ||

&#x20;       vaporPressurePa >= pressurePa)

&#x20;   {

&#x20;       throw new PsychrometricStateException(

&#x20;           "Vapor pressure must be non-negative and lower than total pressure.");

&#x20;   }



&#x20;   return

&#x20;       PsychrometricConstants.MolecularMassRatio *

&#x20;       vaporPressurePa /

&#x20;       (pressurePa - vaporPressurePa);

}

```



---



# 16. Saturation Humidity Ratio



At saturation:



[

W_s(T,p)

========



\epsilon

\frac{p_{ws}(T)}

{p-p_{ws}(T)}

]



Required condition:



[

p_{ws}(T)<p

]



At high temperatures near the boiling point corresponding to system pressure, this condition may fail.



Such states shall be rejected or processed by a high-temperature steam model outside the initial psychrometric scope.



---



# 17. Relative Humidity from Humidity Ratio



First calculate vapor pressure:



[

p_v

===



\frac{Wp}{\epsilon+W}

]



Then:



[

\phi

====



\frac{p_v}{p_{ws}(T)}

]



Implementation shall not clamp (\phi) before validation.



If:



[

\phi > 1 + \varepsilon_{\phi}

]



the state is supersaturated.



If:



[

1 < \phi \leq 1+\varepsilon_{\phi}

]



the state may be treated as saturated within numerical tolerance.



---



# 18. Dew-Point Definition



The dew-point temperature satisfies:



[

p_{ws}(T_{dp})=p_v

]



Dew point depends on:



* Water-vapor partial pressure

* The chosen saturation-pressure formulation



Dew point does not increase when air is sensibly heated at unchanged humidity ratio and pressure.



---



# 19. Dew-Point Calculation Method



The preferred implementation shall numerically invert the active saturation-pressure provider.



This guarantees consistency between:



* Saturation-pressure calculation

* Relative-humidity calculation

* Dew-point calculation



Recommended algorithm:



1. Validate vapor pressure.

2. Select lower and upper temperature bounds.

3. Use bisection or a safeguarded Newton method.

4. Stop when pressure or temperature tolerance is reached.

5. Return temperature in kelvin.



Recommended initial bisection bounds:



```text

Lower bound: 173.15 K

Upper bound: 373.15 K

```



The bounds may be restricted by the active saturation model.



---



# 20. Dew-Point Bisection Pseudocode



```text

function CalculateDewPoint(vaporPressure):



&#x20;   validate vaporPressure > 0



&#x20;   low = model.MinimumTemperature

&#x20;   high = model.MaximumTemperature



&#x20;   validate Psat(low) <= vaporPressure <= Psat(high)



&#x20;   repeat until convergence:



&#x20;       mid = (low + high) / 2

&#x20;       pressure = Psat(mid)



&#x20;       if pressure < vaporPressure:

&#x20;           low = mid

&#x20;       else:

&#x20;           high = mid



&#x20;   return (low + high) / 2

```



---



# 21. Dew-Point Convergence



The solver shall stop when either condition is met:



[

|p_{ws}(T_{mid})-p_v|

\leq

\varepsilon_p

]



or:



[

T_{high}-T_{low}

\leq

\varepsilon_T

]



Recommended initial tolerances:



```text

Pressure tolerance: 0.1 Pa

Temperature tolerance: 0.0001 K

Maximum iterations: 100

```



Bisection shall normally converge well before the maximum iteration count.



---



# 22. Dew-Point Edge Cases



## 22.1 Zero vapor pressure



Mathematically, dew point tends toward negative infinity.



The implementation shall not return negative infinity as an ordinary temperature.



Options:



* Return `null` through a nullable API

* Return the minimum supported dew-point temperature with a diagnostic

* Reject exactly zero vapor pressure for dew-point calculation



Recommended approach:



```csharp

double? CalculateDewPointTemperatureK(

&#x20;   double vaporPressurePa);

```



Return `null` when vapor pressure is effectively zero.



## 22.2 Saturated air



When:



[

\phi=1

]



then:



[

T_{dp}=T

]



within tolerance.



## 22.3 Supersaturated candidate



A dew point above dry-bulb temperature indicates a supersaturated requested state.



The state shall be passed to a condensation resolver rather than silently committed.



---



# 23. Moist-Air Enthalpy Reference



ThermoCore shall initially use an HVAC-style moist-air enthalpy reference.



Reference state:



```text

Dry air: 0°C

Liquid water: 0°C

```



Specific enthalpy per kilogram of dry air:



[

h_{ma}

======



c_{p,da}T_C

+

W

\left(

h_{fg,0}

+

c_{p,v}T_C

\right)

]



where:



* (T_C) is in degrees Celsius

* (h_{ma}) is in J/kg dry air

* (W) is kg water/kg dry air



Using initial constants:



[

h_{ma}

======



1006T_C

+

W

\left(

2{,}501{,}000

+

1860T_C

\right)

]



---



# 24. Moist-Air Enthalpy Implementation



```csharp

public static double CalculateSpecificEnthalpyJPerKgDryAir(

&#x20;   double temperatureK,

&#x20;   double humidityRatioKgPerKgDryAir)

{

&#x20;   double temperatureC = temperatureK - 273.15;



&#x20;   return

&#x20;       PsychrometricConstants.DryAirSpecificHeatJPerKgK *

&#x20;       temperatureC

&#x20;       +

&#x20;       humidityRatioKgPerKgDryAir *

&#x20;       (

&#x20;           PsychrometricConstants.ReferenceVaporizationEnthalpyJPerKg

&#x20;           +

&#x20;           PsychrometricConstants.WaterVaporSpecificHeatJPerKgK *

&#x20;           temperatureC

&#x20;       );

}

```



The reference convention shall remain identical across:



* Mixers

* Condensers

* Silica-gel components

* Heat exchangers

* Results

* CSV exports

* Web API responses



---



# 25. Temperature from Enthalpy and Humidity Ratio



From:



[

h

=



c_{p,da}T_C

+

W(h_{fg,0}+c_{p,v}T_C)

]



solve:



[

T_C

===



\frac{

h-Wh_{fg,0}

}{

c_{p,da}+Wc_{p,v}

}

]



Then:



[

T_K=T_C+273.15

]



This calculation is required by an adiabatic mixer.



---



# 26. Specific Volume of Moist Air



Per kilogram of dry air:



[

v_{ma}

======



\frac{

R_{da}T

}{

p

}

\left(

1+

\frac{W}{\epsilon}

\right)

]



Equivalent form:



[

v_{ma}

======



\frac{

R_{da}T(1+1.607858W)

}{

p

}

]



Unit:



```text

m³/kg dry air

```



---



# 27. Moist-Air Density



The total moist-air density may be calculated from dry-air-based specific volume:



[

\rho_{ma}

=========



\frac{1+W}{v_{ma}}

]



Unit:



```text

kg moist air/m³

```



Dry-air density within the moist mixture:



[

\rho_{da}

=========



\frac{1}{v_{ma}}

]



Water-vapor density:



[

\rho_v

======



\frac{W}{v_{ma}}

]



---



# 28. Converting Volumetric Airflow to Dry-Air Mass Flow



Given actual volumetric airflow:



[

\dot V

]



dry-air mass flow is:



[

\dot m_{da}

===========



\frac{\dot V}{v_{ma}}

]



Total moist-air mass flow is:



[

\dot m_{ma}

===========



\dot m_{da}(1+W)

]



The volumetric-flow value shall correspond to the same temperature, pressure and moisture state used to calculate specific volume.



---



# 29. Water-Vapor Mass Flow



[

\dot m_v

========



W\dot m_{da}

]



This value shall be stored or derivable in each flowing `MoistAirState`.



---



# 30. Absolute Humidity



Absolute humidity is water-vapor mass per moist-air volume:



[

AH

==



\rho_v

]



Using the ideal-gas equation:



[

AH

==



\frac{p_v}{R_vT}

]



Unit:



```text

kg water vapor/m³

```



For display:



```text

g/m³ = kg/m³ × 1000

```



Absolute humidity shall not be used as the primary moisture-composition variable inside ThermoCore.



Humidity ratio is preferred because it remains convenient for dry-air mass balances.



---



# 31. Specific Humidity



Specific humidity is:



[

q

=



\frac{m_v}{m_{da}+m_v}

]



Using humidity ratio:



[

q

=



\frac{W}{1+W}

]



Inverse:



[

W

=



\frac{q}{1-q}

]



Specific humidity may be exposed for reporting but is not required as an authoritative state property.



---



# 32. Sensible Heating



For sensible heating with no water transfer:



[

W_{out}=W_{in}

]



[

\dot m_{da,out}=\dot m_{da,in}

]



[

p_{out}\approx p_{in}

]



Heat added:



[

\dot Q

======



\dot m_{da}

\left(

h_{out}-h_{in}

\right)

]



The relative humidity normally decreases because:



[

p_{ws}(T_{out})>p_{ws}(T_{in})

]



while vapor partial pressure remains approximately unchanged.



The dew point remains unchanged at constant pressure and unchanged humidity ratio.



---



# 33. Sensible Cooling without Condensation



Sensible cooling is valid while:



[

T_{out}\geq T_{dp,in}

]



and:



[

W_{out}=W_{in}

]



When the requested outlet temperature is lower than the inlet dew point, condensation shall be resolved explicitly.



---



# 34. Humidification



Water-vapor addition rate:



[

\dot m_{v,added}

================



\dot m_{da}

(W_{out}-W_{in})

]



For pure steam injection, the injected stream also carries enthalpy.



For evaporation from liquid water, the evaporation process removes latent heat from another physical body or stream.



A component shall not increase humidity ratio without accounting for:



* Added water mass

* Associated energy transfer



---



# 35. Dehumidification



Water removal rate:



[

\dot m_{water,removed}

======================



\dot m_{da}

(W_{in}-W_{out})

]



where:



[

W_{out}\leq W_{in}

]



The removed water may become:



* Liquid condensate

* Adsorbed water

* Absorbed solution

* Exhausted vapor through stream separation



The receiving phase or sink shall be explicit.



---



# 36. Theoretical Condensation State



For air contacting a surface at temperature (T_s), the equilibrium saturated humidity ratio is:



[

W_{s,surface}

=============



W_s(T_s,p)

]



Maximum thermodynamic condensation rate:



[

\dot m_{cond,max}

=================



\dot m_{da}

\max

\left(

0,

W_{in}-W_{s,surface}

\right)

]



This value assumes that outlet air reaches saturation at the surface temperature.



A real condenser will usually remove less water.



---



# 37. Bypass-Factor Condenser Model



A simple practical condenser model may use a bypass factor (BF):



[

0\leq BF\leq1

]



Outlet humidity ratio:



[

W_{out}

=======



BF,W_{in}

+

(1-BF)W_{s,surface}

]



Outlet temperature:



[

T_{out}

=======



BF,T_{in}

+

(1-BF)T_s

]



Condensed water rate:



[

\dot m_{cond}

=============



\dot m_{da}

(W_{in}-W_{out})

]



The detailed condenser document shall additionally limit condensation by cooling power.



---



# 38. Apparatus Dew Point



For the bypass-factor model, (T_s) acts as an apparatus dew-point temperature.



It is not necessarily identical to:



* Peltier cold-side ceramic temperature

* Fin-base temperature

* Average fin-surface temperature

* Collected-water temperature



The condenser component shall calculate or configure the effective apparatus dew point based on thermal resistances.



---



# 39. Saturated Outlet Validation



If the calculated outlet has:



[

\phi_{out}>1

]



the condenser algorithm is inconsistent.



The model shall:



1. Recalculate outlet state from conserved quantities.

2. Reduce outlet humidity ratio through additional condensation if cooling permits.

3. Raise a balance or convergence error if no consistent state exists.



It shall not clamp relative humidity independently.



---



# 40. Adiabatic Mixing of Two Air Streams



For two inlet streams:



Dry-air balance:



[

\dot m_{da,3}

=============



\dot m_{da,1}

+

\dot m_{da,2}

]



Water-vapor balance:



[

\dot m_{da,3}W_3

================



\dot m_{da,1}W_1

+

\dot m_{da,2}W_2

]



Therefore:



[

W_3

===



\frac{

\dot m_{da,1}W_1

+

\dot m_{da,2}W_2

}{

\dot m_{da,1}

+

\dot m_{da,2}

}

]



Enthalpy balance:



[

h_3

===



\frac{

\dot m_{da,1}h_1

+

\dot m_{da,2}h_2

}{

\dot m_{da,1}

+

\dot m_{da,2}

}

]



Temperature is then calculated from (h_3) and (W_3).



---



# 41. Mixing Supersaturation



Adiabatic mixing can theoretically produce a supersaturated combined state.



After mixing:



1. Calculate (W_3).

2. Calculate (h_3).

3. Calculate (T_3).

4. Calculate (\phi_3).



If:



[

\phi_3>1

]



a separate equilibrium-condensation calculation is required.



The mixer may either:



* Resolve condensation internally and expose liquid water

* Return a supersaturated candidate to an explicit phase-equilibrium component



The first implementation should reject supersaturated mixer output unless liquid-water output is implemented.



---



# 42. Stream Splitting



An ideal splitter preserves all intensive properties:



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



Only flow rates change.



For split fraction (f_i):



[

\dot m_{da,out,i}

=================



f_i\dot m_{da,in}

]



[

\dot m_{v,out,i}

================



f_i\dot m_{v,in}

]



---



# 43. Recirculated Moist-Air Mixing



For fresh air and recirculated air:



[

\dot m_{da,mixed}

=================



\dot m_{da,fresh}

+

\dot m_{da,recirc}

]



The mixed humidity ratio and enthalpy shall use the ordinary mixer equations.



The implementation shall calculate and report:



```text

Fresh-air water-vapor input

Recirculated water-vapor input

Mixed-stream water-vapor flow

Exhaust water-vapor loss

```



This is essential for assessing whether the AWG wastes desorbed water vapor.



---



# 44. Dew-Point Approach



For a condenser surface:



[

\Delta T_{approach}

===================



T_{surface}-T_{dp,in}

]



Interpretation:



```text

Positive value: no equilibrium condensation expected

Zero: onset of condensation

Negative value: condensation is thermodynamically possible

```



The absolute difference between hot-air temperature and condenser temperature is not by itself a condensation criterion.



---



# 45. High-Temperature Moist Air



At elevated temperatures, the saturation vapor pressure increases rapidly.



A state such as:



```text

80°C and 95% relative humidity

```



contains an extremely high water-vapor partial pressure and humidity ratio at normal atmospheric pressure.



Such a state may be physically possible only when sufficient water was added through:



* Desorption

* Evaporation

* Steam injection



Heating ambient air alone cannot create this state.



The model shall therefore conserve humidity ratio during sensible heating.



---



# 46. Silica-Gel Outlet State Requirement



The silica-gel component shall not assign a configured outlet relative humidity such as 95% without checking water and energy availability.



Instead, it shall calculate:



[

\dot m_{v,out}

==============



\dot m_{v,in}

+

\dot m_{desorbed}

-----------------



\dot m_{adsorbed}

]



Then:



[

W_{out}

=======



\frac{\dot m_{v,out}}{\dot m_{da}}

]



After the outlet temperature is known, relative humidity shall be derived from (T), (p) and (W).



This rule directly prevents physically unrealistic internal tables.



---



# 47. Moist-Air State Factory Algorithm



Recommended state-creation sequence from temperature, pressure and humidity ratio:



```text

1. Validate temperature.

2. Validate pressure.

3. Validate humidity ratio.

4. Calculate vapor pressure.

5. Calculate saturation pressure.

6. Calculate relative humidity.

7. Determine phase-state classification.

8. Calculate dew point.

9. Calculate enthalpy.

10. Calculate specific volume.

11. Calculate density.

12. Calculate vapor mass flow.

13. Construct immutable state.

```



---



# 48. State Consistency Validation



For every committed `MoistAirState`, verify:



[

p_v

===



\frac{Wp}{\epsilon+W}

]



[

\phi

====



\frac{p_v}{p_{ws}(T)}

]



[

\dot m_v

========



W\dot m_{da}

]



[

h

=



h(T,W)

]



[

v

=



v(T,p,W)

]



[

\rho

====



\frac{1+W}{v}

]



Derived values shall agree within configured tolerances.



---



# 49. Validation Ranges



Initial recommended supported ranges:



| Quantity            |             Minimum |                 Maximum |

| ------------------- | ------------------: | ----------------------: |

| Temperature         |            228.15 K |                373.15 K |

| Celsius temperature |               −45°C |                   100°C |

| Pressure            |           50,000 Pa |              120,000 Pa |

| Relative humidity   |                   0 |                       1 |

| Humidity ratio      |             0 kg/kg |                 1 kg/kg |

| Dry-air mass flow   |              0 kg/s | configuration dependent |

| Enthalpy            | calculated validity |     calculated validity |



The AWG module may use a narrower validated operating envelope.



---



# 50. Numerical Stability



The implementation shall guard against:



* Division by (p-p_v) approaching zero

* Saturation pressure greater than total pressure

* Exponential overflow

* Logarithm of zero

* Negative humidity ratio

* Negative pressure

* Temperature below absolute zero

* NaN input

* Infinite input

* Non-convergent dew-point inversion



All public calculator methods shall validate finite inputs.



---



# 51. Tolerance Model



Recommended configuration:



```csharp

public sealed record PsychrometricTolerances

{

&#x20;   public double TemperatureK { get; init; } = 1e-4;



&#x20;   public double PressurePa { get; init; } = 0.1;



&#x20;   public double RelativeHumidityFraction { get; init; } = 1e-8;



&#x20;   public double HumidityRatioKgPerKgDryAir { get; init; } = 1e-10;



&#x20;   public int MaximumRootIterations { get; init; } = 100;

}

```



Exact values shall later be harmonized with `25_NumericalMethods.md`.



---



# 52. Error Types



Recommended exceptions:



```csharp

public class PsychrometricException : Exception

{

}

```



```csharp

public sealed class PsychrometricInputException

&#x20;   : PsychrometricException

{

}

```



```csharp

public sealed class PsychrometricStateException

&#x20;   : PsychrometricException

{

}

```



```csharp

public sealed class PsychrometricConvergenceException

&#x20;   : PsychrometricException

{

}

```



Library code shall not display UI dialogs or write directly to the console.



---



# 53. Recommended Calculator Interface



```csharp

public interface IPsychrometricCalculator

{

&#x20;   MoistAirState CreateFromRelativeHumidity(

&#x20;       double temperatureK,

&#x20;       double pressurePa,

&#x20;       double relativeHumidityFraction,

&#x20;       double dryAirMassFlowKgPerSecond);



&#x20;   MoistAirState CreateFromHumidityRatio(

&#x20;       double temperatureK,

&#x20;       double pressurePa,

&#x20;       double humidityRatioKgPerKgDryAir,

&#x20;       double dryAirMassFlowKgPerSecond);



&#x20;   MoistAirState CreateFromDewPoint(

&#x20;       double temperatureK,

&#x20;       double pressurePa,

&#x20;       double dewPointTemperatureK,

&#x20;       double dryAirMassFlowKgPerSecond);



&#x20;   double CalculateSaturationPressurePa(

&#x20;       double temperatureK);



&#x20;   double CalculateHumidityRatio(

&#x20;       double pressurePa,

&#x20;       double vaporPressurePa);



&#x20;   double CalculateVaporPressurePa(

&#x20;       double pressurePa,

&#x20;       double humidityRatioKgPerKgDryAir);



&#x20;   double? CalculateDewPointTemperatureK(

&#x20;       double vaporPressurePa);



&#x20;   double CalculateSpecificEnthalpyJPerKgDryAir(

&#x20;       double temperatureK,

&#x20;       double humidityRatioKgPerKgDryAir);



&#x20;   double CalculateTemperatureKFromEnthalpy(

&#x20;       double specificEnthalpyJPerKgDryAir,

&#x20;       double humidityRatioKgPerKgDryAir);



&#x20;   double CalculateSpecificVolumeM3PerKgDryAir(

&#x20;       double temperatureK,

&#x20;       double pressurePa,

&#x20;       double humidityRatioKgPerKgDryAir);

}

```



---



# 54. Statelessness and Thread Safety



The default calculator implementation shall be stateless after construction.



It shall be safe for concurrent use by:



* Console simulations

* Desktop UI simulations

* ASP.NET Core requests

* Background web jobs

* Parallel parameter studies



Any caches shall be:



* Immutable

* Thread-safe

* Optional

* Transparent to the result



---



# 55. Web Execution Requirements



Psychrometric calculations shall produce identical results regardless of whether they execute:



* In an ASP.NET Core server process

* In a desktop application

* In a command-line application

* In a test runner

* In a WebAssembly client, if the Core assembly is later reused there



No calculation shall depend on:



* Current culture

* Decimal separator

* Local timezone

* Operating system

* Browser type



JSON serialization shall use explicit property names and SI units.



---



# 56. Web API Data Contract Guidance



API request models may expose user-friendly units:



```csharp

public sealed record MoistAirRequest

{

&#x20;   public required double TemperatureC { get; init; }



&#x20;   public required double RelativeHumidityPercent { get; init; }



&#x20;   public double PressurePa { get; init; } = 101325.0;



&#x20;   public double AirFlowM3PerHour { get; init; }

}

```



The API layer shall convert these values to the Core model.



API response:



```csharp

public sealed record MoistAirResponse

{

&#x20;   public required double TemperatureC { get; init; }



&#x20;   public required double RelativeHumidityPercent { get; init; }



&#x20;   public required double HumidityRatioGPerKgDryAir { get; init; }



&#x20;   public required double DewPointC { get; init; }



&#x20;   public required double EnthalpyKJPerKgDryAir { get; init; }



&#x20;   public required double AbsoluteHumidityGPerM3 { get; init; }



&#x20;   public required double DensityKgPerM3 { get; init; }

}

```



API DTOs shall not replace Core state types.



---



# 57. Serialization Precision



The web API shall preserve sufficient numerical precision.



Recommended:



* Internal computation: full `double`

* JSON response: no manual premature rounding

* UI display: configurable rounding

* CSV: invariant-culture round-trip or high-precision formatting



Rounding shall occur only for display.



---



# 58. Unit Tests: Saturation Pressure



Minimum tests shall include:



```text

0°C

10°C

20°C

25°C

30°C

35°C

50°C

80°C

100°C where valid

```



Each result shall be compared to trusted reference data within the selected approximation's documented tolerance.



---



# 59. Unit Tests: Relative Humidity and Humidity Ratio



For each reference case:



1. Create a state from temperature and RH.

2. Read the calculated humidity ratio.

3. Recreate the state from temperature and humidity ratio.

4. Verify that RH is reproduced within tolerance.



This is a round-trip test.



---



# 60. Unit Tests: Dew Point



Required cases:



| Dry-bulb temperature |  RH |

| -------------------: | --: |

|                  0°C | 30% |

|                  0°C | 50% |

|                 10°C | 40% |

|                 20°C | 50% |

|                 25°C | 30% |

|                 25°C | 50% |

|                 30°C | 40% |

|                 35°C | 50% |

|                 35°C | 90% |



For each case:



[

p_{ws}(T_{dp})

]



shall reproduce the original vapor pressure within tolerance.



---



# 61. Unit Test: Sensible Heating



Initial state:



```text

25°C

50% RH

101325 Pa

```



Heat the air to:



```text

80°C

```



Expected:



* Humidity ratio unchanged

* Water-vapor mass flow unchanged

* Dew point unchanged within tolerance

* Relative humidity substantially reduced

* Enthalpy increased

* No water created or removed



This test is critical for the AWG concept.



---



# 62. Unit Test: Saturated State



For:



```text

30°C

100% RH

```



Expected:



```text

Dew point = 30°C

Relative humidity = 1

Humidity ratio = saturation humidity ratio

Phase state = Saturated

```



within tolerance.



---



# 63. Unit Test: Supersaturation Detection



Create a state request with temperature and humidity ratio corresponding to a dew point above dry-bulb temperature.



Expected:



* The factory shall not silently clamp RH.

* The result shall be classified as a supersaturated candidate or rejected.

* A condensation resolver shall be required.



---



# 64. Unit Test: Air Mixing



Mix two air streams with different temperatures and humidity ratios.



Verify:



* Dry-air mass conservation

* Water-vapor mass conservation

* Enthalpy conservation

* Physically consistent mixed temperature

* Physically consistent mixed RH



---



# 65. Unit Test: Recirculation



Mix:



* Ambient fresh air

* Hot humid recirculated air



Verify that the mixed state contains exactly the combined:



* Dry-air flow

* Vapor flow

* Enthalpy flow



This test shall support future optimization of AWG exhaust losses.



---



# 66. Acceptance Criteria



The psychrometric module is accepted when:



1. It creates physically consistent moist-air states.

2. RH and humidity-ratio conversions round-trip within tolerance.

3. Dew-point inversion uses the same saturation model as forward calculations.

4. Sensible heating preserves humidity ratio and dew point.

5. Mixing conserves dry air, water and enthalpy.

6. Supersaturation is detected instead of hidden.

7. All calculations use SI units internally.

8. The implementation has no UI dependency.

9. Results are deterministic across Console, desktop and web hosts.

10. All public calculations are covered by unit tests.



---



# 67. Implementation Priority



Recommended order:



```text

1. Psychrometric constants

2. Saturation-pressure provider

3. Vapor-pressure calculation

4. Humidity-ratio conversion

5. Relative-humidity conversion

6. Dew-point inversion

7. Enthalpy calculation

8. Temperature from enthalpy

9. Specific volume

10. Density

11. MoistAirState factory

12. State validation

13. Mixer calculations

14. Condensation support methods

15. Unit tests

16. Web API DTO mapping

```



---



# 68. Model Limitations



The first implementation does not model:



* Wet-bulb temperature

* Ice saturation

* Frost formation

* Fog droplets

* Liquid aerosol

* Detailed steam tables

* Non-ideal gas behavior

* Chemical contaminants

* Altitude-dependent gravitational effects

* Dynamic diffusion within the air stream

* Spatial humidity gradients



These may be added as separate fidelity levels.



---



# 69. Relationship to Other Documents



General conservation laws:



```text

04_MathematicalModel.md

```



Solar collector outlet-state calculation:



```text

06_SolarCollector.md

```



Peltier and cold-side heat flow:



```text

08_Peltier.md

```



Silica-gel vapor transfer:



```text

09_SilicaGel.md

```



Condensation process:



```text

10_Condenser.md

```



Heat-recovery air mixing and transfer:



```text

11_HeatRecovery.md

```



Constants:



```text

26_Constants.md

```



Units:



```text

27_Units.md

```



---



# 70. Final Psychrometric Principle



The moisture content of air shall be determined from conserved water mass, not from an arbitrarily assigned relative humidity.



Relative humidity and dew point shall always be derived from:



```text

Temperature

Pressure

Humidity ratio

```



Sensible heating shall change temperature and relative humidity, but shall not change humidity ratio or dew point.



Water shall enter or leave a moist-air stream only through an explicit mass-transfer process.



---



**End of Document**



