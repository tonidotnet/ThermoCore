**Project:** ThermoCore  
**Track:** AWG Cooling Research & Evolution  
**Status:** Implemented (R1-001)  
**Baseline:** Existing ThermoCore `main` with Core, AWG, API/Web, calibration/optimization and SolarAirHeater app already implemented.

---

# AWG Performance Metrics

## Required KPIs

```text
L/day
Wh_electric/L
L/kWh_electric
L/kWh_solar_primary
L/day/m² solar aperture
WaterRecoveryFraction
DesorptionCaptureFraction
BareCoolingDeviceCOP          (R1-002)
CoolingPlantCOP               (R1-002)
AverageTemperatureLift        (R1-002)
AverageDewPointMargin         (R1-002)
```

## Definitions (R1-001)

| KPI | Symbol / field | Formula | Zero-denominator |
|---|---|---|---|
| Liters/day | `LitersPerDay` | \(W_\mathrm{tank}\times(86400/\Delta t_\mathrm{run})\), \(\rho\approx1\,\mathrm{kg/L}\) | duration ≤ 0 → throw / omit |
| Wh_electric/L | `WattHoursElectricPerLiter` | \(E_\mathrm{e}/3600\,/\,W_\mathrm{tank}\) | water ≤ 0 or \(E_\mathrm{e}\) ≤ 0 → **null** |
| L/kWh_electric | `LitersPerKwhElectric` (KPI-001) | \(W_\mathrm{tank}/(E_\mathrm{e}/3.6\times10^6)\) | \(E_\mathrm{e}\) ≤ 0 → **null** |
| L/kWh_solar_primary | `LitersPerKwhSolarPrimary` (KPI-002) | \(W_\mathrm{tank}/(E_\mathrm{solar,inc}/3.6\times10^6)\) | incident solar ≤ 0 → **null** |
| L/day/m² | `LitersPerDayPerSquareMeterAperture` (KPI-003) | `LitersPerDay / A_collector` | aperture ≤ 0 → **null** |
| WaterRecoveryFraction | `WaterRecoveryFraction` (KPI-004) | \(W_\mathrm{tank}/M_\mathrm{ambient,in}\) | ambient moisture ≤ 0 → **null** |
| DesorptionCaptureFraction | `DesorptionCaptureFraction` | \(W_\mathrm{tank}/M_\mathrm{desorbed}\) | no desorption → **null** |

Never emit NaN; omit corresponding `ScalarMetrics` keys when the value is null.

### Electrical energy \(E_\mathrm{e}\)

```text
E_e = Σ (P_bus + P_peltier_proxy) · Δt
```

- `P_bus` = `power-manager.bus` (served loads: fan + controller)
- `P_peltier_proxy` = `condenser-cooling.outlet` heat request (current topology does not place the cooling actuator on the DC bus; treat as COP≈1 until cooling-plant accounting)

### Solar primary energy

```text
E_solar,inc = Σ G_poa · A_collector · Δt
```

Uses the **thermal-collector aperture** and `solar-radiation` POA only.

**Do not** add to the solar-primary denominator:

- useful collector heat (`UsefulCollectorEnergyJ`)
- recovered internal heat (`heat-recovery`)
- PV aperture incident energy (tracked separately when needed)
- Peltier hot-side / PV rear-air heat transfers

### Water mass terms

- \(M_\mathrm{ambient,in}\) = Σ `ambient-source.outlet` water-vapor mass flow · Δt
- \(M_\mathrm{desorbed}\) = Σ max(0, \(\dot m_{w,\mathrm{bed,out}}-\dot m_{w,\mathrm{collector,out}}\)) · Δt

## Solar accounting

Track separately:

```text
Incident PV solar energy
Incident thermal-collector solar energy   ← solar primary denominator
PV electrical output
Thermal energy transferred to process     ← useful collector (not primary)
Curtailed PV
Recovered internal heat                   ← never counted as new solar input
```

## Exposure

| Surface | Location |
|---|---|
| Summary record | `AwgRunSummary` additive nullable fields |
| Calculator | `AwgPerformanceKpiCalculator` |
| Console text | `AwgRunSummaryFormatter` |
| Export scalars | `AwgResultExporter` → `kpi.*`, `energy.*`, `efficiency.whPerLiterApprox` |
| API/Web | `SimulationSummaryResponse` additive fields |

## Comparison table

| Variant | L/day | Wh_e/L | L/kWh_e | L/kWh_solar | L/day/m² | recovery |
|---|---:|---:|---:|---:|---:|---:|

Every report must state which energy denominator it uses.
