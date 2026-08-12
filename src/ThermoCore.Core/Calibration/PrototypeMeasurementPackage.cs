namespace ThermoCore.Core.Calibration;

/// <summary>Imported prototype campaign package (metadata + wide rows).</summary>
public sealed record PrototypeMeasurementPackage
{
    public required PrototypeCampaignDocument Campaign { get; init; }

    public required string CsvSourcePath { get; init; }

    public required IReadOnlyList<PrototypeWideMeasurementRow> Rows { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
