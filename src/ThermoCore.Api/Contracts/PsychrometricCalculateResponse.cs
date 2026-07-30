namespace ThermoCore.Api.Contracts;

public sealed record PsychrometricCalculateResponse
{
    public required double TemperatureC { get; init; }

    public required double RelativeHumidityPercent { get; init; }

    public required double AbsolutePressurePa { get; init; }

    public required double HumidityRatioKgPerKgDryAir { get; init; }

    public required double? DewPointTemperatureC { get; init; }

    public required double SpecificEnthalpyKJPerKgDryAir { get; init; }

    public required double SpecificVolumeM3PerKgDryAir { get; init; }
}
