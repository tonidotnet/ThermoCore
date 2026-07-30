namespace ThermoCore.Api.Contracts;

/// <summary>List entry for a persisted simulation summary (DATA-008).</summary>
public sealed record PersistedSimulationListItem
{
    public required string SummaryId { get; init; }

    public required string ConfigurationVersionId { get; init; }

    public required string Status { get; init; }

    public required bool Succeeded { get; init; }

    public required string TopologyId { get; init; }

    public required int CompletedSteps { get; init; }

    public double? FinalWaterTankContentKg { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
