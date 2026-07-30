namespace ThermoCore.Persistence;

/// <summary>Persisted immutable configuration version (DOC-021 §5).</summary>
public sealed record StoredConfigurationVersion
{
    public required Guid Id { get; init; }

    public required Guid ConfigurationId { get; init; }

    public required int VersionNumber { get; init; }

    public required string Name { get; init; }

    public required string SchemaVersion { get; init; }

    public required string ConfigurationJson { get; init; }

    public required string ContentHash { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
