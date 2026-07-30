namespace ThermoCore.Core.Environment;

/// <summary>Provenance metadata for a weather series (docs/04_Simulation/28_WeatherModel.md).</summary>
public sealed record WeatherSourceMetadata
{
    public required string SourceName { get; init; }

    public required string SourceVersion { get; init; }

    public required string LocationName { get; init; }

    public required double LatitudeDegrees { get; init; }

    public required double LongitudeDegrees { get; init; }

    public required double ElevationM { get; init; }

    public required string TimezoneId { get; init; }

    public required string DataLicense { get; init; }

    public required IReadOnlyCollection<string> DerivedFields { get; init; }
}
