using ThermoCore.Core.Physics;

namespace ThermoCore.Core.Environment;

/// <summary>Constant ambient/solar boundary used for engineering cases and tests.</summary>
public sealed class ConstantWeatherProvider : IWeatherProvider
{
    private readonly WeatherState _state;

    public ConstantWeatherProvider(
        WeatherState state,
        WeatherSourceMetadata? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state = state.Validate();
        Metadata = metadata ?? new WeatherSourceMetadata
        {
            SourceName = "constant",
            SourceVersion = "1.0",
            LocationName = "synthetic",
            LatitudeDegrees = 0.0,
            LongitudeDegrees = 0.0,
            ElevationM = 0.0,
            TimezoneId = "UTC",
            DataLicense = "synthetic",
            DerivedFields = Array.Empty<string>()
        };
    }

    public static ConstantWeatherProvider FromAmbient(
        double temperatureK,
        double relativeHumidityFraction,
        double irradianceWPerM2,
        double? pressurePa = null,
        double windSpeedMPerSecond = 0.0)
        => new(new WeatherState
        {
            TimestampUtc = DateTimeOffset.UnixEpoch,
            AmbientTemperatureK = temperatureK,
            RelativeHumidityFraction = relativeHumidityFraction,
            AbsolutePressurePa = pressurePa ?? PhysicalConstants.StandardAtmosphericPressurePa,
            WindSpeedMPerSecond = windSpeedMPerSecond,
            GlobalHorizontalIrradianceWPerM2 = irradianceWPerM2,
            QualityFlags = WeatherQualityFlags.Synthetic
        });

    public WeatherSourceMetadata Metadata { get; }

    public WeatherState GetState(DateTimeOffset timestampUtc)
        => _state with { TimestampUtc = timestampUtc };
}
