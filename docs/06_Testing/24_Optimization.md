# ThermoCore
## 24_Optimization.md

**Version:** 1.1  
**Status:** Implemented  
**Document Type:** Optimization specification  
**Applies To:** ThermoCore.AWG.Optimization  
**Related tasks:** OPT-001 to OPT-005

---

# 1. Purpose

Defines optimization strategies for maximizing water production while minimizing energy consumption for the AWG reference application.

Optimization tooling shall call AWG simulation services and shall not invent component physics.

---

# 2. Objective examples

- Max liters/day
- Min Wh/liter
- Max solar utilization (later)
- Min battery cycling (later)
- Max condenser effectiveness (later)

MVP objective helpers:

```text
ThermoCore.AWG.Optimization.AwgOptimizationObjectives.LitersPerDay
ThermoCore.AWG.Optimization.AwgOptimizationObjectives.WattHoursPerLiter
```

Notes:

- Liters/day extrapolates collected tank water over the simulated duration to 24 h (1 kg ≈ 1 L).
- Wh/liter uses final bus power as a constant-power proxy when the electrical subsystem is enabled.

---

# 3. Optimization variables

MVP sweeps reuse calibratable parameter ids (CAL-006 catalog):

```text
condenser.bypassFactor
condenser.drainageEfficiency
silicaGel.referenceMassTransferCoefficientPerSecond
solarCollector.overallLossCoefficientWPerM2K
heatRecovery.effectivenessFraction
```

Broader design variables (later):

- Airflow
- Recirculation ratio
- Peltier power
- Collector area
- Condenser area
- Silica-gel mass

---

# 4. Constraints

- Battery limits
- Thermal limits
- Physical parameter bounds (enforced by configuration `Validate`)
- Stable numerical convergence (failed runs marked unsuccessful)

---

# 5. Supported algorithms

| Algorithm | Status |
|---|---|
| Grid search (Cartesian sweep, ≤3 axes) | Implemented (`AwgParameterSweepRunner`) |
| One-at-a-time local sensitivity | Implemented (`AwgSensitivityAnalysisRunner`) |
| Bounded coordinate descent (calibration) | Implemented (CAL-006) |
| Random search | Implemented (`AwgRandomSearchRunner`) |
| Nelder–Mead | Later |
| Differential Evolution | Future |
| Bayesian optimization | Future |

---

# 6. Parameter sweep usage

```bash
dotnet run --project src/ThermoCore.Console -- sweep \
  --params condenser.bypassFactor=0.10,0.20,0.30 \
  --duration 10 --dt 1
```

Multiple `--params` axes build a Cartesian product (MVP max 3 axes).

---

# 6b. Sensitivity analysis usage

```bash
dotnet run --project src/ThermoCore.Console -- sensitivity \
  --params condenser.bypassFactor,condenser.drainageEfficiency \
  --perturbation 0.10 --duration 10 --dt 1
```

For each selected calibratable parameter, the runner evaluates baseline and ± relative half-steps (clamped to catalog bounds), then ranks by absolute liters/day elasticity:

```text
ε = ((y_high - y_low) / y_baseline) / ((x_high - x_low) / x_baseline)
```

When baseline liters/day is ~0, elasticity falls back to `(y_high - y_low) / ((x_high - x_low) / x_baseline)` so short dry runs still produce a ranking.

---

# 7. Outputs

- Best parameter combination for liters/day
- Best Wh/liter when electrical proxy is available
- Full point table (console)
- Sensitivity ranking by |liters/day elasticity| (OPT-003)
- Bi-objective Pareto front max L/day vs min Wh/L (`AwgParetoFront`, OPT-006)

---

# 8. Acceptance criteria

1. Same configuration, axes, and timing produce the same sweep ranking.
2. Invalid parameter combinations fail the point without aborting the sweep.
3. Liters/day and optional Wh/liter are reported for successful points.
4. When Wh/liter is available, the non-dominated Pareto set for max L/day and min Wh/L is reported.

---

**End of Document**
