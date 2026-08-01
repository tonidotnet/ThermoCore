namespace ThermoCore.AWG.Sizing;

/// <summary>Hourly aggregates from the 24 h summer-diurnal run.</summary>
public sealed record AwgDiurnalHourlySample
{
    public required int HourOfDay { get; init; }

    public required double AmbientTemperatureC { get; init; }

    public required double RelativeHumidityPercent { get; init; }

    public required double IrradianceWPerM2 { get; init; }

    public required double WaterProducedKg { get; init; }

    public required string DominantMode { get; init; }

    public required double MeanPeltierW { get; init; }

    public required double MeanFanOnFraction { get; init; }
}
