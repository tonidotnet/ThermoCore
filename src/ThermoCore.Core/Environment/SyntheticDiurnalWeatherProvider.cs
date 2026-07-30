using ThermoCore.Core.Physics;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Environment;

/// <summary>
/// Synthetic clear-sky diurnal temperature and GHI profile for 24-hour engineering runs.
/// </summary>
public sealed class SyntheticDiurnalWeatherProvider : IWeatherProvider
{
    private readonly DateTimeOffset _dayStartUtc;
    private readonly double _meanTemperatureK;
    private readonly double _temperatureAmplitudeK;
    private readonly double _relativeHumidityFraction;
    private readonly double _pressurePa;
    private readonly double _peakIrradianceWPerM2;
    private readonly double _windSpeedMPerSecond;

    public SyntheticDiurnalWeatherProvider(
        DateTimeOffset dayStartUtc,
        double meanTemperatureK,
        double temperatureAmplitudeK,
        double relativeHumidityFraction,
        double peakIrradianceWPerM2,
        double? pressurePa = null,
        double windSpeedMPerSecond = 1.0,
        WeatherSourceMetadata? metadata = null)
    {
        _dayStartUtc = dayStartUtc.ToUniversalTime();
        _meanTemperatureK = meanTemperatureK;
        _temperatureAmplitudeK = temperatureAmplitudeK;
        _relativeHumidityFraction = relativeHumidityFraction;
        _pressurePa = pressurePa ?? PhysicalConstants.StandardAtmosphericPressurePa;
        _peakIrradianceWPerM2 = peakIrradianceWPerM2;
        _windSpeedMPerSecond = windSpeedMPerSecond;
        Metadata = metadata ?? new WeatherSourceMetadata
        {
            SourceName = "synthetic-diurnal",
            SourceVersion = "1.0",
            LocationName = "synthetic",
            LatitudeDegrees = 0.0,
            LongitudeDegrees = 0.0,
            ElevationM = 0.0,
            TimezoneId = "UTC",
            DataLicense = "synthetic",
            DerivedFields = ["temperature_sinusoid", "clear_sky_ghi_half_sine"]
        };
    }

    public static SyntheticDiurnalWeatherProvider CreateDefault(DateTimeOffset dayStartUtc)
        => new(
            dayStartUtc,
            meanTemperatureK: UnitConversions.CelsiusToKelvin(25.0),
            temperatureAmplitudeK: 5.0,
            relativeHumidityFraction: 0.50,
            peakIrradianceWPerM2: 900.0);

    public WeatherSourceMetadata Metadata { get; }

    public WeatherState GetState(DateTimeOffset timestampUtc)
    {
        var utc = timestampUtc.ToUniversalTime();
        var hours = (utc - _dayStartUtc).TotalHours;
        var phase = (hours / 24.0) * 2.0 * Math.PI;
        // Peak temperature mid-afternoon (~15:00), min near dawn.
        var temperatureK = _meanTemperatureK + (_temperatureAmplitudeK * Math.Sin(phase - (Math.PI / 2.0)));
        var solarHour = hours - (24.0 * Math.Floor(hours / 24.0));
        var irradiance = 0.0;
        if (solarHour is > 6.0 and < 18.0)
        {
            var solarPhase = ((solarHour - 6.0) / 12.0) * Math.PI;
            irradiance = _peakIrradianceWPerM2 * Math.Sin(solarPhase);
        }

        return new WeatherState
        {
            TimestampUtc = utc,
            AmbientTemperatureK = temperatureK,
            RelativeHumidityFraction = _relativeHumidityFraction,
            AbsolutePressurePa = _pressurePa,
            WindSpeedMPerSecond = _windSpeedMPerSecond,
            GlobalHorizontalIrradianceWPerM2 = Math.Max(0.0, irradiance),
            QualityFlags = WeatherQualityFlags.Synthetic | WeatherQualityFlags.DerivedSolar
        }.Validate();
    }
}
