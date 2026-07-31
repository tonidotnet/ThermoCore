# AWG prototype measurement campaign protocol (M5)

This protocol defines the **minimum physical campaign** required before claiming M5 “prototype-validated.” Software holdout (`holdout` console command) is ready; field data is not yet in-repo.

## 1. Objectives

1. Collect synchronized ambient, solar, and key process channels for calibration.
2. Reserve a chronological holdout window (≥30% of timestamps) never used for fitting.
3. Publish RMSE/MAE/bias on holdout against the fitted configuration.

## 2. Required channels (minimum)

| Channel id pattern | Unit | Notes |
|---|---|---|
| `ambient-source.outlet.temperature` | K or °C (document unit) | Shielded dry-bulb |
| `ambient-source.outlet.humidityRatio` or RH + convert | kg/kg or — | Prefer humidity ratio after lab conversion |
| `solar-radiation.outlet.irradiance` | W/m² | POA on collector plane |
| Condenser / tank water mass or level | kg or — | Production metric |
| Optional: battery SOC, bus power | — / W | When electrical subsystem exercised |

Use long-format CSV (`timestamp_utc,channel_id,value,unit`) matching `samples/calibration/README.md`.

## 3. Operating points

Run at least three steady or slowly varying regimes (≥15 min each after settle):

1. Low irradiance / high humidity  
2. High irradiance / mid humidity  
3. Night or near-zero solar (control / leakage check)

Log controller mode and fan/TEC setpoints with each segment.

## 4. Holdout workflow (software)

```bash
dotnet run --project src/ThermoCore.Console -- holdout path/to/campaign.csv \
  --duration <sec> --dt 1 --train-fraction 0.7 \
  --params condenser.bypassFactor,condenser.drainageEfficiency
```

Acceptance for a campaign release:

- Holdout fitted RMSE ≤ holdout baseline RMSE (or documented justification).
- Fitted parameters remain inside catalog physical bounds.
- Limitations in `docs/06_Testing/26_ModelLimitations.md` reviewed.

## 5. Status

| Item | Status |
|---|---|
| CSV schema + import | Done |
| Synthetic ambient smoke | Done (`awg-mvp-ambient-smoke.csv`) |
| Synthetic three-regime campaign stand-in | Done (`awg-mvp-campaign-synthetic.csv` / `write-campaign`) |
| Holdout runner / console | Done |
| Physical prototype CSV in repo | **Open** (required for M5 closure) |

Regenerate the synthetic stand-in:

```bash
dotnet run --project src/ThermoCore.Console -- write-campaign samples/calibration/awg-mvp-campaign-synthetic.csv
```
