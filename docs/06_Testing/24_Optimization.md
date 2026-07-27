# ThermoCore
## 24_Optimization.md

**Version:** 1.0
**Status:** ReadyForImplementation

# Purpose

Defines optimization strategies for maximizing water production while minimizing energy consumption.

## Objective examples

- Max liters/day
- Min Wh/liter
- Max solar utilization
- Min battery cycling
- Max condenser effectiveness

## Optimization variables

- Airflow
- Recirculation ratio
- Peltier power
- Collector area
- Condenser area
- Silica-gel mass

## Constraints

- Battery limits
- Thermal limits
- Physical parameter bounds
- Stable numerical convergence

## Supported algorithms

- Grid search
- Random search
- Nelder–Mead
- Differential Evolution (future)
- Bayesian optimization (future)

## Outputs

- Best parameter combination
- Pareto front (future)
- Sensitivity ranking
- Optimization report

## Acceptance criteria

Optimization must always produce reproducible results using the same inputs and random seed (where applicable).
