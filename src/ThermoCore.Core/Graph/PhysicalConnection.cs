namespace ThermoCore.Core.Graph;

public sealed record PhysicalConnection
{
    public required string Id { get; init; }

    public required string SourceComponentId { get; init; }

    public required string SourcePortId { get; init; }

    public required string TargetComponentId { get; init; }

    public required string TargetPortId { get; init; }
}
