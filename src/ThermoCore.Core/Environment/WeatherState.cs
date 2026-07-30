using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Environment;

/// <summary>Time-indexed ambient and solar environment state (docs/04_Simulation/28_WeatherModel.md).</summary>
public sealed record WeatherState
{
    public required DateTimeOffset TimestampUtc { get; init; }

    public required double AmbientTemperatureK { get; init; }

    public required double RelativeHumidityFraction { get; init; }

    public required double AbsolutePressurePa { get; init; }

    public required double WindSpeedMPerSecond { get; init; }

    public required double GlobalHorizontalIrradianceWPerM2 { get; init; }

    public double? DirectNormalIrradianceWPerM2 { get; init; }

    public double? DiffuseHorizontalIrradianceWPerM2 { get; init; }

    public double? SkyTemperatureK { get; init; }

    public double? GroundTemperatureK { get; init; }

    public required WeatherQualityFlags QualityFlags { get; init; }

    public WeatherState Validate()
    {
        FiniteNumber.Require(AmbientTemperatureK, nameof(AmbientTemperatureK));
        FiniteNumber.Require(RelativeHumidityFraction, nameof(RelativeHumidityFraction));
        FiniteNumber.RequirePositive(AbsolutePressurePa, nameof(AbsolutePressurePa));
        FiniteNumber.RequireNonNegative(WindSpeedMPerSecond, nameof(WindSpeedMPerSecond));
        FiniteNumber.RequireNonNegative(GlobalHorizontalIrradianceWPerM2, nameof(GlobalHorizontalIrradianceWPerM2));
        if (RelativeHumidityFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(RelativeHumidityFraction));
        }

        if (DirectNormalIrradianceWPerM2 is { } dni)
        {
            FiniteNumber.RequireNonNegative(dni, nameof(DirectNormalIrradianceWPerM2));
        }

        if (DiffuseHorizontalIrradianceWPerM2 is { } dhi)
        {
            FiniteNumber.RequireNonNegative(dhi, nameof(DiffuseHorizontalIrradianceWPerM2));
        }

        return this;
    }
}
