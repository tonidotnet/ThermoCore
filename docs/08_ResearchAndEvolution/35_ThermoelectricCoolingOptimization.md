**Project:** ThermoCore  
**Track:** AWG Cooling Research & Evolution  
**Status:** Proposed / Ready for implementation planning  
**Baseline:** Existing ThermoCore `main` with Core, AWG, API/Web, calibration/optimization and SolarAirHeater app already implemented.

---

# Thermoelectric Cooling Optimization

## Objective

Optimize:

```text
maximum collected water / electrical energy
```

not the lowest possible cold-side temperature.

## Dew-point tracking

Target:

```text
Tsurface,target = Tdewpoint,in - 2...5 K
```

The controller should request the lowest TEC drive that can maintain the target inside safety limits.

## Manufacturer TEC profiles

The implementation shall support named hardware profiles instead of hard-coded generic TEC values.

Suggested initial profiles:

```text
Generic TEC1-12706
Industrial 40x40 mm TEC profile
Measured commercial Peltier dehumidifier profile
```

## Sweep variables

```text
Hot-side temperature
TEC current
Airflow
Dew-point margin
Hot-side thermal resistance
Cold-side thermal resistance
```

## Required output channels

```text
cooling.tec.current
cooling.tec.voltage
cooling.tec.power
cooling.tec.qCold
cooling.tec.qHot
cooling.tec.cop
cooling.system.cop
cooling.surface.temperature
cooling.dewPointMargin
water.coolingPlant.collectedRate
```

## Key hypothesis

A sorbent-regeneration stream may have sufficiently high dew point to reduce TEC temperature lift and improve real system COP.

This is a testable hypothesis, not an assumption.
