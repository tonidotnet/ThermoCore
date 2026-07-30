namespace ThermoCore.Api.Contracts;

public sealed record PsychrometricCalculateRequest
{
    public required double TemperatureC { get; init; }

    public required double RelativeHumidityPercent { get; init; }

    public double AbsolutePressurePa { get; init; } = 101_325.0;
}
