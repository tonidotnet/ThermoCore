namespace ThermoCore.AWG.Calibration;

/// <summary>One steady operating-point segment for a synthetic M5 campaign CSV.</summary>
public sealed record AwgSyntheticCampaignSegment
{
    public required string Id { get; init; }

    public required double AmbientTemperatureK { get; init; }

    public required double RelativeHumidityFraction { get; init; }

    public required double SolarIrradianceWPerM2 { get; init; }

    public required TimeSpan Duration { get; init; }

    public required TimeSpan TimeStep { get; init; }

    public double TruthCondenserBypassFactor { get; init; } = 0.22;
}
