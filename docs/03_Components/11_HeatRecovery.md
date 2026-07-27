# ThermoCore
## 11_HeatRecovery.md

**Version:** 1.1  
**Status:** ReadyForImplementation  
**Document Type:** Engineering and implementation specification  
**Internal units:** SI  
**Primary implementation language:** C#

---


# 1. Purpose

This document defines sensible heat recovery and the extension points for future latent recovery in ThermoCore.AWG.

The MVP shall use a two-stream, no-mixing sensible heat exchanger. Moisture transfer is disabled unless an explicit membrane or enthalpy-exchanger model is selected.

# 2. Responsibilities

- accept hot and cold moist-air inlet streams;
- conserve dry air and water independently on both sides;
- calculate maximum and actual sensible heat transfer;
- calculate outlet states;
- report hot- and cold-side pressure drops;
- support bypass and effectiveness control;
- detect temperature crossing, condensation and frost risks;
- expose energy residuals.

# 3. Ports

```text
HotMoistAirIn
HotMoistAirOut
ColdMoistAirIn
ColdMoistAirOut
AmbientHeatOut
ControlIn
```

# 4. Capacity rates

For each moist-air stream:

\[
C
=
\dot m_{da}(c_{p,da}+Wc_{p,v})
\]

\[
C_{min}=\min(C_h,C_c)
\]

\[
C_{max}=\max(C_h,C_c)
\]

\[
C_r=C_{min}/C_{max}
\]

# 5. Maximum heat transfer

\[
\dot Q_{max}
=
C_{min}(T_{h,in}-T_{c,in})
\]

If \(T_{h,in}\le T_{c,in}\), normal heat recovery is zero unless reverse operation is explicitly allowed.

# 6. Effectiveness model

\[
\dot Q_{actual}
=
\varepsilon\dot Q_{max}
\]

\[
T_{h,out}
=
T_{h,in}
-
\frac{\dot Q_{actual}}{C_h}
\]

\[
T_{c,out}
=
T_{c,in}
+
\frac{\dot Q_{actual}}{C_c}
\]

The model shall prevent unphysical temperature crossing.

# 7. Counter-flow effectiveness–NTU

\[
NTU=\frac{UA}{C_{min}}
\]

For \(C_r\ne1\):

\[
\varepsilon
=
\frac{1-\exp[-NTU(1-C_r)]}
{1-C_r\exp[-NTU(1-C_r)]}
\]

For \(C_r=1\):

\[
\varepsilon=\frac{NTU}{1+NTU}
\]

# 8. Humidity behavior

In sensible-only mode:

\[
W_{h,out}=W_{h,in}
\]

\[
W_{c,out}=W_{c,in}
\]

No water shall cross the exchanger wall.

If a hot-side surface falls below the hot-stream dew point, condensation risk shall be reported. An explicit condensing heat-recovery fidelity level is required before removing vapor.

# 9. Bypass

For bypass fraction \(b\):

\[
0\le b\le1
\]

The effective outlet is the dry-air-mass-weighted mixture of bypassed and exchanged streams. Bypass may be used to prevent overcooling or condensation.

# 10. Pressure drop

Calculate hot and cold sides independently:

\[
\Delta p_i
=
\Delta p_{ref,i}
\left(
\frac{\dot V_i}{\dot V_{ref,i}}
\right)^2
\]

# 11. Configuration

```csharp
public sealed record HeatRecoveryParameters
{
    public required HeatRecoveryModelType ModelType { get; init; }
    public required double EffectivenessFraction { get; init; }
    public required double UaWPerK { get; init; }
    public required double HotReferencePressureDropPa { get; init; }
    public required double ColdReferencePressureDropPa { get; init; }
    public required double HotReferenceFlowM3PerSecond { get; init; }
    public required double ColdReferenceFlowM3PerSecond { get; init; }
    public required double HeatLeakCoefficientWPerK { get; init; }
    public bool EnableCondensationRiskDiagnostics { get; init; } = true;
}
```

# 12. Result

```csharp
public sealed record HeatRecoveryStepResult
{
    public required MoistAirState HotOutlet { get; init; }
    public required MoistAirState ColdOutlet { get; init; }
    public required double RecoveredHeatW { get; init; }
    public required double HotPressureDropPa { get; init; }
    public required double ColdPressureDropPa { get; init; }
    public required double EffectivenessFraction { get; init; }
    public required ConservationBalance Balance { get; init; }
    public required IReadOnlyCollection<SimulationDiagnostic> Diagnostics { get; init; }
}
```

# 13. AWG integration

Recommended placement:

```text
Condenser exhaust or regeneration exhaust
        ↓ hot side

Fresh or recirculated inlet air
        ↓ cold side
```

Heat-recovery output shall not be counted again by the solar collector or Peltier hot-side exchanger.

# 14. Required tests

- equal inlet temperatures;
- known-effectiveness case;
- counter-flow NTU reference cases;
- unequal capacity rates;
- bypass;
- no humidity change in sensible-only mode;
- pressure-drop scaling;
- temperature-crossing prevention;
- energy conservation;
- deterministic execution.

# 15. Acceptance criteria

- no hidden water transfer in MVP mode;
- energy transferred from hot side equals energy received by cold side plus explicit loss;
- outlet states remain physically valid;
- pressure drops are independent and explicit;
- condensation risk is diagnosed rather than silently resolved.

---

**End of Document**
