namespace ThermoCore.AWG.Regression;

/// <summary>Compact machine-readable metrics for one regression scenario (R0-001).</summary>
public sealed record AwgRegressionBaselineScenarioEntry
{
    public required string ScenarioId { get; init; }

    public required string Description { get; init; }

    public required bool Passed { get; init; }

    public required int CompletedSteps { get; init; }

    public required double DurationSeconds { get; init; }

    public required double TimeStepSeconds { get; init; }

    public required string TopologyId { get; init; }

    public required string TopologyVersion { get; init; }

    public required string GraphFingerprint { get; init; }

    public required bool SimulationSucceeded { get; init; }

    public required bool BalanceAllPassed { get; init; }

    public required double AggregatedEnergyResidualJ { get; init; }

    public required double AggregatedWaterResidualKg { get; init; }

    public required double AggregatedDryAirResidualKg { get; init; }

    public double? FinalWaterTankContentKg { get; init; }

    public double? FinalBusPowerW { get; init; }

    public double? FinalBatteryStateOfChargeFraction { get; init; }

    public double? SolarUtilizationFraction { get; init; }

    public IReadOnlyList<string> Failures { get; init; } = Array.Empty<string>();
}
