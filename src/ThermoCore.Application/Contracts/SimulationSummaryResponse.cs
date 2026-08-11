namespace ThermoCore.Api.Contracts;

public sealed record SimulationSummaryResponse
{
    public required string SimulationId { get; init; }

    public required string Status { get; init; }

    public required bool Succeeded { get; init; }

    public required string TopologyId { get; init; }

    public required int CompletedSteps { get; init; }

    public required double AggregatedEnergyResidualJ { get; init; }

    public required double AggregatedWaterResidualKg { get; init; }

    public required double AggregatedDryAirResidualKg { get; init; }

    public required bool WaterBalancePassed { get; init; }

    public required bool EnergyBalancePassed { get; init; }

    public required int WarningCount { get; init; }

    public required int ErrorCount { get; init; }

    public double? FinalWaterTankContentKg { get; init; }

    public double? FinalBusPowerW { get; init; }

    /// <summary>Collected tank water (kg ≈ L for MVP).</summary>
    public double? CollectedWaterKg { get; init; }

    /// <summary>Extrapolated liters/day from collected water and run duration.</summary>
    public double? LitersPerDay { get; init; }

    /// <summary>Wh/liter proxy when electrical energy is available.</summary>
    public double? WattHoursPerLiter { get; init; }

    /// <summary>L/kWh_electric (KPI-001).</summary>
    public double? LitersPerKwhElectric { get; init; }

    /// <summary>L/kWh_solar_primary (KPI-002); incident collector-aperture solar only.</summary>
    public double? LitersPerKwhSolarPrimary { get; init; }

    /// <summary>L/day/m² solar aperture (KPI-003).</summary>
    public double? LitersPerDayPerSquareMeterAperture { get; init; }

    /// <summary>Collected water / ambient moisture intake (KPI-004).</summary>
    public double? WaterRecoveryFraction { get; init; }

    /// <summary>Collected water / desorbed bed water when desorption occurred.</summary>
    public double? DesorptionCaptureFraction { get; init; }

    /// <summary>Bare device COP = Σ Qc / Σ Pe (KPI-005).</summary>
    public double? BareCoolingDeviceCOP { get; init; }

    /// <summary>Plant COP including fan electrical (KPI-005).</summary>
    public double? CoolingPlantCOP { get; init; }

    /// <summary>Mean temperature lift over cooling-active samples (K).</summary>
    public double? AverageTemperatureLiftK { get; init; }

    /// <summary>Mean dew-point margin (T_dp − T_surface) over cooling-active samples (K).</summary>
    public double? AverageDewPointMarginK { get; init; }

    /// <summary>Σ cooling-plant electrical energy (J).</summary>
    public double? CoolingPlantElectricalEnergyJ { get; init; }

    /// <summary>Σ delivered condenser cooling (J).</summary>
    public double? CoolingPlantThermalInputJ { get; init; }
}
