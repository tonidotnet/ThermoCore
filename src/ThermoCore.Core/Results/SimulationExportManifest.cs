namespace ThermoCore.Core.Results;

/// <summary>DOC-029 export package manifest.</summary>
public sealed record SimulationExportManifest
{
    public required string PackageType { get; init; }

    public required string PackageVersion { get; init; }

    public required string SimulationId { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required string ResultFormatVersion { get; init; }

    public required IReadOnlyList<SimulationExportManifestFile> Files { get; init; }
}
