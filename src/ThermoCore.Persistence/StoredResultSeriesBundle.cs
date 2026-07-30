namespace ThermoCore.Persistence;

/// <summary>Channel descriptors plus optional loaded sample payload for one simulation.</summary>
public sealed record StoredResultSeriesBundle
{
    public required Guid SimulationSummaryId { get; init; }

    public required IReadOnlyList<StoredResultSeriesDescriptor> Channels { get; init; }

    /// <summary>Channel id → sample values when the compressed payload was loaded.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<double>>? ValuesByChannelId { get; init; }
}
