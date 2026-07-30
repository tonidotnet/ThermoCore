namespace ThermoCore.Core.Results;

/// <summary>One file entry in a DOC-029 export manifest.</summary>
public sealed record SimulationExportManifestFile
{
    public required string Path { get; init; }

    public required string Sha256 { get; init; }
}
