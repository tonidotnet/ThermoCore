using ThermoCore.AWG.Simulation;

namespace ThermoCore.AWG.Sizing;

/// <summary>24 h summer-diurnal baseline + scaled sizing for water targets.</summary>
public sealed record AwgDiurnalSizingReport
{
    public required DateTimeOffset DayStartUtc { get; init; }

    public required bool SimulationSucceeded { get; init; }

    public required double BaselineWaterLiters { get; init; }

    public required double BaselineDailyElectricalWh { get; init; }

    public required double BaselinePvGenerationWh { get; init; }

    public required double BaselinePeltierElectricalWh { get; init; }

    public required double BaselineBusLoadWh { get; init; }

    public required double BaselineNightElectricalWh { get; init; }

    public required double PeakSunHours { get; init; }

    public required double SpecificEnergyWhPerLiter { get; init; }

    public required IReadOnlyList<AwgDiurnalSizingPointResult> Targets { get; init; }

    public required IReadOnlyList<AwgDiurnalHourlySample> HourlySamples { get; init; }

    public AwgSimulationRunResult? Run { get; init; }

    public string? FailureMessage { get; init; }
}
