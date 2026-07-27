# ThermoCore
## 27_Units.md

**Version:** 1.0  
**Status:** ReadyForImplementation  
**Document Type:** Engineering and implementation specification  
**Internal units:** SI  
**Primary implementation language:** C#

---


# 1. Purpose

This document defines the unit policy, conversion boundaries and naming conventions of ThermoCore.

ThermoCore.Core shall calculate in SI units. UI, API and import/export layers may use user-friendly units, but conversion shall occur explicitly at boundaries.

# 2. Internal units

| Quantity | Internal unit |
|---|---|
| Absolute temperature | K |
| Temperature difference | K |
| Pressure | Pa |
| Length | m |
| Area | m² |
| Volume | m³ |
| Time | s |
| Mass | kg |
| Dry-air mass flow | kg/s |
| Water mass flow | kg/s |
| Volumetric flow | m³/s |
| Energy | J |
| Power/heat flow | W |
| Specific enthalpy | J/kg dry air unless otherwise named |
| Specific heat | J/(kg·K) |
| Thermal conductance | W/K |
| Heat-transfer coefficient | W/(m²·K) |
| Thermal resistance | K/W |
| Relative humidity | fraction 0–1 |
| Angle | rad |
| Electrical voltage | V |
| Electrical current | A |
| Resistance | Ω |

# 3. Boundary/display units

Permitted examples:

```text
°C
%
kPa
hPa
m³/h
L/min
g
mL
L
Wh
kWh
degrees
```

The unit must be explicit in DTO property names, headers or metadata.

# 4. Naming convention

Examples:

```csharp
TemperatureK
TemperatureC
PressurePa
VolumetricFlowM3PerSecond
VolumetricFlowM3PerHour
MassFlowKgPerSecond
EnergyJ
EnergyWh
PowerW
RelativeHumidityFraction
RelativeHumidityPercent
AngleRadians
AngleDegrees
```

Avoid unitless ambiguous names such as `Temperature`, `Flow`, `Humidity` and `Energy`.

# 5. Temperature conversion

\[
T_K=T_C+273.15
\]

\[
T_C=T_K-273.15
\]

A temperature difference has the same numeric magnitude in kelvin and Celsius, but identifiers shall still state the expected unit.

# 6. Relative humidity

Core:

```text
0.50 = 50%
```

Conversions:

\[
RH_{fraction}=RH_{\%}/100
\]

\[
RH_{\%}=100RH_{fraction}
\]

# 7. Airflow

\[
\dot V_{m^3/s}=\dot V_{m^3/h}/3600
\]

\[
\dot V_{m^3/h}=3600\dot V_{m^3/s}
\]

Volumetric flow shall always be associated with the state at which volume is measured.

# 8. Energy

\[
1 Wh=3600 J
\]

\[
1 kWh=3.6\times10^6 J
\]

Power integrated over timestep:

\[
\Delta E=P\Delta t
\]

with seconds and watts producing joules.

# 9. Water volume

\[
V=m/\rho
\]

Do not universally equate kilograms and liters without documenting the density approximation.

# 10. Pressure

\[
1 kPa=1000 Pa
\]

\[
1 hPa=100 Pa
\]

\[
1 bar=100000 Pa
\]

Gauge and absolute pressure shall have different names:

```csharp
AbsolutePressurePa
GaugePressurePa
PressureDropPa
```

Psychrometric total pressure is absolute.

# 11. Angles

\[
\theta_{rad}=\theta_{deg}\pi/180
\]

\[
\theta_{deg}=\theta_{rad}180/\pi
\]

Core geometry uses radians.

# 12. Typed units strategy

Initial implementation may use `double` with unit-bearing names.

A future strongly typed unit library may be introduced only if it:

- remains cross-platform;
- does not create significant serialization friction;
- has acceptable performance;
- does not obscure mathematical equations;
- is applied consistently.

Do not mix an incomplete typed-unit system with raw ambiguous doubles.

# 13. Conversion layer

Recommended static service:

```csharp
public static class UnitConversions
{
    public static double CelsiusToKelvin(double valueC);
    public static double KelvinToCelsius(double valueK);
    public static double PercentToFraction(double valuePercent);
    public static double FractionToPercent(double valueFraction);
    public static double CubicMetersPerHourToPerSecond(double value);
    public static double CubicMetersPerSecondToPerHour(double value);
    public static double WattHoursToJoules(double valueWh);
    public static double JoulesToWattHours(double valueJ);
    public static double DegreesToRadians(double valueDegrees);
    public static double RadiansToDegrees(double valueRadians);
}
```

# 14. API policy

Public API DTOs may use user-friendly units when property names are explicit.

Example:

```csharp
public sealed record AmbientAirRequest
{
    public required double TemperatureC { get; init; }
    public required double RelativeHumidityPercent { get; init; }
    public double AbsolutePressurePa { get; init; } = 101325.0;
    public required double VolumetricFlowM3PerHour { get; init; }
}
```

Map to Core before physical calculation.

# 15. JSON and CSV

- include unit suffix in JSON property name or schema metadata;
- include unit in CSV column header;
- use invariant numeric representation for machine files;
- do not round authoritative values for storage;
- display rounding belongs to presentation.

# 16. Validation

Every conversion method shall:

- reject non-finite values;
- validate physical domains where appropriate;
- not reject valid negative Celsius temperatures;
- reject kelvin below zero;
- reject fractions outside expected range unless the method is purely arithmetic and validation occurs separately.

# 17. Required tests

- Celsius/Kelvin round trip;
- percent/fraction round trip;
- m³/h and m³/s round trip;
- J and Wh round trip;
- degrees/radians round trip;
- absolute-zero validation;
- invariant serialization;
- property-name unit audit where automated.

# 18. Acceptance criteria

- Core calculations use SI;
- every ambiguous public physical quantity exposes its unit;
- conversions occur only at boundaries or dedicated conversion services;
- no hidden conversion exists inside component equations;
- API, Web and Console produce equivalent Core values from equivalent user inputs.

---

**End of Document**
