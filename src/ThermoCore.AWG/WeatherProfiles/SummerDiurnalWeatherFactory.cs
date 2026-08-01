using ThermoCore.Core.Environment;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.WeatherProfiles;

/// <summary>
/// Average summer day: warm/dry afternoon (~32 °C, ~30% RH) and cool/humid night (~20 °C, ~60% RH)
/// with a clear-sky GHI half-sine (06:00–18:00, peak 950 W/m²).
/// </summary>
public static class SummerDiurnalWeatherFactory
{
    public const double DayPeakTemperatureC = 32.0;
    public const double NightTemperatureC = 20.0;
    public const double DayRelativeHumidityFraction = 0.30;
    public const double NightRelativeHumidityFraction = 0.60;
    public const double PeakIrradianceWPerM2 = 950.0;

    public static InterpolatingWeatherProvider CreateProvider(DateTimeOffset dayStartUtc)
    {
        var series = CreateSeries(dayStartUtc);
        return new InterpolatingWeatherProvider(series);
    }

    public static WeatherTimeSeries CreateSeries(DateTimeOffset dayStartUtc)
    {
        var start = dayStartUtc.ToUniversalTime();
        // Hourly anchors: anti-correlated T/RH, GHI clear-sky day.
        (double Hour, double TemperatureC, double Rh, double Ghi)[] anchors =
        [
            (0, 20.0, 0.60, 0),
            (3, 19.5, 0.62, 0),
            (6, 21.0, 0.55, 0),
            (7, 22.5, 0.50, 120),
            (9, 26.0, 0.42, 520),
            (12, 30.0, 0.34, 900),
            (15, 32.0, 0.30, 950),
            (17, 30.0, 0.33, 420),
            (18, 28.0, 0.38, 0),
            (21, 23.5, 0.50, 0),
            (24, 20.0, 0.60, 0)
        ];

        var states = anchors.Select(a => new WeatherState
        {
            TimestampUtc = start.AddHours(a.Hour),
            AmbientTemperatureK = UnitConversions.CelsiusToKelvin(a.TemperatureC),
            RelativeHumidityFraction = a.Rh,
            AbsolutePressurePa = PhysicalConstants.StandardAtmosphericPressurePa,
            WindSpeedMPerSecond = 1.5,
            GlobalHorizontalIrradianceWPerM2 = a.Ghi,
            QualityFlags = WeatherQualityFlags.Synthetic | WeatherQualityFlags.DerivedSolar
        }.Validate()).ToArray();

        return new WeatherTimeSeries
        {
            States = states,
            Metadata = new WeatherSourceMetadata
            {
                SourceName = "summer-diurnal-average",
                SourceVersion = "1.0",
                LocationName = "synthetic-mediterranean-summer",
                LatitudeDegrees = 42.0,
                LongitudeDegrees = 19.0,
                ElevationM = 50.0,
                TimezoneId = "UTC",
                DataLicense = "synthetic",
                DerivedFields =
                [
                    "day_32C_30pct_rh",
                    "night_20C_60pct_rh",
                    "clear_sky_ghi_anchors"
                ]
            }
        }.Validate();
    }

    /// <summary>Peak-sun-hours equivalent from the profile (∫G dt / 1000 W/m²).</summary>
    public static double EstimatePeakSunHours()
    {
        // Piecewise-linear integral of the GHI anchors over 24 h.
        (double Hour, double Ghi)[] g =
        [
            (0, 0), (6, 0), (7, 120), (9, 520), (12, 900), (15, 950), (17, 420), (18, 0), (24, 0)
        ];
        var whPerM2 = 0.0;
        for (var i = 0; i < g.Length - 1; i++)
        {
            var dtH = g[i + 1].Hour - g[i].Hour;
            whPerM2 += 0.5 * (g[i].Ghi + g[i + 1].Ghi) * dtH;
        }

        return whPerM2 / 1000.0;
    }
}
