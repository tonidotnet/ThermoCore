# R1-002 — Add Cooling-System Result Channels

Requirements:
KPI-005.

Add channels/summary values for:

```text
BareCoolingDeviceCOP
CoolingPlantCOP
AverageTemperatureLift
AverageDewPointMargin
CoolingPlantElectricalEnergy
CoolingPlantThermalInput
```

Reuse existing Peltier/condenser signals where available.

Do not invent values when unavailable; use optional/diagnostic behavior consistent with existing result conventions.

Acceptance:
- additive result format;
- deterministic calculation;
- tests for TEC baseline.
