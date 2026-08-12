namespace ThermoCore.Core.Calibration;

/// <summary>
/// Provenance document for a prototype measurement campaign (VAL-001/002/003 / R3-001).
/// Points at a wide-format CSV and preserves hardware / sensor identity.
/// </summary>
public sealed record PrototypeCampaignDocument
{
    public const string CurrentSchemaVersion = "1.0";

    public string SchemaVersion { get; init; } = CurrentSchemaVersion;

    public required string CampaignId { get; init; }

    public required PrototypeValidationLevel ValidationLevel { get; init; }

    public required PrototypeHardwareIdentity Hardware { get; init; }

    public IReadOnlyList<PrototypeSensorCalibrationRef> Sensors { get; init; }
        = Array.Empty<PrototypeSensorCalibrationRef>();

    /// <summary>Path to the wide CSV (absolute, or relative to the document file).</summary>
    public required string MeasurementCsvPath { get; init; }

    public string? VariantId { get; init; }

    public string? TestProtocolId { get; init; }

    public string? SourceIdentifier { get; init; }

    public string? SourceRevision { get; init; }

    public string? Notes { get; init; }

    /// <summary>
    /// Optional overrides mapping wide CSV columns → long-format channel ids.
    /// When empty, <see cref="PrototypeWideCsvSchema.DefaultChannelMap"/> is used.
    /// </summary>
    public IReadOnlyDictionary<string, string>? ChannelMap { get; init; }

    public PrototypeCampaignDocument Validate()
    {
        if (string.IsNullOrWhiteSpace(SchemaVersion))
        {
            throw new ArgumentException("SchemaVersion is required.", nameof(SchemaVersion));
        }

        if (!string.Equals(SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Unsupported prototype campaign schema version '{SchemaVersion}'. Supported: {CurrentSchemaVersion}.",
                nameof(SchemaVersion));
        }

        if (string.IsNullOrWhiteSpace(CampaignId))
        {
            throw new ArgumentException("CampaignId is required.", nameof(CampaignId));
        }

        if (!Enum.IsDefined(ValidationLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(ValidationLevel));
        }

        if (string.IsNullOrWhiteSpace(MeasurementCsvPath))
        {
            throw new ArgumentException("MeasurementCsvPath is required.", nameof(MeasurementCsvPath));
        }

        Hardware.Validate();
        foreach (var sensor in Sensors)
        {
            sensor.Validate();
        }

        return this;
    }
}
