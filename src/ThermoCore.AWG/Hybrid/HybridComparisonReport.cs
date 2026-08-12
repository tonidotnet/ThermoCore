namespace ThermoCore.AWG.Hybrid;

/// <summary>Common KPI row for one hybrid variant evaluation (R6-001).</summary>
public sealed record HybridComparisonPointResult
{
    public required string ScenarioId { get; init; }

    public required HybridComparisonVariant Variant { get; init; }

    public required double AmbientTemperatureC { get; init; }

    public required double RelativeHumidityPercent { get; init; }

    public required double InletTemperatureC { get; init; }

    public required double InletDewPointC { get; init; }

    public required double InletHumidityRatioKgPerKgDryAir { get; init; }

    public required double CondensedWaterKgPerSecond { get; init; }

    /// <summary>Outlet vapor mass flow — water not captured (HYB-003).</summary>
    public required double ExhaustedVaporKgPerSecond { get; init; }

    /// <summary>
    /// Extra vapor mass flow attributed to regeneration vs ambient (sorbent+ variants);
    /// null for direct / heating-only paths.
    /// </summary>
    public double? DesorbedVaporKgPerSecond { get; init; }

    public required double CoolingDeliveredW { get; init; }

    public required double ElectricalInputW { get; init; }

    public double? BareDeviceCop { get; init; }

    public double? CoolingPlantCop { get; init; }

    public double? LitersPerKwhElectric { get; init; }

    public double? DewPointMarginK { get; init; }

    public required double EnergyResidualJ { get; init; }

    public required double WaterMassResidualKg { get; init; }

    public required bool Passed { get; init; }

    public string? FailureMessage { get; init; }
}

/// <summary>Suite report across hybrid architecture variants.</summary>
public sealed record HybridComparisonReport
{
    public required IReadOnlyList<HybridComparisonPointResult> Points { get; init; }

    public IReadOnlyList<HybridComparisonPointResult> PassedPoints
        => Points.Where(p => p.Passed).ToArray();

    public HybridComparisonPointResult? BestLitersPerKwhElectric
        => PassedPoints
            .Where(p => p.LitersPerKwhElectric is not null)
            .OrderByDescending(p => p.LitersPerKwhElectric)
            .FirstOrDefault();

    public HybridComparisonPointResult? BestWaterRate
        => PassedPoints
            .OrderByDescending(p => p.CondensedWaterKgPerSecond)
            .FirstOrDefault();
}
