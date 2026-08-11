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
BareCoolingDeviceCOP          (R1-002) — `BareCoolingDeviceCOP`
CoolingPlantCOP               (R1-002) — `CoolingPlantCOP`
AverageTemperatureLift        (R1-002) — `AverageTemperatureLiftK`
AverageDewPointMargin         (R1-002) — `AverageDewPointMarginK`
CoolingPlantElectricalEnergy  (R1-002) — `CoolingPlantElectricalEnergyJ`
CoolingPlantThermalInput      (R1-002) — `CoolingPlantThermalInputJ`
```

## Cooling channels (R1-002 / KPI-005)

| Channel | Formula | Null when |
|---|---|---|
| `CoolingPlantThermalInputJ` | Σ delivered condenser cooling \(Q_{c,\mathrm{del}}=\dot m_{da}(h_{in}-h_{out})\) · Δt | condenser moist-air ports not observed |
| `CoolingPlantElectricalEnergyJ` | Σ \((P_{e,\mathrm{device}}+P_{e,\mathrm{fan}})\) · Δt during cooling-active steps | no device/fan electrical signal |
| `BareCoolingDeviceCOP` | Σ \(Q_{c,\mathrm{device}}\) / Σ \(P_{e,\mathrm{device}}\) | device Pe ≤ 0 |
| `CoolingPlantCOP` | `CoolingPlantThermalInputJ` / `CoolingPlantElectricalEnergyJ` | either ≤ 0 / missing |
| `AverageTemperatureLiftK` | mean \((T_{hot}-T_{cold})\) over cooling-active samples | no cooling samples |
| `AverageDewPointMarginK` | mean \((T_{dp,in}-T_{surface})\) over cooling-active samples | no cooling samples |

### Device electrical / cold heat today

AWG V3 uses `ControllableHeatSourceComponent` (`condenser-cooling`) as a **Qc capacity actuator**. The request heat is treated as a **COP≈1 electrical proxy** until a real TEC is wired into the graph:

```text
P_e,device ≈ Q_c,request
BareCoolingDeviceCOP ≈ 1.0
```

When an `AnalyticalPeltierComponent` or `ConstantCopPeltierComponent` is present in the graph, device Qc/Pe are taken from `cold_heat` / electrical (or Pe = Qc/COPc for ConstantCop when the electrical port is not published).

Fan electrical is included in plant COP per cooling-system rules (`ElectricalLoads` LoadId `"fan"`, else process-fan `LastElectricalPowerW`).

Hot-side temperature for lift: TEC `hot_heat` when present, otherwise ambient as heat-sink proxy.

### Scalar export keys

```text
energy.coolingPlant.thermalInputJ
energy.coolingPlant.electricalJ
kpi.bareCoolingDeviceCOP
kpi.coolingPlantCOP
kpi.averageTemperatureLiftK
kpi.averageDewPointMarginK
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
| Water/energy calculator | `AwgPerformanceKpiCalculator` |
| Cooling calculator | `AwgCoolingMetricsCalculator` |
| Console text | `AwgRunSummaryFormatter` |
| Export scalars | `AwgResultExporter` → `kpi.*`, `energy.*`, `efficiency.whPerLiterApprox` |
| API/Web | `SimulationSummaryResponse` additive fields |

## Comparison table

| Variant | L/day | Wh_e/L | L/kWh_e | L/kWh_solar | L/day/m² | recovery |
|---|---:|---:|---:|---:|---:|---:|

Every report must state which energy denominator it uses.
