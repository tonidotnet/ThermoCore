# ThermoCore
## 29_ResultFormats.md

**Version:** 1.0  
**Status:** Implemented  
**Document Type:** Result, export and serialization specification  
**Applies To:** Console, API, Web, persistence and analysis tools

---

# 1. Purpose

This document defines canonical result identifiers, metadata, JSON and CSV formats, export bundles and versioning rules.

# 2. Goals

- stable machine-readable output;
- explicit units;
- reproducibility;
- human-readable exports;
- large-series support;
- forward-compatible schemas;
- no loss of authoritative precision.

# 3. Result levels

```text
Run metadata
Run summary
Time-series channels
Diagnostics
Balance records
Configuration snapshot
Model metadata
```

# 4. Channel identifiers

Use stable dot-separated IDs.

Examples:

```text
environment.ambient.temperature
environment.ambient.relativeHumidity
air.mp05.temperature
air.mp05.humidityRatio
air.mp07.dewPoint
water.collected.rate
water.collected.cumulative
silicaGel.loading
battery.soc
pv.electricalPower
peltier.coolingPower
fan.volumetricFlow
balance.energy.residual
balance.water.residual
```

# 5. Channel metadata

```csharp
public sealed record ResultChannelDefinition
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string QuantityType { get; init; }

    public required string Unit { get; init; }

    public required string ComponentId { get; init; }

    public required string Description { get; init; }
}
```

# 6. Canonical internal units

Exports may use SI by default.

Display exports may use requested units if metadata and headers remain explicit.

# 7. JSON run envelope

```json
{
  "resultFormatVersion": "1.0",
  "metadata": {},
  "summary": {},
  "channels": [],
  "diagnostics": [],
  "balances": []
}
```

# 8. Time-series JSON

```json
{
  "id": "battery.soc",
  "unit": "fraction",
  "startTimeUtc": "2026-07-27T00:00:00Z",
  "intervalSeconds": 60,
  "values": [0.8, 0.799, 0.798]
}
```

For irregular data:

```json
{
  "timestampsUtc": [],
  "values": []
}
```

# 9. CSV wide format

Header example:

```text
timestamp_utc,ambient_temperature_c,battery_soc_fraction,collected_water_kg
```

Advantages:

- easy spreadsheet use;
- one row per time point.

Limitations:

- large width;
- missing-channel complexity.

# 10. CSV long format

```text
timestamp_utc,channel_id,value,unit
```

Advantages:

- flexible;
- suitable for databases and analytics.

# 11. Recommended CSV exports

Provide both:

```text
summary.csv
series-wide.csv
series-long.csv
diagnostics.csv
balances.csv
```

# 12. Export ZIP

Recommended contents:

```text
manifest.json
configuration.json
metadata.json
summary.json
summary.csv
channels.json
series-wide.csv
diagnostics.csv
balances.csv
README.txt
```

# 13. Manifest

```json
{
  "packageType": "ThermoCoreSimulationExport",
  "packageVersion": "1.0",
  "simulationId": "...",
  "createdAtUtc": "...",
  "files": [
    {
      "path": "summary.json",
      "sha256": "..."
    }
  ]
}
```

# 14. Summary metrics

Canonical IDs:

```text
water.collected.totalKg
water.collected.totalLitersApprox
water.production.averageKgPerDay
water.production.litersPerDay
energy.solar.totalJ
energy.electrical.totalJ
energy.peltier.totalJ
energy.fan.totalJ
efficiency.whPerLiterApprox
kpi.litersPerKwhElectric
kpi.litersPerKwhSolarPrimary
kpi.litersPerDayPerSquareMeterAperture
kpi.waterRecoveryFraction
kpi.desorptionCaptureFraction
battery.minimumSoc
battery.finalSoc
temperature.peltierHot.maximumK
temperature.collector.maximumK
balance.water.maximumAbsoluteResidualKg
balance.energy.maximumAbsoluteResidualJ
```

`kpi.*` keys are omitted when the corresponding denominator is zero/undefined (never NaN).
Solar primary energy (`energy.solar.totalJ` / `kpi.litersPerKwhSolarPrimary`) uses incident
collector-aperture irradiance only; recovered internal heat is excluded.
# 15. Diagnostics CSV

Columns:

```text
step_index
simulation_time_utc
severity
code
component_id
port_id
message
numeric_context_json
```

# 16. Balance CSV

Columns:

```text
step_index
simulation_time_utc
balance_type
input
output
storage_change
residual
absolute_tolerance
relative_tolerance
status
```

# 17. Precision

- store full `double` precision in JSON;
- use invariant formatting;
- do not round persisted values for display;
- UI rounding is separate.

# 18. Missing values

JSON:

```text
null
```

CSV:

```text
empty field
```

NaN and infinity shall not be serialized as valid physical results.

# 19. Units metadata

Every channel must have a unit.

Dimensionless quantities use:

```text
fraction
ratio
count
```

# 20. Versioning

`resultFormatVersion` is independent from application version.

Breaking format changes require migration or new reader version.

# 21. Downsampled data

Downsampled chart series shall be marked:

```json
{
  "isDownsampled": true,
  "sourceSampleCount": 1440,
  "sampleCount": 300,
  "method": "LargestTriangleThreeBuckets"
}
```

Summary metrics must come from full-resolution data.

# 22. Configuration snapshot

Every export includes the exact configuration version used.

# 23. Model metadata

Store:

```text
Core version
AWG version
Topology version
Component models
Parameter-set IDs
Numerical settings
Weather source
```

# 24. File naming

Use safe lowercase names with hyphens where possible.

Avoid user-provided names as raw file paths.

# 25. Required tests

- JSON round trip;
- CSV invariant formatting;
- unit metadata completeness;
- manifest hashes;
- wide and long export;
- missing values;
- no NaN/Infinity;
- version handling;
- downsample metadata;
- full-resolution summary consistency.

# 26. Acceptance criteria

The format is accepted when:

1. all channels have stable IDs and units;
2. exports contain reproducibility metadata;
3. full precision is retained;
4. CSV is spreadsheet-friendly;
5. large-series export is supported;
6. result format is versioned independently.

---

**End of Document**
