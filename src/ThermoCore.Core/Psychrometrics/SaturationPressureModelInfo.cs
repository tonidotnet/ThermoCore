namespace ThermoCore.Core.Psychrometrics;

public sealed record SaturationPressureModelInfo
{
    public required string ModelName { get; init; }

    public required double MinimumTemperatureK { get; init; }

    public required double MaximumTemperatureK { get; init; }

    public required string Reference { get; init; }

    public required string PhaseBasis { get; init; }
}
