namespace ThermoCore.AWG.Simulation;

/// <summary>Human- and machine-readable summary of an AWG run (APP-004).</summary>
public sealed record AwgRunSummary
{
    public required bool Succeeded { get; init; }

    public required string TopologyId { get; init; }

    public required string TopologyVersion { get; init; }

    public required string GraphFingerprint { get; init; }

    public required int ComponentCount { get; init; }

    public required int ConnectionCount { get; init; }

    public required int CompletedSteps { get; init; }

    public required TimeSpan Duration { get; init; }

    public required TimeSpan TimeStep { get; init; }

    public required double AggregatedEnergyResidualJ { get; init; }

    public required double AggregatedWaterResidualKg { get; init; }

    public required double AggregatedDryAirResidualKg { get; init; }

    public required int WarningCount { get; init; }

    public required int ErrorCount { get; init; }

    public required IReadOnlyDictionary<string, double?> FinalMoistAirTemperaturesC { get; init; }

    public required IReadOnlyDictionary<string, double?> FinalHumidityRatiosKgPerKg { get; init; }

    public double? FinalBusPowerW { get; init; }

    public double? FinalCurtailedPowerW { get; init; }

    public double? FinalWaterTankContentKg { get; init; }

    public double? FinalWaterTankLevelFraction { get; init; }

    /// <summary>Mean POA irradiance observed on the solar-radiation outlet (W/m²).</summary>
    public double? MeanIncidentSolarIrradianceWPerM2 { get; init; }

    /// <summary>Σ G·A·Δt over the run using configured collector aperture (J).</summary>
    public double? IncidentSolarEnergyJ { get; init; }

    /// <summary>Σ ṁ·(h_out−h_in)·Δt across the collector moist-air ports (J, floored at 0 per step).</summary>
    public double? UsefulCollectorEnergyJ { get; init; }

    /// <summary>UsefulCollectorEnergyJ / IncidentSolarEnergyJ when incident energy &gt; 0.</summary>
    public double? SolarUtilizationFraction { get; init; }

    public double? FinalBatteryStateOfChargeFraction { get; init; }

    public double? BatteryStateOfChargeSwingFraction { get; init; }

    /// <summary>(Σ E_charge + Σ E_discharge) / NominalCapacity when electrical subsystem is present.</summary>
    public double? BatteryThroughputFraction { get; init; }

    /// <summary>Extrapolated liters/day from collected tank water (ρ≈1 kg/L).</summary>
    public double? LitersPerDay { get; init; }

    /// <summary>Σ (bus load + Peltier cooling proxy) · Δt over the run (J).</summary>
    public double? ElectricEnergyConsumedJ { get; init; }

    /// <summary>Σ power-manager.bus · Δt (J).</summary>
    public double? BusElectricalEnergyJ { get; init; }

    /// <summary>Σ condenser-cooling heat request · Δt (J); COP≈1 electrical proxy.</summary>
    public double? PeltierElectricalProxyEnergyJ { get; init; }

    /// <summary>Ambient moisture mass supplied at ambient-source.outlet (kg).</summary>
    public double? AmbientMoistureIntakeKg { get; init; }

    /// <summary>Integrated desorbed water mass from bed moist-air balance (kg).</summary>
    public double? DesorbedWaterMassKg { get; init; }

    /// <summary>Configured thermal-collector aperture (m²).</summary>
    public double? SolarCollectorApertureAreaM2 { get; init; }

    /// <summary>L/kWh_electric (KPI-001). Null when electric energy ≤ 0.</summary>
    public double? LitersPerKwhElectric { get; init; }

    /// <summary>
    /// L/kWh_solar_primary (KPI-002). Denominator is incident aperture solar only;
    /// recovered internal heat is excluded.
    /// </summary>
    public double? LitersPerKwhSolarPrimary { get; init; }

    /// <summary>L/day/m² solar aperture (KPI-003). Null when aperture ≤ 0.</summary>
    public double? LitersPerDayPerSquareMeterAperture { get; init; }

    /// <summary>Collected water / ambient moisture intake (KPI-004). Null when intake ≤ 0.</summary>
    public double? WaterRecoveryFraction { get; init; }

    /// <summary>Collected water / desorbed bed water. Null when no desorption.</summary>
    public double? DesorptionCaptureFraction { get; init; }

    /// <summary>Wh_electric/L from integrated electrical energy. Null when water or energy ≤ 0.</summary>
    public double? WattHoursElectricPerLiter { get; init; }

    /// <summary>Σ delivered condenser cooling · Δt (J).</summary>
    public double? CoolingPlantThermalInputJ { get; init; }

    /// <summary>Σ (device Pe + fan Pe during cooling) · Δt (J).</summary>
    public double? CoolingPlantElectricalEnergyJ { get; init; }

    /// <summary>Bare device COP = Σ Qc / Σ Pe (KPI-005). ~1 for ControllableHeatSource proxy.</summary>
    public double? BareCoolingDeviceCOP { get; init; }

    /// <summary>Plant COP = thermal input / plant electrical including fan (KPI-005).</summary>
    public double? CoolingPlantCOP { get; init; }

    /// <summary>Mean (T_hot − T_cold) over cooling-active samples (K).</summary>
    public double? AverageTemperatureLiftK { get; init; }

    /// <summary>Mean (T_dp,in − T_surface) over cooling-active samples (K).</summary>
    public double? AverageDewPointMarginK { get; init; }
}
