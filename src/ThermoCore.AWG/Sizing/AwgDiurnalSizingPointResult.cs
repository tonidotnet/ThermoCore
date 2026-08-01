namespace ThermoCore.AWG.Sizing;

/// <summary>Sized plant for one daily water production target.</summary>
public sealed record AwgDiurnalSizingPointResult
{
    public required double TargetLitersPerDay { get; init; }

    public required double ScaleFactorVersusBaseline { get; init; }

    public required double DailyElectricalEnergyWh { get; init; }

    public required double SpecificEnergyWhPerLiter { get; init; }

    public required double RecommendedPvRatedPowerW { get; init; }

    public required double RecommendedPvAreaM2 { get; init; }

    public required double RecommendedBatteryCapacityWh { get; init; }

    public required double NightElectricalEnergyWh { get; init; }

    public required bool Feasible { get; init; }

    public string? Notes { get; init; }
}
