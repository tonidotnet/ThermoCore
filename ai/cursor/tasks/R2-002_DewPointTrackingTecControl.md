# R2-002 — Dew-Point-Tracking TEC Controller

Requirements:
COOL-004.

Target:

```text
Tsurface,target = Tdewpoint,in - configuredMargin
```

Add:
- configurable margin;
- min/max current or power;
- safety limits;
- anti-chatter/rate limiting as appropriate;
- diagnostics for unreachable target.

Reuse existing psychrometrics and Peltier model.

Acceptance:
- controller chooses less drive when dew point is high enough;
- never violates current/power/thermal limits;
- deterministic tests cover margin and saturation cases.
