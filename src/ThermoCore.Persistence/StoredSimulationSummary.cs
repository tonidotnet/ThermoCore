namespace ThermoCore.Persistence;

/// <summary>Query-friendly persisted simulation summary (DOC-021 §8).</summary>
public sealed record StoredSimulationSummary
{
    public required Guid Id { get; init; }

    public required Guid ConfigurationVersionId { get; init; }

    public required string Status { get; init; }

    public required bool Succeeded { get; init; }

    public required string TopologyId { get; init; }

    public required int CompletedSteps { get; init; }

    public required double AggregatedEnergyResidualJ { get; init; }

    public required double AggregatedWaterResidualKg { get; init; }

    public required bool WaterBalancePassed { get; init; }

    public required bool EnergyBalancePassed { get; init; }

    public double? FinalWaterTankContentKg { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? CompletedAtUtc { get; init; }
}
