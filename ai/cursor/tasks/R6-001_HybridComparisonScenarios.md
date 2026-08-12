# R6-001 — Hybrid Comparison Scenarios

**Status:** Done

Requirements:
HYB-001, HYB-002, HYB-003.

Purpose:
compare architecture variants with common cooling-plant KPIs:

```text
A direct TEC
B heating-only control
C sorbent + TEC
D direct compressor
E sorbent + compressor
```

Implement in `ThermoCore.AWG/Hybrid/`:
- stream factory (ambient / heated / regeneration);
- catalog + runner;
- exhausted / desorbed vapor accounting;
- CSV/Markdown report writer.

Acceptance:
- pairwise TEC and compressor comparisons available;
- regeneration stream raises dew point vs ambient;
- heating-only does not raise dew point;
- TEC regression path unchanged.
