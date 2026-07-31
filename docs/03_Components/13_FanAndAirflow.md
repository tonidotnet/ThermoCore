# ThermoCore
## 13_FanAndAirflow.md

**Version:** 1.1  
**Status:** Implemented  
**Document Type:** Engineering and implementation specification  
**Internal units:** SI  
**Primary implementation language:** C#

---


# 1. Purpose

This document defines fans, ducts and the pressure-flow network used by ThermoCore.

The airflow model shall transport dry air, water vapor and enthalpy through connected components while distinguishing prescribed-flow simulation from coupled fan/system operating-point simulation.

# 2. Domains and responsibilities

The airflow subsystem shall:

- represent directed moist-air flow paths;
- aggregate component pressure losses;
- model fan pressure rise and electrical consumption;
- solve an operating point where fan and system curves intersect;
- support split, mix, bypass and recirculation paths;
- report invalid or imbalanced flows;
- avoid duplicating heat or moisture transfer performed by physical components.

# 3. Flow variables

Authoritative flow variable:

\[
\dot m_{da}
\]

Derived volumetric flow at a state:

\[
\dot V = \dot m_{da}v_{ma}
\]

Total moist-air mass flow:

\[
\dot m_{ma}=\dot m_{da}(1+W)
\]

# 4. Pressure convention

Each passive component exposes a non-negative pressure loss in its configured flow direction.

A fan exposes pressure rise.

For a closed path:

\[
\Delta p_{fan}
=
\sum_i \Delta p_{loss,i}
\]

# 5. Passive pressure loss

Generic quadratic form:

\[
\Delta p
=
K\frac{\rho v^2}{2}
\]

Reference-curve form:

\[
\Delta p
=
\Delta p_{ref}
\left(
\frac{\dot V}{\dot V_{ref}}
\right)^n
\]

with \(n\) normally near 2 for turbulent losses.

# 6. Duct friction

\[
\Delta p_{duct}
=
f\frac{L}{D_h}\frac{\rho v^2}{2}
\]

Local losses:

\[
\Delta p_{local}
=
\sum K_i\frac{\rho v^2}{2}
\]

# 7. Fan curve

A polynomial representation may use:

\[
\Delta p_{fan}(\dot V,u)
=
a_0(u)+a_1(u)\dot V+a_2(u)\dot V^2
\]

where \(u\) is normalized speed or control fraction.

Similarity-law approximation:

\[
\dot V\propto n
\]

\[
\Delta p\propto n^2
\]

\[
P\propto n^3
\]

Use only within the fan's calibrated speed range.

# 8. Fan electrical power

\[
P_{air}
=
\Delta p\dot V
\]

\[
P_{electrical}
=
\frac{\Delta p\dot V}{\eta_{fan}\eta_{driver}}
\]

At very low flow, a measured power curve is preferred because the simple formula may underpredict idle electrical consumption.

# 9. Prescribed-flow mode

Inputs:

- fixed dry-air or volumetric flow;
- component pressure-loss functions.

Outputs:

- path pressure loss;
- required fan pressure;
- estimated fan power.

This mode is recommended for early component development.

# 10. Coupled operating-point mode

Solve:

\[
F(\dot V)
=
\Delta p_{fan}(\dot V)
-
\Delta p_{system}(\dot V)
=
0
\]

Use a bracketed root finder over a physically valid flow interval. No operating point shall be invented if the curves do not intersect.

# 11. Mixer

Dry-air balance:

\[
\dot m_{da,out}
=
\sum_i\dot m_{da,i}
\]

Water and enthalpy are mixed according to `04_MathematicalModel.md` and `05_Psychrometrics.md`.

# 12. Splitter

For fractions \(f_i\):

\[
\sum_i f_i=1
\]

\[
\dot m_{da,i}=f_i\dot m_{da,in}
\]

Intensive state properties remain unchanged for an ideal splitter.

# 13. Recirculation

Recirculation fraction:

\[
r=\dot m_{da,recirc}/\dot m_{da,out}
\]

\[
0\le r<1
\]

At steady dry-air inventory:

\[
\dot m_{da,fresh}
=
\dot m_{da,exhaust}
\]

The network shall report exhausted vapor and recirculated vapor separately.

# 14. Network data model

```csharp
public sealed record AirflowNode
{
    public required string Id { get; init; }
}

public sealed record AirflowBranch
{
    public required string Id { get; init; }
    public required string FromNodeId { get; init; }
    public required string ToNodeId { get; init; }
    public required IPressureLossModel PressureLossModel { get; init; }
}

public sealed record FanOperatingPoint
{
    public required double VolumetricFlowM3PerSecond { get; init; }
    public required double DryAirMassFlowKgPerSecond { get; init; }
    public required double PressureRisePa { get; init; }
    public required double ElectricalPowerW { get; init; }
    public required double EfficiencyFraction { get; init; }
}
```

# 15. Fan configuration

```csharp
public sealed record FanParameters
{
    public required double MaximumFlowM3PerSecond { get; init; }
    public required double ShutoffPressurePa { get; init; }
    public required double MaximumElectricalPowerW { get; init; }
    public required double DriverEfficiencyFraction { get; init; }
    public required IReadOnlyList<FanCurvePoint> CurvePoints { get; init; }
    public required double MinimumControlFraction { get; init; }
    public required double MaximumControlFraction { get; init; }
}
```

# 16. Solver sequence

1. Validate network topology.
2. Determine active branches, bypasses and split fractions.
3. Build system pressure-loss function.
4. In prescribed-flow mode, evaluate loss directly.
5. In coupled mode, bracket and solve fan/system intersection.
6. Convert volumetric flow to dry-air mass flow at the appropriate reference state.
7. Propagate flows.
8. Validate node mass balances.
9. Calculate fan power.
10. Return diagnostics and operating point.

# 17. Diagnostics

```text
NoFanOperatingPoint
FanStalled
FanOutsideCalibratedCurve
PressureDropExceedsFanCapability
AirflowBelowComponentMinimum
AirflowAboveComponentMaximum
NodeDryAirImbalance
RecirculationFractionInvalid
FlowSolverFailed
FanEfficiencyInvalid
```

# 18. Required tests

- zero control and zero flow;
- prescribed flow;
- quadratic pressure-loss scaling;
- known fan/system intersection;
- no-intersection case;
- fan affinity-law scaling;
- mixer and splitter balances;
- recirculation dry-air balance;
- fan power and efficiency;
- deterministic network solution.

# 19. Acceptance criteria

- dry air is conserved at all nodes;
- water and enthalpy are changed only by explicit components;
- fan power is explicit;
- pressure losses are traceable to branches;
- invalid operating points produce diagnostics;
- the network supports the AWG V3 recirculation loop without AWG-specific logic in Core.

---

**End of Document**
