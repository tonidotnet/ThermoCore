# ThermoCore Physics Context

## Non-negotiable rules

- Use SI units internally.
- Conserve dry air, water, energy and electrical energy.
- Never invent physical constants or empirical parameters.
- Keep parameter provenance.
- Separate equilibrium from kinetics.
- Separate component physics from control logic.
- Never assign outlet relative humidity directly when it should be derived.
- Never create condensed water without latent heat.
- Never create or destroy stored water.
- Never treat calibration as validation.

## Moist-air conventions

Authoritative variables should usually be:

- dry-bulb temperature;
- absolute pressure;
- humidity ratio;
- dry-air mass flow.

Derived properties include:

- relative humidity;
- dew point;
- enthalpy;
- density;
- vapor pressure.

## Component balance pattern

For every timestep:

```text
Input
-
Output
-
Storage change
=
Residual
```

## Fidelity

Every model shall declare its fidelity level and validity range.

A simpler validated model is preferable to a complex undocumented model.
