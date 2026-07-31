# ThermoCore
## 10_Condenser.md

**Version:** 1.1  
**Status:** Implemented  
**Document Type:** Engineering and implementation specification  
**Internal units:** SI  
**Primary implementation language:** C#

---


# 1. Purpose

This document defines the condenser used by ThermoCore.AWG. The condenser cools a moist-air stream, removes water vapor when the effective surface temperature is below the inlet dew point, produces a liquid-water stream and exposes the complete sensible and latent cooling demand.

The model shall never create liquid water by assigning a target relative humidity. Condensate shall be derived from conserved inlet water vapor, outlet vapor and liquid drainage.

# 2. Responsibilities

The condenser shall:

- accept a psychrometrically consistent inlet state;
- determine whether condensation is thermodynamically possible;
- calculate sensible cooling before and during condensation;
- calculate the equilibrium saturation limit at the effective apparatus dew point;
- limit condensation by cooling power, heat transfer, mass transfer and residence time;
- return a psychrometrically consistent outlet state;
- return a liquid-water output;
- report rejected heat, pressure drop, drainage loss and residuals;
- support static and dynamic fidelity levels.

It shall not calculate Peltier electrical behavior. The Peltier component supplies available cold-side cooling power and effective thermal-boundary information.

# 3. Ports

```text
MoistAirIn
MoistAirOut
CoolingPowerIn
LiquidWaterOut
RejectedHeatOut
OptionalControlIn
```

# 4. Authoritative inputs

```csharp
public sealed record CondenserStepInput
{
    public required MoistAirState InletAir { get; init; }
    public required double AvailableCoolingPowerW { get; init; }
    public required double EffectiveSurfaceTemperatureK { get; init; }
    public required double AmbientPressurePa { get; init; }
    public required TimeSpan TimeStep { get; init; }
}
```

# 5. Configuration

```csharp
public sealed record CondenserParameters
{
    public required double HeatTransferUaWPerK { get; init; }
    public required double MassTransferEffectivenessFraction { get; init; }
    public required double AirSideBypassFactor { get; init; }
    public required double DrainageEfficiencyFraction { get; init; }
    public required double EffectiveThermalCapacityJPerK { get; init; }
    public required double EnvironmentalLossCoefficientWPerK { get; init; }
    public required double ReferencePressureDropPa { get; init; }
    public required double ReferenceVolumetricFlowM3PerSecond { get; init; }
    public required double MinimumSurfaceTemperatureK { get; init; }
    public required double MaximumSurfaceTemperatureK { get; init; }
}
```

# 6. Condensation criterion

Condensation is possible when:

\[
T_s < T_{dp,in}
\]

Equivalently:

\[
p_{v,in} > p_{ws}(T_s)
\]

If this condition is false, the component may still cool the air sensibly but shall produce zero condensate.

# 7. Thermodynamic maximum

The saturated humidity ratio at the effective surface temperature is:

\[
W_{sat,s} = \epsilon \frac{p_{ws}(T_s)}{p-p_{ws}(T_s)}
\]

The ideal upper bound is:

\[
\dot m_{cond,thermo}
=
\dot m_{da}\max(0,W_{in}-W_{sat,s})
\]

This is only a thermodynamic upper bound.

# 8. Bypass-factor model

For a configured air-side bypass factor \(BF\):

\[
T_{out,ideal}
=
BF T_{in}+(1-BF)T_s
\]

\[
W_{out,ideal}
=
BF W_{in}+(1-BF)W_{sat,s}
\]

with:

\[
0\le BF\le1
\]

The result must be recalculated from conserved enthalpy and water mass if power limitation prevents reaching this state.

# 9. Sensible and latent cooling

The dry-air-based enthalpy method is authoritative:

\[
\dot Q_{total}
=
\dot m_{da}(h_{in}-h_{out})
-
\dot m_{cond}h_l(T_{drain})
\]

A reporting decomposition may use:

\[
\dot Q_{sensible}
\approx
\dot m_{da}(c_{p,da}+W_{in}c_{p,v})(T_{in}-T_{out})
\]

\[
\dot Q_{latent}
\approx
\dot m_{cond}h_{fg}
\]

The decomposition shall not be double-counted in the total balance.

# 10. Cooling-power limit

The actual outlet state shall satisfy:

\[
\dot Q_{total}\le \dot Q_{available}
\]

When the ideal outlet requires more cooling than available, solve for an outlet state between the inlet and ideal states. A safeguarded one-dimensional root solver should solve for condensate or outlet enthalpy.

# 11. Heat-transfer limit

A UA limit may be expressed as:

\[
\dot Q_{UA}
=
UA \cdot \Delta T_{lm}
\]

For the MVP, an effectiveness form is acceptable:

\[
\varepsilon_T
=
1-\exp\left(-\frac{UA}{C_{air}}\right)
\]

Actual heat removal is bounded by both available cooling and air-side heat transfer.

# 12. Mass-transfer limit

A configured effectiveness may limit the approach to saturation:

\[
W_{out}
=
W_{in}
-
\varepsilon_m
(W_{in}-W_{out,thermo})
\]

where:

\[
0\le \varepsilon_m\le1
\]

# 13. Drainage

Collected water:

\[
\dot m_{collected}
=
\eta_{drain}\dot m_{condensed}
\]

Uncollected water shall be reported as retained film, carryover or drainage loss. It shall not disappear.

# 14. Dynamic thermal state

A dynamic surface or plate model may use:

\[
C_{cond}\frac{dT_s}{dt}
=
\dot Q_{load}
-
\dot Q_{cooling}
-
UA_{env}(T_s-T_{amb})
\]

The first implementation may receive the effective surface temperature externally and introduce internal dynamics later.

# 15. Pressure drop

Initial model:

\[
\Delta p
=
\Delta p_{ref}
\left(
\frac{\dot V}{\dot V_{ref}}
\right)^2
\]

# 16. Proposed result

```csharp
public sealed record CondenserStepResult
{
    public required MoistAirState OutletAir { get; init; }
    public required double CondensedWaterRateKgPerSecond { get; init; }
    public required double CollectedWaterRateKgPerSecond { get; init; }
    public required double SensibleCoolingPowerW { get; init; }
    public required double LatentCoolingPowerW { get; init; }
    public required double TotalCoolingPowerW { get; init; }
    public required double PressureDropPa { get; init; }
    public required ConservationBalance Balance { get; init; }
    public required IReadOnlyCollection<SimulationDiagnostic> Diagnostics { get; init; }
}
```

# 17. Solver sequence

1. Validate inlet state and parameters.
2. Calculate inlet dew point.
3. Determine condensation possibility.
4. Calculate thermodynamic ideal outlet.
5. Apply heat-transfer and mass-transfer limits.
6. Calculate the cooling required for the candidate outlet.
7. If required power exceeds available power, solve a constrained outlet state.
8. Calculate condensate and drainage.
9. Create outlet `MoistAirState`.
10. Calculate pressure drop and residuals.
11. Return proposed result without mutating committed state.

# 18. Diagnostics

```text
NoCondensationPossible
SurfaceAboveDewPoint
CoolingPowerLimited
HeatTransferLimited
MassTransferLimited
DrainageLossSignificant
OutletSupersaturated
PressureDropExceedsFanCapability
WaterBalanceResidualExceeded
EnergyBalanceResidualExceeded
NumericalSolverFailed
```

# 19. Required tests

- no condensation above dew point;
- onset of condensation at the dew point;
- thermodynamic upper bound;
- cooling-power-limited case;
- heat-transfer-limited case;
- mass-transfer-limited case;
- drainage efficiency;
- water conservation;
- energy conservation;
- deterministic execution;
- timestep sensitivity for dynamic fidelity.

# 20. Acceptance criteria

The module is accepted when:

1. condensate never exceeds inlet vapor;
2. outlet air is psychrometrically consistent;
3. latent heat is included;
4. cooling power is not exceeded;
5. drainage losses are explicit;
6. dry air, water and energy balances close within tolerance;
7. the same inputs produce identical results across hosts.

---

**End of Document**
