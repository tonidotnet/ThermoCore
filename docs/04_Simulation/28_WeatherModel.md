# ThermoCore
## 28_WeatherModel.md

**Version:** 1.0  
**Status:** Implemented  
**Document Type:** Weather and solar-environment input specification  
**Applies To:** ThermoCore.Core environment contracts and ThermoCore.AWG scenarios  
**Internal units:** SI

---

# 1. Purpose

This document defines weather and environmental inputs used by ThermoCore simulations.

The weather model shall provide time-indexed:

- dry-bulb temperature;
- relative humidity or humidity ratio;
- absolute pressure;
- solar irradiance;
- wind speed;
- optional sky temperature;
- optional ground temperature;
- optional precipitation and cloud metadata.

It shall not fabricate precision that is absent from source data.

# 2. Architectural placement

Generic contracts:

```text
ThermoCore.Core.Environment
```

Weather-file parsing and external providers may live in application or infrastructure layers.

# 3. Weather state

```csharp
public sealed record WeatherState
{
    public required DateTimeOffset TimestampUtc { get; init; }

    public required double AmbientTemperatureK { get; init; }

    public required double RelativeHumidityFraction { get; init; }

    public required double AbsolutePressurePa { get; init; }

    public required double WindSpeedMPerSecond { get; init; }

    public required double GlobalHorizontalIrradianceWPerM2 { get; init; }

    public double? DirectNormalIrradianceWPerM2 { get; init; }

    public double? DiffuseHorizontalIrradianceWPerM2 { get; init; }

    public double? SkyTemperatureK { get; init; }

    public double? GroundTemperatureK { get; init; }

    public required WeatherQualityFlags QualityFlags { get; init; }
}
```

# 4. Time basis

Every weather record shall include an explicit UTC offset or UTC timestamp.

Local timestamps without timezone are invalid for solar-position calculations.

# 5. Pressure

Absolute pressure is required for psychrometrics.

If source pressure is missing, the scenario may use:

- configured site pressure;
- altitude-derived estimate;
- standard atmosphere with a diagnostic.

The selected method shall be stored in metadata.

# 6. Relative humidity validation

\[
0\le RH\le1
\]

Values outside source tolerance shall be rejected or corrected only by an explicit import-cleaning policy with diagnostics.

# 7. Solar variables

Preferred source set:

```text
GHI
DNI
DHI
```

If only GHI is available, direct and diffuse decomposition may be estimated by a documented provider. Estimated values shall be flagged.

# 8. Plane-of-array irradiance

A solar geometry service shall calculate:

\[
G_{poa}
=
G_{beam,poa}
+
G_{diffuse,poa}
+
G_{ground,poa}
\]

Inputs include:

```text
Latitude
Longitude
Timestamp
Panel tilt
Panel azimuth
DNI
DHI
GHI
Ground albedo
```

# 9. Solar position

The weather component may consume an external solar-position provider.

Required outputs:

```text
Solar zenith
Solar azimuth
Incidence angle
Sun above horizon
```

Astronomical calculations shall not be duplicated in panel and collector components.

# 10. Wind

Wind speed influences:

- collector losses;
- PV cooling;
- enclosure heat loss;
- optional fan inlet conditions.

Wind direction may be stored for future use but is not required for MVP.

# 11. Sky temperature

When unavailable, an empirical estimate may be used only through a named model.

A fixed sky-temperature offset is acceptable for exploratory studies if clearly marked as an engineering approximation.

# 12. Weather series

```csharp
public sealed record WeatherTimeSeries
{
    public required IReadOnlyList<WeatherState> States { get; init; }

    public required WeatherSourceMetadata Metadata { get; init; }
}
```

# 13. Metadata

```csharp
public sealed record WeatherSourceMetadata
{
    public required string SourceName { get; init; }

    public required string SourceVersion { get; init; }

    public required string LocationName { get; init; }

    public required double LatitudeDegrees { get; init; }

    public required double LongitudeDegrees { get; init; }

    public required double ElevationM { get; init; }

    public required string TimezoneId { get; init; }

    public required string DataLicense { get; init; }

    public required IReadOnlyCollection<string> DerivedFields { get; init; }
}
```

# 14. Interpolation

Simulation timesteps may differ from weather resolution.

Default interpolation:

```text
Temperature: linear
Pressure: linear
Relative humidity: linear with post-validation
Wind speed: linear or step, configurable
Irradiance: linear between records, zero below horizon
Categorical quality flags: conservative union
```

# 15. Humidity interpolation

Interpolating relative humidity directly may not conserve vapor content.

Preferred higher-fidelity approach:

1. convert source records to vapor pressure or humidity ratio;
2. interpolate the conserved moisture variable;
3. derive RH at interpolated temperature and pressure.

# 16. Missing data

Policies:

```text
RejectSeries
InterpolateShortGap
UseConfiguredFallback
MarkUnavailable
```

Every repaired gap shall be recorded.

# 17. Short-gap interpolation

A maximum gap duration shall be configured.

Do not interpolate long missing solar periods across sunrise, sunset or weather transitions without explicit policy.

# 18. Synthetic weather

Synthetic scenarios may be used for testing.

Examples:

```text
Constant ambient state
Sinusoidal daily temperature
Clear-sky irradiance
Step humidity event
Battery stress day
```

Synthetic data shall be marked and never confused with measured weather.

# 19. CSV input schema

Recommended columns:

```text
timestamp
temperature_c
relative_humidity_percent
pressure_pa
wind_speed_m_s
ghi_w_m2
dni_w_m2
dhi_w_m2
```

Optional:

```text
sky_temperature_c
ground_temperature_c
quality_flag
```

# 20. Import validation

Validate:

- monotonic timestamps;
- duplicate timestamps;
- valid timezone;
- finite values;
- physical ranges;
- irradiance non-negative;
- pressure plausible for configured site;
- missing-field policy;
- unit headers.

# 21. Weather provider interface

```csharp
public interface IWeatherProvider
{
    WeatherState GetState(DateTimeOffset timestampUtc);

    WeatherSourceMetadata Metadata { get; }
}
```

# 22. Caching

A provider may cache immutable weather records.

Caching shall not change interpolation or numerical results.

# 23. Reproducibility

Every run shall store:

```text
Weather source ID
Source version
Location
Time range
Interpolation policy
Gap-filling policy
Derived-field models
Input-file hash
```

# 24. Typical simulation scenarios

## Constant-state engineering case

Used for component comparison and debugging.

## 24-hour measured case

Used for daily water production.

## Multi-day sequence

Used for battery and adsorption-cycle evaluation.

## Seasonal dataset

Used for feasibility and optimization.

# 25. Diagnostics

```text
WeatherTimestampDuplicate
WeatherTimestampOutOfOrder
WeatherValueNonFinite
HumidityOutsideRange
PressureFallbackUsed
SolarComponentsEstimated
WeatherGapInterpolated
WeatherGapTooLong
SunBelowHorizonIrradianceCorrected
SourceMetadataIncomplete
```

# 26. Required tests

- constant-state provider;
- exact timestamp lookup;
- interpolation;
- humidity-ratio interpolation;
- timezone handling;
- duplicate timestamp rejection;
- short-gap repair;
- long-gap rejection;
- sunrise/sunset behavior;
- non-negative irradiance;
- deterministic lookup;
- CSV round trip.

# 27. Integration tests

- weather to ambient-air source;
- weather to solar panel;
- weather to solar collector;
- 24-hour AWG run;
- different timestep from source interval;
- missing pressure fallback;
- synthetic and measured metadata separation.

# 28. Acceptance criteria

The weather model is accepted when:

1. every state has an explicit timestamp and pressure;
2. source and derived fields are distinguishable;
3. interpolation policy is deterministic;
4. missing data is never silently hidden;
5. solar geometry is centralized;
6. simulation results store enough weather metadata for reproduction.

---

**End of Document**
