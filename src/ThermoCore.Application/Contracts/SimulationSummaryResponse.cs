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
}
