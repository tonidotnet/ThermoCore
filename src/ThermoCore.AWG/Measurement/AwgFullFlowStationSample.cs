namespace ThermoCore.AWG.Measurement;

/// <summary>Moist-air state at one full-flow process station.</summary>
public sealed record AwgFullFlowStationSample
{
    public required string StationId { get; init; }

    public required string HungarianName { get; init; }

    public required string EnglishName { get; init; }

    public required double TemperatureC { get; init; }

    public required double RelativeHumidityFraction { get; init; }

    public required double HumidityRatioKgPerKgDryAir { get; init; }

    public required double DryAirMassFlowKgPerSecond { get; init; }

    public required double WaterVaporMassFlowKgPerSecond { get; init; }
}
