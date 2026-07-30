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
}
