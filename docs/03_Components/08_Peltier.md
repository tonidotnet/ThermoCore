# ThermoCore
## 08_Peltier.md

**Version:** 1.0  
**Document Type:** Thermoelectric Component Engineering and Mathematical Specification  
**Status:** Draft  
**Applies To:** ThermoCore.Core, ThermoCore.AWG and future thermoelectric modules  
**Primary implementation language:** C#  
**Internal unit system:** SI

---

# 1. Purpose

This document defines the mathematical and software model of a thermoelectric Peltier module used by ThermoCore.

The component converts electrical power into a controlled heat transfer from a cold side to a hot side.

The model shall calculate:

- Electrical input power
- Cold-side heat pumping capacity
- Hot-side heat rejection
- Cold-side temperature
- Hot-side temperature
- Temperature difference
- Cooling coefficient of performance
- Heating coefficient of performance
- Joule heating
- Seebeck and Peltier effects
- Internal thermal conduction
- External thermal-resistance effects
- Operating-point limits
- Condenser cooling availability
- Hot-side air-heating contribution
- Dynamic thermal response
- Electrical and thermal balance residuals
- Overtemperature, overcurrent and invalid-state diagnostics

The Peltier component shall remain independent from AWG-specific control logic.

---

# 2. Scope

The initial implementation targets a single-stage thermoelectric cooler module such as a common bismuth-telluride TEC module.

The model shall support:

- Variable supply voltage
- Variable supply current
- Variable electrical power
- PWM-equivalent average-power control
- Variable hot-side thermal resistance
- Variable cold-side thermal resistance
- Variable ambient and process-air temperature
- Dynamic hot-side and cold-side temperatures
- Manufacturer-data-based calibration
- Simplified constant-COP operation
- Multiple fidelity levels
- Series or parallel use through higher-level composite components

The first implementation shall not require:

- Detailed semiconductor finite-element analysis
- Individual thermocouple-pair simulation
- Switching-ripple waveform simulation
- Electromagnetic parasitics
- Detailed mechanical-stress simulation
- Contact-pressure distribution
- Moisture ingress into the ceramic package
- Long-term material degradation
- Full nonlinear circuit simulation

---

# 3. Architectural Placement

The generic Peltier model shall be implemented in:

```text
ThermoCore.Core
```

Recommended namespace:

```csharp
ThermoCore.Core.Components.Thermoelectric
```

AWG-specific mounting, control and topology shall be implemented in:

```text
ThermoCore.AWG
```

The generic Peltier component shall not know:

- Whether its cold side is connected to an AWG condenser
- Whether its hot side heats incoming process air
- Whether silica gel is present
- Whether water is being produced
- Which operating mode the AWG controller selected
- Which UI host runs the simulation

---

# 4. Component Classification

The Peltier module is:

```text
Electrical-energy consumer
Heat-pump component
Multi-domain conversion component
Dynamic thermal component
Controlled component
```

It converts:

```text
Electrical power
        +
Cold-side absorbed heat
        ↓
Hot-side rejected heat
```

The fundamental balance is:

\[
Q_h = Q_c + P_e
\]

where:

- \(Q_c\) is cold-side heat absorbed
- \(Q_h\) is hot-side heat rejected
- \(P_e\) is electrical input power

---

# 5. Ports

Recommended ports:

```text
ElectricalPowerIn
ColdSideHeatIn
HotSideHeatOut
ControlIn
```

Optional temperature boundary ports:

```text
ColdSideThermalBoundary
HotSideThermalBoundary
```

Optional diagnostic ports:

```text
ColdSideTemperatureMeasurement
HotSideTemperatureMeasurement
ElectricalCurrentMeasurement
ElectricalVoltageMeasurement
CoolingPowerMeasurement
```

---

# 6. Internal State

The Peltier module shall be stateful when thermal masses are enabled.

Recommended state:

```csharp
public sealed record PeltierState
{
    public required double ColdSideTemperatureK { get; init; }

    public required double HotSideTemperatureK { get; init; }

    public required double ModuleAverageTemperatureK { get; init; }

    public required double StoredThermalEnergyJ { get; init; }

    public required double LastElectricalPowerW { get; init; }

    public required double LastCoolingPowerW { get; init; }

    public required double LastHeatingPowerW { get; init; }

    public required double LastCoolingCop { get; init; }

    public required double LastHeatingCop { get; init; }

    public required double LastCurrentA { get; init; }

    public required double LastVoltageV { get; init; }

    public required bool IsEnabled { get; init; }
}
```

---

# 7. Configuration Model

Recommended configuration:

```csharp
public sealed record PeltierParameters
{
    public required double SeebeckCoefficientVPerK { get; init; }

    public required double ElectricalResistanceOhm { get; init; }

    public required double ThermalConductanceWPerK { get; init; }

    public required double MaximumCurrentA { get; init; }

    public required double MaximumVoltageV { get; init; }

    public required double MaximumElectricalPowerW { get; init; }

    public required double MaximumTemperatureDifferenceK { get; init; }

    public required double MaximumHotSideTemperatureK { get; init; }

    public required double MinimumColdSideTemperatureK { get; init; }

    public required double EffectiveColdSideThermalCapacityJPerK { get; init; }

    public required double EffectiveHotSideThermalCapacityJPerK { get; init; }

    public required double InternalThermalCapacityJPerK { get; init; }

    public required double ColdSideContactThermalResistanceKPerW { get; init; }

    public required double HotSideContactThermalResistanceKPerW { get; init; }

    public required double ColdSideSpreaderThermalResistanceKPerW { get; init; }

    public required double HotSideHeatSinkThermalResistanceKPerW { get; init; }

    public required double DriverEfficiencyFraction { get; init; }

    public required double ControlMinimumFraction { get; init; }

    public required double ControlMaximumFraction { get; init; }

    public double MinimumUsefulCoolingCop { get; init; } = 0.0;

    public double MaximumAllowedColdSideHeatFluxWPerM2 { get; init; }

    public double ActiveColdSideAreaM2 { get; init; }

    public double ActiveHotSideAreaM2 { get; init; }

    public bool AllowReverseOperation { get; init; } = false;
}
```

---

# 8. Required Inputs

The component shall receive or derive:

```text
Electrical supply voltage or available electrical power
Control request
Cold-side thermal boundary temperature or heat load
Hot-side thermal boundary temperature or heat-sink condition
Current timestep
Current internal state
Optional manufacturer calibration data
```

The minimum first-version input set is:

```text
Requested electrical power
Cold-side boundary temperature
Hot-side boundary temperature
Current Peltier state
```

---

# 9. Thermoelectric Governing Equations

For a simplified single-stage thermoelectric module:

Cold-side heat pumping:

\[
Q_c
=
\alpha I T_c
-
\frac{1}{2}I^2R
-
K(T_h-T_c)
\]

Hot-side heat rejection:

\[
Q_h
=
\alpha I T_h
+
\frac{1}{2}I^2R
-
K(T_h-T_c)
\]

Electrical voltage:

\[
V
=
\alpha(T_h-T_c)
+
IR
\]

Electrical input power:

\[
P_e
=
VI
\]

The equations satisfy:

\[
Q_h = Q_c + P_e
\]

where:

- \(\alpha\) is effective Seebeck coefficient
- \(I\) is current
- \(R\) is electrical resistance
- \(K\) is thermal conductance
- \(T_c\) is cold-side temperature
- \(T_h\) is hot-side temperature

---

# 10. Sign Convention

The component shall use:

```text
Positive Qc = heat absorbed from the cold-side boundary
Positive Qh = heat delivered to the hot-side boundary
Positive Pe = electrical power consumed by the Peltier module
```

Normal cooling operation:

\[
Q_c \geq 0
\]

\[
Q_h > 0
\]

\[
P_e > 0
\]

If the calculated \(Q_c\) is negative, the operating point is not providing net cooling.

---

# 11. Average Module Temperature

Where temperature-dependent coefficients are used:

\[
T_m
=
\frac{T_h+T_c}{2}
\]

Then:

\[
\alpha=\alpha(T_m)
\]

\[
R=R(T_m)
\]

\[
K=K(T_m)
\]

The initial implementation may use constant coefficients calibrated at a reference temperature.

---

# 12. Electrical Power from Current

Given current:

\[
V
=
\alpha\Delta T+IR
\]

where:

\[
\Delta T=T_h-T_c
\]

Then:

\[
P_e
=
I
\left(
\alpha\Delta T+IR
\right)
\]

Equivalent:

\[
P_e
=
\alpha I\Delta T
+
I^2R
\]

---

# 13. Current from Voltage

Given applied voltage:

\[
I
=
\frac{V-\alpha\Delta T}{R}
\]

The result shall be limited by:

```text
Maximum current
Maximum voltage
Maximum electrical power
Driver capability
Control request
```

---

# 14. Current from Requested Electrical Power

When the control system provides requested power instead of voltage, solve:

\[
P_e
=
\alpha I\Delta T
+
I^2R
\]

Rearrange:

\[
RI^2+\alpha\Delta T I-P_e=0
\]

Positive cooling-current solution:

\[
I
=
\frac{
-\alpha\Delta T
+
\sqrt{
(\alpha\Delta T)^2+4RP_e
}
}{
2R
}
\]

The implementation shall validate the discriminant and use the physically meaningful root.

---

# 15. Joule Heating

Total Joule heating:

\[
Q_J
=
I^2R
\]

The simplified thermoelectric model assigns half to each side:

\[
Q_{J,c}
=
\frac{1}{2}I^2R
\]

\[
Q_{J,h}
=
\frac{1}{2}I^2R
\]

This approximation shall be treated as part of the selected model fidelity.

---

# 16. Internal Thermal Conduction

Heat conducted from hot side back to cold side:

\[
Q_K
=
K(T_h-T_c)
\]

This term reduces cooling capacity.

A high hot-side temperature therefore directly reduces \(Q_c\).

---

# 17. Peltier Heat Terms

Cold-side Peltier pumping:

\[
Q_{\Pi,c}
=
\alpha I T_c
\]

Hot-side Peltier release:

\[
Q_{\Pi,h}
=
\alpha I T_h
\]

These terms differ because hot- and cold-side temperatures differ.

---

# 18. Cold-Side Cooling Capacity

\[
Q_c
=
Q_{\Pi,c}
-
Q_{J,c}
-
Q_K
\]

Expanded:

\[
Q_c
=
\alpha I T_c
-
\frac{1}{2}I^2R
-
K(T_h-T_c)
\]

Cooling is available only when:

\[
Q_c>0
\]

---

# 19. Hot-Side Heat Rejection

\[
Q_h
=
Q_{\Pi,h}
+
Q_{J,h}
-
Q_K
\]

Expanded:

\[
Q_h
=
\alpha I T_h
+
\frac{1}{2}I^2R
-
K(T_h-T_c)
\]

Energy consistency shall be verified against:

\[
Q_h-Q_c-P_e=0
\]

---

# 20. Cooling Coefficient of Performance

\[
COP_c
=
\frac{Q_c}{P_e}
\]

The value is meaningful only when:

\[
Q_c>0
\]

and:

\[
P_e>0
\]

A low electrical operating point may provide better COP but lower absolute cooling power.

---

# 21. Heating Coefficient of Performance

\[
COP_h
=
\frac{Q_h}{P_e}
\]

Because:

\[
Q_h=Q_c+P_e
\]

then:

\[
COP_h=COP_c+1
\]

within numerical tolerance.

---

# 22. Maximum Temperature Difference

The configured maximum temperature difference is not the expected operating value under load.

The actual temperature difference depends on:

- Current
- Cold-side heat load
- Hot-side heat sink
- Contact resistances
- Ambient temperature
- Module coefficients
- Thermal losses

The model shall reject or diagnose:

\[
T_h-T_c
>
\Delta T_{max}
\]

unless the selected manufacturer model explicitly supports the state.

---

# 23. External Thermal Resistances

The ceramic temperatures are not equal to the temperatures of the connected condenser plate or heat sink.

Cold-side boundary relation:

\[
T_{load}
-
T_c
=
Q_c
R_{th,cold,total}
\]

Hot-side boundary relation:

\[
T_h
-
T_{sink}
=
Q_h
R_{th,hot,total}
\]

where:

\[
R_{th,cold,total}
=
R_{contact,cold}
+
R_{spreader,cold}
+
R_{condenser}
\]

\[
R_{th,hot,total}
=
R_{contact,hot}
+
R_{spreader,hot}
+
R_{heatsink}
\]

These equations may require iteration.

---

# 24. Hot-Side Heat-Sink Boundary

For a hot-side air heat exchanger:

\[
T_h
=
T_{air,mean}
+
Q_hR_{th,hot}
\]

where the thermal resistance may depend on airflow.

A simplified airflow-dependent model:

\[
R_{th,hot}
=
R_{th,ref}
\left(
\frac{\dot V_{ref}}{\max(\dot V,\dot V_{min})}
\right)^n
\]

The exponent \(n\) shall be configurable or calibrated.

---

# 25. Cold-Side Condenser Boundary

For a condenser plate:

\[
T_{plate}
=
T_c
+
Q_cR_{th,cold}
\]

Depending on sign convention and heat-flow direction, the implementation shall ensure that the physical plate contacting moist air is warmer than or equal to the ceramic cold face when heat flows into the module.

The condenser model shall use the effective plate temperature, not the ideal ceramic temperature.

---

# 26. Thermal Contact Resistance

Thermal contact resistance may include:

- Thermal paste
- Surface roughness
- Mounting pressure
- Copper spreader
- Aluminum plate
- Interface pads
- Oxide layers

It shall be represented explicitly when condenser accuracy matters.

---

# 27. Heat Spreader

A heat spreader may be used to enlarge the effective cold-side or hot-side area.

The model may represent it as:

```text
Thermal resistance
Thermal mass
Effective heat-transfer area
Maximum heat flux
```

A spreader does not create additional cooling power.

It redistributes heat and may reduce local heat flux.

---

# 28. Cold-Side Heat Flux

\[
q''_c
=
\frac{Q_c}{A_c}
\]

The component shall report heat flux when active area is configured.

If:

\[
q''_c
>
q''_{max}
\]

the component shall produce a warning.

---

# 29. Driver Efficiency

Electrical power drawn from the electrical network:

\[
P_{network}
=
\frac{P_e}{\eta_{driver}}
\]

Driver loss:

\[
P_{driver,loss}
=
P_{network}-P_e
\]

The driver loss shall be assigned to an electrical-loss or thermal sink.

---

# 30. PWM Control

The initial implementation may represent PWM control using average electrical power.

For control fraction \(u\):

\[
0\leq u\leq1
\]

Simplified average power:

\[
P_{requested}
=
uP_{max}
\]

This approximation does not reproduce current ripple or nonlinear pulse behavior.

A higher-fidelity model may evaluate on-state operating points and duty-cycle-average heat flows.

---

# 31. Control Fraction Limits

\[
u_{effective}
=
\min
\left(
u_{max},
\max(u_{min},u_{requested})
\right)
\]

When disabled:

\[
u_{effective}=0
\]

The controller shall not directly overwrite temperatures or cooling power.

---

# 32. Optimal Operating Current

For fixed side temperatures, cooling power is:

\[
Q_c(I)
=
\alpha IT_c
-
\frac{1}{2}I^2R
-
K\Delta T
\]

Differentiating:

\[
\frac{dQ_c}{dI}
=
\alpha T_c-IR
\]

Maximum \(Q_c\) occurs at:

\[
I_{Qc,max}
=
\frac{\alpha T_c}{R}
\]

subject to electrical and thermal limits.

This operating point does not necessarily maximize COP.

---

# 33. COP-Optimized Operation

Maximum COP occurs at a lower current than maximum cooling power for most practical conditions.

The AWG controller or optimization layer may choose between:

```text
Maximum cooling power
Maximum COP
Maximum daily water production
Minimum Wh per liter
Hot-side temperature limit
Battery-constrained operation
```

The generic Peltier component shall expose results, not choose the system objective.

---

# 34. Electrical Availability Constraint

The actual electrical power is limited by:

\[
P_{actual}
=
\min
(
P_{requested},
P_{source,available},
P_{driver,max},
P_{module,max}
)
\]

The component shall report:

```text
Requested power
Available power
Accepted power
Rejected power request
Limiting constraint
```

---

# 35. Dynamic Cold-Side Temperature

Cold-side thermal balance:

\[
C_c
\frac{dT_c}{dt}
=
Q_{load,cold}
-
Q_c
+
Q_{coupling,c}
\]

where:

- \(Q_{load,cold}\) enters from the condenser or cold load
- \(Q_c\) is removed by the Peltier effect
- \(Q_{coupling,c}\) represents other configured heat transfers

---

# 36. Dynamic Hot-Side Temperature

Hot-side thermal balance:

\[
C_h
\frac{dT_h}{dt}
=
Q_h
-
Q_{sink}
+
Q_{coupling,h}
\]

where:

- \(Q_h\) is heat rejected by the module
- \(Q_{sink}\) is removed by the hot-side heat exchanger
- \(Q_{coupling,h}\) represents other configured heat transfers

---

# 37. Explicit Euler Update

\[
T_{c,n+1}
=
T_{c,n}
+
\frac{
Q_{load,cold}
-
Q_c
}{
C_c
}
\Delta t
\]

\[
T_{h,n+1}
=
T_{h,n}
+
\frac{
Q_h
-
Q_{sink}
}{
C_h
}
\Delta t
\]

This method requires timestep-sensitivity testing.

---

# 38. Semi-Implicit Update

When thermal resistances dominate, a semi-implicit update is recommended.

For example, cold side:

\[
C_c
\frac{T_{c,n+1}-T_{c,n}}{\Delta t}
=
Q_{load}
-
Q_c(T_{c,n+1},T_{h,n+1})
\]

The coupled nonlinear system may be solved by fixed-point iteration or a safeguarded root solver.

---

# 39. Coupled Operating-Point Solution

The side temperatures and heat flows are mutually dependent.

Recommended iteration:

```text
1. Initialize Tc and Th from previous timestep.
2. Calculate electrical current.
3. Calculate voltage and electrical power.
4. Calculate Qc and Qh.
5. Calculate cold boundary temperature relation.
6. Calculate hot boundary temperature relation.
7. Update Tc and Th.
8. Apply relaxation.
9. Test convergence.
10. Repeat until converged or maximum iterations reached.
```

---

# 40. Convergence Variables

The solver shall check:

```text
Cold-side temperature
Hot-side temperature
Electrical current
Electrical voltage
Cooling power
Heating power
Electrical power
```

Recommended initial tolerances:

| Quantity | Absolute tolerance |
|---|---:|
| Temperature | 0.01 K |
| Current | 0.001 A |
| Voltage | 0.001 V |
| Heat flow | 0.1 W |
| Electrical power | 0.1 W |

Final tolerances shall be harmonized with `25_NumericalMethods.md`.

---

# 41. Relaxation

For calculated temperature \(T^*\):

\[
T^{k+1}
=
\lambda T^*
+
(1-\lambda)T^k
\]

where:

\[
0<\lambda\leq1
\]

A default relaxation factor between 0.2 and 0.8 may be useful for strongly coupled conditions.

---

# 42. Energy Balance

The thermoelectric balance shall satisfy:

\[
Q_h-Q_c-P_e=R_{TE}
\]

Network electrical balance:

\[
P_{network}
=
P_e
+
P_{driver,loss}
\]

System thermal balance shall also include:

```text
Cold-side boundary heat
Hot-side boundary heat
Thermal-storage changes
Environmental losses
```

---

# 43. Timestep Energy Balance

For one timestep:

\[
E_h
=
E_c
+
E_e
+
R_E
\]

where:

\[
E=Q\Delta t
\]

When dynamic side thermal masses are externalized as separate components, their storage changes shall not be double-counted inside the Peltier component.

---

# 44. Off-State Thermal Conduction

When electrical current is zero:

\[
I=0
\]

then:

\[
Q_c
=
-K(T_h-T_c)
\]

This indicates passive heat conduction from hot side to cold side.

The Peltier module therefore remains a thermal bridge when switched off.

The first implementation shall model this behavior.

---

# 45. Reverse Operation

If current direction is reversed, hot and cold sides swap.

The first AWG implementation shall disable reverse operation unless explicitly configured.

If reverse operation is not allowed and a negative current request occurs:

- Reject the request
- Emit a diagnostic
- Keep the module disabled or at zero current

---

# 46. Startup Condition

At startup:

\[
T_h\approx T_c
\]

As current begins:

- Cold side cools
- Hot side heats
- External thermal loads develop
- COP changes over time

The model shall not assume immediate steady-state \(\Delta T\).

---

# 47. Hot-Side Overheating

The hot side is critical.

When:

\[
T_h
>
T_{h,max}
\]

the component shall:

- Emit a critical diagnostic
- Reduce or disable electrical power according to control policy
- Continue passive thermal simulation
- Avoid silently clamping temperature
- Preserve energy balance

---

# 48. Cold-Side Minimum Temperature

When:

\[
T_c
<
T_{c,min}
\]

the component shall:

- Emit a warning or critical diagnostic
- Limit current if configured
- Consider frost risk at the application layer
- Avoid committing invalid manufacturer operating states

---

# 49. Condensation and Frost Risk

The generic Peltier module shall not itself calculate condensation.

It shall expose effective cold-side temperature and cooling capacity.

The connected condenser component shall compare surface temperature to dew point.

Frost modelling belongs to the condenser or phase-change module.

---

# 50. Hot-Side Air Heating

In the AWG concept, the hot-side heat exchanger may transfer \(Q_h\) to incoming process air.

Air enthalpy increase:

\[
h_{out}
=
h_{in}
+
\frac{Q_{air}}{\dot m_{da}}
\]

where:

\[
Q_{air}
\leq Q_h
\]

Any unrecovered hot-side heat shall be lost to an explicit environment sink or stored in thermal mass.

---

# 51. Hot-Side Heat-Recovery Fraction

A simplified heat-recovery fraction:

\[
\eta_{hot,recovery}
=
\frac{Q_{air}}{Q_h}
\]

where:

\[
0\leq\eta_{hot,recovery}\leq1
\]

Then:

\[
Q_{air}
=
\eta_{hot,recovery}Q_h
\]

\[
Q_{hot,loss}
=
Q_h-Q_{air}
\]

The preferred model uses thermal resistance and airflow rather than a fixed fraction.

---

# 52. AWG V3 Topology Requirement

Recommended process sequence:

```text
Ambient air
    ↓
Peltier hot-side heat exchanger
    ↓
Photovoltaic rear-air channel
    ↓
Solar air collector
    ↓
Silica-gel bed
    ↓
Condenser connected to Peltier cold side
```

The Peltier module shall participate in two separate thermal paths:

```text
Cold-side path:
Condenser → Cold spreader → Peltier cold side

Hot-side path:
Peltier hot side → Hot spreader → Air heat exchanger → Incoming air
```

The electrical component shall not directly modify moist-air states.

Separate heat-exchanger components shall perform those conversions.

---

# 53. Separation of Responsibilities

The Peltier component shall calculate:

```text
Electrical operating point
Qc
Qh
Tc
Th
COP
Limits
Residuals
```

The hot-side heat exchanger shall calculate:

```text
Air temperature rise
Pressure drop
Heat transferred from hot side
```

The condenser shall calculate:

```text
Air cooling
Condensed-water mass
Latent heat
Remaining humidity
```

This separation prevents hidden double counting.

---

# 54. Simplified Fidelity Level 0

Constant cooling source:

```text
Configured Qc
Configured electrical power
Qh = Qc + Pe
```

Use cases:

- Graph testing
- Condenser development
- UI development
- Early system studies

---

# 55. Fidelity Level 1

Constant COP model:

\[
Q_c
=
COP_cP_e
\]

\[
Q_h
=
Q_c+P_e
\]

Includes:

- Electrical limit
- Constant COP
- Configured maximum \(\Delta T\)
- No thermoelectric coefficient model

---

# 56. Fidelity Level 2

Temperature-dependent empirical model:

Includes:

- Manufacturer \(Q_c\) versus current and \(\Delta T\) data
- Interpolation
- COP calculation
- Electrical limits
- Thermal-resistance corrections

This may be the most practical model when reliable datasheets are available.

---

# 57. Fidelity Level 3

Analytical thermoelectric model:

Includes:

- Seebeck coefficient
- Electrical resistance
- Thermal conductance
- Joule heating
- Internal conduction
- Dynamic side temperatures
- Coupled iteration
- Contact resistances

This is the recommended engineering model when coefficients can be calibrated.

---

# 58. Fidelity Level 4

Temperature-dependent calibrated model:

Includes:

- \(\alpha(T)\)
- \(R(T)\)
- \(K(T)\)
- Manufacturer curves
- Measured heat-sink performance
- Measured spreader resistance
- Prototype calibration
- Dynamic controller behavior

---

# 59. Manufacturer Data Calibration

Common datasheet values may include:

```text
Imax
Vmax
Qmax
ΔTmax
Hot-side reference temperature
Module dimensions
Maximum operating temperature
```

These values alone are not sufficient to uniquely determine every model coefficient without assumptions.

A calibration routine may fit:

```text
Effective Seebeck coefficient
Electrical resistance
Thermal conductance
Contact thermal resistances
Temperature dependence
```

The source and fitting method shall be recorded.

---

# 60. Parameter Estimation from Datasheet

At \(\Delta T=0\):

\[
Q_c
=
\alpha IT_c
-
\frac{1}{2}I^2R
\]

At the datasheet maximum-current point, measured \(Q_{max}\), \(I_{max}\), \(V_{max}\) and reference \(T_h\) may be used to estimate \(\alpha\) and \(R\).

At \(Q_c=0\) and \(\Delta T=\Delta T_{max}\):

\[
0
=
\alpha I T_c
-
\frac{1}{2}I^2R
-
K\Delta T_{max}
\]

These relations may support initial fitting.

The calibration document shall state all assumptions.

---

# 61. Example Simplified Operating Point

Assume:

```text
Electrical power: 30 W
Cooling COP: 0.45
```

Then:

\[
Q_c
=
0.45\cdot30
=
13.5\ W
\]

\[
Q_h
=
13.5+30
=
43.5\ W
\]

This demonstrates that the hot side must reject more heat than the electrical input alone.

---

# 62. Example Condensation Energy Limit

Assume available cooling power:

\[
Q_c=13.5\ W
\]

If all cooling were used only for condensation at approximately:

\[
h_{fg}=2.4\times10^6\ J/kg
\]

maximum ideal condensation rate:

\[
\dot m
=
\frac{13.5}{2.4\times10^6}
\approx
5.63\times10^{-6}\ kg/s
\]

Equivalent hourly rate:

\[
0.0203\ kg/h
\]

approximately:

```text
20 mL/h
```

Real output is lower because sensible air cooling and losses also consume cooling power.

This illustrates why small Peltier power strongly limits water production.

---

# 63. Example Hot-Side Requirement

For:

```text
Qc = 13.5 W
Pe = 30 W
```

hot-side rejection:

\[
Q_h=43.5\ W
\]

If hot-side total thermal resistance is:

\[
R_{th,hot}=1.5\ K/W
\]

temperature rise above sink boundary:

\[
\Delta T_h
=
43.5\cdot1.5
=
65.25\ K
\]

This is too high for many operating conditions.

Therefore, low hot-side thermal resistance is essential.

---

# 64. Invalid Configuration Rules

Reject configuration when:

- Seebeck coefficient is non-finite
- Electrical resistance is non-positive
- Thermal conductance is negative
- Maximum current is non-positive
- Maximum voltage is non-positive
- Maximum electrical power is non-positive
- Maximum temperature difference is non-positive
- Hot-side maximum temperature is invalid
- Cold-side minimum temperature is below absolute zero
- Driver efficiency is outside 0–1
- Thermal capacities are negative
- Thermal resistances are negative
- Control minimum exceeds control maximum
- Active area is negative
- Any required input is NaN or infinite

---

# 65. Runtime Diagnostics

Recommended diagnostics:

```text
Hot-side overtemperature
Cold-side below minimum temperature
Temperature difference exceeds configured maximum
Requested power exceeds source capability
Requested current exceeds module maximum
Requested voltage exceeds module maximum
Cooling power is negative
COP below useful threshold
Driver loss is significant
Hot-side thermal resistance too high
Cold-side contact resistance too high
Thermal iteration failed to converge
Energy-balance residual above tolerance
Electrical operating point invalid
Module disabled by protection
Cold-side heat flux exceeds limit
```

---

# 66. Required Unit Tests

## TEC-001 Disabled module

Expected:

- Current equals zero
- Electrical power equals zero
- Passive thermal conduction remains active
- Energy balance remains valid

## TEC-002 Zero temperature difference

Expected:

- Analytical equations match configured coefficients
- \(Q_h=Q_c+P_e\)

## TEC-003 Increased current

At fixed side temperatures:

- Cooling power initially increases
- Joule heating increases quadratically
- COP changes
- Limits are enforced

## TEC-004 Increased hot-side temperature

At equal current and cold-side temperature:

- Cooling power decreases or operating margin worsens

## TEC-005 Electrical balance

Expected:

\[
P_{network}
=
P_e+P_{driver,loss}
\]

within tolerance.

## TEC-006 Thermal balance

Expected:

\[
Q_h=Q_c+P_e
\]

within tolerance.

## TEC-007 Power-request solver

Given requested power:

- Current solution reproduces electrical power within tolerance

## TEC-008 Maximum-current limit

Expected:

- Current does not exceed configured maximum
- Rejected request is reported

## TEC-009 Maximum-temperature difference

Expected:

- Invalid operating state generates a diagnostic

## TEC-010 Off-state conduction

With \(T_h>T_c\) and \(I=0\):

- Heat conducts from hot to cold side

## TEC-011 Dynamic response

Expected:

- Side temperatures change consistently with thermal capacities
- No instantaneous jump occurs in dynamic fidelity mode

## TEC-012 Determinism

Identical states and inputs produce identical results.

---

# 67. Integration Tests

## TEC-INT-001 Peltier and condenser

Expected:

- Condenser receives available cold-side heat-removal capacity
- Condensed-water calculation includes latent heat
- Condenser does not exceed \(Q_c\)

## TEC-INT-002 Peltier and hot-side air exchanger

Expected:

- Hot-side air exchanger receives \(Q_h\)
- Air enthalpy rise plus losses equals hot-side heat rejection

## TEC-INT-003 Peltier and battery

Expected:

- Electrical draw cannot exceed battery and panel availability
- Driver losses are included
- SOC changes consistently

## TEC-INT-004 Peltier and solar panel

Expected:

- Solar-panel output and battery jointly limit Peltier operation
- No electrical energy is double-counted

## TEC-INT-005 Web and console consistency

The same configuration shall produce identical results in:

```text
ThermoCore.Console
ThermoCore.Web
ThermoCore.Desktop
```

## TEC-INT-006 AWG recirculation

Changing recirculation shall affect condenser load and hot-side inlet conditions without modifying Peltier equations.

---

# 68. Web API Configuration Example

```json
{
  "seebeckCoefficientVPerK": 0.052,
  "electricalResistanceOhm": 1.9,
  "thermalConductanceWPerK": 0.75,
  "maximumCurrentA": 6.0,
  "maximumVoltageV": 15.4,
  "maximumElectricalPowerW": 70.0,
  "maximumTemperatureDifferenceK": 67.0,
  "maximumHotSideTemperatureC": 90.0,
  "minimumColdSideTemperatureC": -20.0,
  "effectiveColdSideThermalCapacityJPerK": 800.0,
  "effectiveHotSideThermalCapacityJPerK": 1200.0,
  "internalThermalCapacityJPerK": 400.0,
  "coldSideContactThermalResistanceKPerW": 0.15,
  "hotSideContactThermalResistanceKPerW": 0.12,
  "coldSideSpreaderThermalResistanceKPerW": 0.18,
  "hotSideHeatSinkThermalResistanceKPerW": 0.60,
  "driverEfficiencyFraction": 0.94,
  "controlMinimumFraction": 0.0,
  "controlMaximumFraction": 1.0,
  "minimumUsefulCoolingCop": 0.15,
  "activeColdSideAreaM2": 0.0016,
  "activeHotSideAreaM2": 0.0016,
  "allowReverseOperation": false
}
```

The API layer shall convert Celsius values to kelvin before constructing Core parameters.

The numerical values above are illustrative placeholders and shall not be treated as validated data for a specific module.

---

# 69. Recommended C# Interface

```csharp
public interface IPeltierModel
{
    PeltierStepResult Evaluate(
        PeltierControlRequest control,
        ElectricalPowerAvailability electricalAvailability,
        ThermalBoundaryState coldBoundary,
        ThermalBoundaryState hotBoundary,
        PeltierState currentState,
        PeltierParameters parameters,
        TimeSpan timeStep);
}
```

---

# 70. Control Request Model

```csharp
public sealed record PeltierControlRequest
{
    public required bool Enabled { get; init; }

    public required PeltierControlMode Mode { get; init; }

    public double RequestedControlFraction { get; init; }

    public double? RequestedElectricalPowerW { get; init; }

    public double? RequestedCurrentA { get; init; }

    public double? RequestedColdSideTemperatureK { get; init; }
}
```

Recommended modes:

```csharp
public enum PeltierControlMode
{
    Disabled,
    Power,
    Current,
    ControlFraction,
    ColdSideTemperature
}
```

---

# 71. Thermal Boundary Model

```csharp
public sealed record ThermalBoundaryState
{
    public required double BoundaryTemperatureK { get; init; }

    public required double ThermalResistanceKPerW { get; init; }

    public required double AvailableHeatCapacityJPerK { get; init; }

    public double? AppliedHeatLoadW { get; init; }
}
```

A future model may replace this simplified boundary with explicit connected thermal components.

---

# 72. Proposed Result Model

```csharp
public sealed record PeltierStepResult
{
    public required PeltierState ProposedState { get; init; }

    public required double RequestedElectricalPowerW { get; init; }

    public required double AcceptedElectricalPowerW { get; init; }

    public required double NetworkElectricalPowerW { get; init; }

    public required double DriverLossW { get; init; }

    public required double CurrentA { get; init; }

    public required double VoltageV { get; init; }

    public required double CoolingPowerW { get; init; }

    public required double HeatingPowerW { get; init; }

    public required double CoolingCop { get; init; }

    public required double HeatingCop { get; init; }

    public required double ColdSideTemperatureK { get; init; }

    public required double HotSideTemperatureK { get; init; }

    public required double TemperatureDifferenceK { get; init; }

    public required ConservationBalance Balance { get; init; }

    public required IReadOnlyCollection<SimulationDiagnostic> Diagnostics { get; init; }
}
```

---

# 73. Determinism and Thread Safety

The Peltier model shall:

- Be deterministic
- Avoid mutable static state
- Avoid dependence on system clock
- Avoid UI dependencies
- Support parallel scenario execution
- Return immutable results
- Use only supplied input, parameters and state
- Avoid hidden manufacturer-data lookups at runtime

---

# 74. Calibration Requirements

Future prototype calibration should record:

```text
Supply voltage
Supply current
Electrical power
Cold ceramic temperature
Cold spreader temperature
Condenser surface temperature
Hot ceramic temperature
Hot spreader temperature
Hot-side air inlet temperature
Hot-side air outlet temperature
Hot-side airflow
Cold-side heat load
Condensed-water rate
Ambient temperature
```

Calibration targets:

```text
Seebeck coefficient
Electrical resistance
Thermal conductance
Cold contact resistance
Hot contact resistance
Heat-sink thermal resistance
Driver efficiency
Dynamic thermal capacities
Temperature dependence
```

---

# 75. Acceptance Criteria

The Peltier module is accepted when:

1. It satisfies \(Q_h=Q_c+P_e\) within tolerance.
2. It limits current, voltage and power to configured values.
3. It models passive heat conduction when disabled.
4. It calculates cooling COP and heating COP consistently.
5. It exposes actual cold-side and hot-side temperatures.
6. It includes external thermal resistances.
7. It detects hot-side overheating.
8. It detects invalid temperature difference.
9. It does not calculate condensation internally.
10. It does not directly modify air states.
11. It supports at least fidelity levels 0–3.
12. It produces identical results in console, desktop and web hosts.
13. It reports driver losses explicitly.
14. It supports battery- and solar-limited operation.
15. It returns immutable, auditable results.
16. It separates evaluation from commit.
17. It supports later calibration from manufacturer and prototype data.

---

# 76. Relationship to Other Documents

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

Photovoltaic electrical source:

```text
07_SolarPanel.md
```

Silica-gel model:

```text
09_SilicaGel.md
```

Condenser model:

```text
10_Condenser.md
```

Battery model:

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

---

# 77. Final Thermoelectric Principle

The Peltier module shall transfer heat according to a coupled electrical and thermal model.

It shall always satisfy:

\[
Q_h-Q_c-P_e=R_E
\]

where the residual shall approach zero within numerical tolerance.

A Peltier module does not create cold independently of its hot-side conditions.

Its useful cooling capacity depends on:

```text
Electrical current
Electrical resistance
Seebeck coefficient
Internal thermal conduction
Cold-side temperature
Hot-side temperature
External thermal resistances
Available heat-sink performance
```

The hot side must reject both the absorbed cold-side heat and the complete electrical input.

---

**End of Document**
