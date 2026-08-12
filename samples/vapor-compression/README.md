# Vapor-compression samples (R5-001 / COOL-006)

Performance-map contract artifacts for the Core `VaporCompressionPerformanceMap` schema.

| File | Role |
|---|---|
| `generic-small-dc-module.r5-001.json` | Synthetic manufacturer-style grid (8 points) |

```csharp
var map = VaporCompressionPerformanceMapSerializer.LoadFromFile(
    "samples/vapor-compression/generic-small-dc-module.r5-001.json");
var result = new VaporCompressionMapEvaluator(map).Evaluate(
    evaporatingTemperatureK: 283.15,
    condensingTemperatureK: 308.15,
    speedFraction: 1.0);
```

Extrapolation: `clampWithDiagnostic` (default) or `reject`. Cycling min runtime/off-time and frost/high-side limits are part of the contract; the AWG plant adapter is R5-002.
