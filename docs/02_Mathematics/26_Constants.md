# ThermoCore
## 26_Constants.md

**Version:** 1.0  
**Status:** ReadyForImplementation  
**Document Type:** Engineering and implementation specification  
**Internal units:** SI  
**Primary implementation language:** C#

---


# 1. Purpose

This document defines the authoritative shared physical constants used by ThermoCore.

Constants shall exist in one code location with unit, value, source classification and reference conditions. Component-specific empirical parameters are not shared constants.

# 2. Required metadata

```csharp
public sealed record PhysicalConstantDefinition
{
    public required string Id { get; init; }
    public required string Symbol { get; init; }
    public required double Value { get; init; }
    public required string Unit { get; init; }
    public required string Source { get; init; }
    public required string SourceClassification { get; init; }
    public string? Notes { get; init; }
}
```

# 3. Core constants

| ID | Symbol | Value | SI unit | Use |
|---|---:|---:|---|---|
| CelsiusOffset | — | 273.15 | K | °C–K conversion |
| StandardAtmosphericPressure | \(p_0\) | 101325 | Pa | default/reference pressure |
| UniversalGasConstant | \(R\) | 8.31446261815324 | J/(mol·K) | thermodynamics |
| DryAirGasConstant | \(R_{da}\) | 287.055 | J/(kg·K) | moist-air ideal gas |
| WaterVaporGasConstant | \(R_v\) | 461.52 | J/(kg·K) | water-vapor ideal gas |
| MolecularMassRatio | \(\epsilon\) | 0.621945 | dimensionless | humidity-ratio equations |
| DryAirSpecificHeat | \(c_{p,da}\) | 1006 | J/(kg·K) | initial HVAC approximation |
| WaterVaporSpecificHeat | \(c_{p,v}\) | 1860 | J/(kg·K) | initial HVAC approximation |
| LiquidWaterSpecificHeat | \(c_{p,l}\) | 4180 | J/(kg·K) | initial engineering approximation |
| ReferenceVaporizationEnthalpy | \(h_{fg,0}\) | 2501000 | J/kg | moist-air enthalpy reference |
| StefanBoltzmannConstant | \(\sigma\) | 5.670374419e-8 | W/(m²·K⁴) | thermal radiation |
| StandardGravity | \(g\) | 9.80665 | m/s² | potential energy |
| WaterDensityReference | \(\rho_w\) | 997 | kg/m³ | display conversion near room temperature |

# 4. Accuracy classes

A constant or property approximation shall be marked as:

```text
ExactDefinition
InternationalRecommended
EngineeringReference
TemperatureDependentApproximation
CalibrationParameter
```

`CelsiusOffset`, `StandardGravity` and SI definitions are treated differently from temperature-dependent properties such as heat capacity.

# 5. Heat capacities

The fixed heat capacities above are acceptable only for the initial documented operating range. Higher-fidelity providers may replace them with functions of temperature and pressure.

Code shall not name a temperature-dependent approximation simply `Exact...`.

# 6. Latent heat

The fixed reference value is used by the initial moist-air enthalpy convention. Condenser calculations may use a temperature-dependent latent-heat provider.

The reference enthalpy constant and the operating latent heat must not be confused.

# 7. Water density

Water volume reporting:

\[
V=m/\rho
\]

For rough AWG reporting, 1 kg may be displayed approximately as 1 L. Precise volume shall use a temperature-dependent density model or state the approximation.

# 8. Prohibited constants

Do not place the following in this shared file:

- collector efficiency;
- Peltier COP;
- silica-gel capacity;
- fan efficiency;
- battery efficiency;
- heat-transfer coefficient;
- pressure-drop coefficient;
- empirical calibration factor.

These are parameters with source and validity range.

# 9. Code organization

Recommended:

```csharp
public static class PhysicalConstants
{
    public const double CelsiusOffsetK = 273.15;
    public const double StandardAtmosphericPressurePa = 101_325.0;
    public const double UniversalGasConstantJPerMolK = 8.31446261815324;
    public const double DryAirGasConstantJPerKgK = 287.055;
    public const double WaterVaporGasConstantJPerKgK = 461.52;
    public const double MolecularMassRatio = 0.621945;
    public const double StefanBoltzmannConstantWPerM2K4 = 5.670374419e-8;
    public const double StandardGravityMPerS2 = 9.80665;
}
```

Approximate heat capacities may be grouped separately:

```csharp
public static class ReferenceThermophysicalProperties
{
    public const double DryAirSpecificHeatJPerKgK = 1006.0;
    public const double WaterVaporSpecificHeatJPerKgK = 1860.0;
    public const double LiquidWaterSpecificHeatJPerKgK = 4180.0;
    public const double ReferenceVaporizationEnthalpyJPerKg = 2_501_000.0;
}
```

# 10. Validation

- every constant shall be finite;
- positive quantities shall be positive;
- unit-bearing identifier shall match metadata unit;
- constants used in equations shall have tests;
- source changes require documentation-version update.

# 11. Required tests

- Celsius/Kelvin reference conversion;
- humidity-ratio molecular-mass constant consistency;
- Stefan–Boltzmann dimensional use;
- JSON or metadata export if implemented;
- no duplicate IDs.

# 12. Acceptance criteria

- one authoritative definition per shared constant;
- every definition includes unit and source classification;
- engineering parameters remain outside shared constants;
- values are consistent across Console, API and Web;
- no culture-dependent formatting or parsing affects values.

---

**End of Document**
