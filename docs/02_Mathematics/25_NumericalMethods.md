# ThermoCore
## 25_NumericalMethods.md

**Version:** 1.1  
**Status:** ReadyForImplementation  
**Document Type:** Engineering and implementation specification  
**Internal units:** SI  
**Primary implementation language:** C#

---


# 1. Purpose

This document defines numerical algorithms, tolerances, solver contracts and stability rules used by ThermoCore.

Physical equations belong to component specifications. This document specifies how those equations are solved.

# 2. Principles

- prefer closed-form solutions where available;
- prefer bracketed algorithms when reliability is more important than speed;
- never silently continue after non-convergence;
- use absolute and relative tolerances together;
- keep algorithms deterministic;
- separate solver diagnostics from physical diagnostics;
- do not clamp an unconverged result into apparent validity.

# 3. Floating-point policy

- use IEEE-754 `double`;
- reject NaN and infinity at public boundaries;
- do not use exact equality for calculated values;
- protect denominators;
- avoid subtractive cancellation where practical;
- use invariant ordering when reduction order can affect reproducibility.

# 4. Tolerances

```csharp
public sealed record NumericalTolerances
{
    public double Absolute { get; init; } = 1e-10;
    public double Relative { get; init; } = 1e-7;
    public double TemperatureK { get; init; } = 1e-4;
    public double PressurePa { get; init; } = 0.1;
    public double MassKg { get; init; } = 1e-9;
    public double MassFlowKgPerSecond { get; init; } = 1e-9;
    public double EnergyJ { get; init; } = 1e-5;
    public double PowerW { get; init; } = 1e-5;
    public int MaximumIterations { get; init; } = 100;
}
```

Defaults are provisional and shall be validated against representative scales.

# 5. Approximate equality

\[
|a-b|
\le
\varepsilon_{abs}
+
\varepsilon_{rel}\max(|a|,|b|)
\]

Use quantity-specific absolute tolerance.

# 6. Root-finding selection

Preferred order:

1. analytical inverse;
2. monotonic bisection or Brent-type bracketed solver;
3. safeguarded Newton;
4. secant only when a bracket or derivative is unavailable.

Pure Newton iteration shall not be the only fallback for safety-critical property calculations.

# 7. Bisection

Requirements:

- finite lower and upper bounds;
- opposite signs or monotonic target bracketing;
- maximum iterations;
- residual and interval-width stopping criteria.

Stop when either:

\[
|f(x)|\le\varepsilon_f
\]

or:

\[
|x_h-x_l|\le\varepsilon_x
\]

# 8. Safeguarded Newton

Newton proposal:

\[
x_{n+1}=x_n-\frac{f(x_n)}{f'(x_n)}
\]

Reject proposal and use bisection when:

- derivative is too small;
- proposal leaves the bracket;
- result is non-finite;
- residual worsens repeatedly.

# 9. Fixed-point iteration

\[
z^{k+1}=F(z^k)
\]

Relaxed update:

\[
z^{k+1}
=
\lambda F(z^k)+(1-\lambda)z^k
\]

\[
0<\lambda\le1
\]

Use for recirculation and coupled component loops when the mapping is sufficiently contractive.

# 10. Fixed-point convergence

For every selected convergence variable:

\[
|x^{k+1}-x^k|
\le
\varepsilon_{abs}
+
\varepsilon_{rel}\max(|x^{k+1}|,|x^k|)
\]

Also validate physical and conservation residuals. Variable stagnation alone is not sufficient if balances remain invalid.

# 11. Divergence and stagnation detection

Report failure when:

- maximum iterations reached;
- residual increases for a configured number of iterations;
- values become non-finite;
- oscillation persists;
- changes become tiny but residual remains high;
- state leaves its physical domain.

# 12. Explicit Euler

\[
x_{n+1}=x_n+\Delta t f(t_n,x_n)
\]

Allowed for slow, non-stiff states only after timestep-sensitivity tests.

# 13. Exact first-order update

For:

\[
\frac{dx}{dt}=k(x_{eq}-x)
\]

use:

\[
x_{n+1}
=
x_{eq}+(x_n-x_{eq})e^{-k\Delta t}
\]

This is preferred for LDF adsorption under constant coefficients within a timestep.

# 14. Semi-implicit Euler

Use when a dominant linear loss term would make explicit Euler unstable.

Generic form:

\[
C\frac{T_{n+1}-T_n}{\Delta t}
=
Q_{source}
-
UA(T_{n+1}-T_b)
\]

Solve algebraically for \(T_{n+1}\).

# 15. Runge–Kutta

RK4 may be introduced as an optional integrator for non-stiff independent state equations. It shall not be used to hide algebraic coupling or conservation defects.

# 16. Timestep selection

Each dynamic component should expose an estimated characteristic time:

\[
\tau=C/UA
\]

Explicit Euler guideline:

\[
\Delta t\le\tau/10
\]

The engine may use global timesteps with component internal substeps.

# 17. Internal substepping

A component may subdivide a timestep when:

- stability criteria require it;
- a state boundary would be crossed;
- a phase transition begins;
- the external timestep is much larger than component dynamics.

Substeps shall preserve the external-step balance.

# 18. Event handling

Events include:

```text
Condensation onset
Battery SOC bound
Tank capacity
Adsorbent loading bound
Peltier protection
Operating-mode transition
```

When practical, locate event time within the timestep instead of overshooting and clamping.

# 19. Interpolation

Initial supported methods:

- piecewise linear one-dimensional;
- bilinear two-dimensional;
- monotonic table interpolation where required.

Rules:

- sorted axes;
- duplicate-axis rejection;
- explicit extrapolation policy;
- metadata with source and range;
- no silent extrapolation.

# 20. Tabular extrapolation

Allowed policies:

```text
Reject
ClampToBoundary
LinearWithDiagnostic
ModelSpecific
```

`ClampToBoundary` is a data-domain policy, not a conservation correction.

# 21. Conservation residual evaluation

Numerical convergence and conservation validation are independent.

A timestep may be numerically converged but physically invalid. It shall not commit when required balances exceed configured tolerances.

# 22. Solver contracts

```csharp
public interface IRootSolver
{
    RootSolveResult Solve(
        Func<double, double> function,
        double lowerBound,
        double upperBound,
        RootSolverOptions options);
}

public interface IFixedPointSolver<TState>
{
    FixedPointResult<TState> Solve(
        TState initialState,
        Func<TState, TState> iteration,
        IConvergenceMetric<TState> metric,
        FixedPointOptions options);
}
```

# 23. Result metadata

Every solver result shall contain:

```text
Converged
Iterations
Final residual
Final interval or state change
Termination reason
Warnings
```

Wall-clock duration may be included for performance diagnostics but shall not affect physics.

# 24. Determinism

- fixed component order;
- fixed convergence-variable order;
- deterministic reductions;
- no unseeded randomness;
- no tolerance based on machine-local timing;
- same configuration and initial state shall produce the same result.

# 25. Required tests

- bisection root;
- endpoint root;
- invalid bracket;
- safeguarded Newton fallback;
- fixed-point convergence;
- oscillation detection;
- divergence detection;
- exact LDF update;
- explicit Euler reference problem;
- semi-implicit stable case;
- timestep sensitivity;
- interpolation and extrapolation policies;
- non-finite input rejection;
- deterministic repeatability.

# 26. Acceptance criteria

- common solver APIs are host-independent;
- every failure is explicit;
- algorithms preserve physical bounds through equations or explicit event handling;
- no solver silently reports success after reaching iteration limit;
- defaults are centralized and configurable;
- component documentation states which numerical algorithm it requires.

---

**End of Document**
