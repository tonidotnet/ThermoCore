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
}
