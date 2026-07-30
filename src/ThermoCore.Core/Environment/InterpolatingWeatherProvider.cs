namespace ThermoCore.Core.Environment;

/// <summary>
/// Linear interpolation over a validated weather time series
/// (docs/04_Simulation/28_WeatherModel.md §14–§15).
/// </summary>
public sealed class InterpolatingWeatherProvider : IWeatherProvider
{
    private readonly IReadOnlyList<WeatherState> _states;

    public InterpolatingWeatherProvider(WeatherTimeSeries series)
    {
        ArgumentNullException.ThrowIfNull(series);
        series.Validate();
        _states = series.States;
        Metadata = series.Metadata;
    }

    public WeatherSourceMetadata Metadata { get; }

    public WeatherState GetState(DateTimeOffset timestampUtc)
    {
        if (timestampUtc <= _states[0].TimestampUtc)
        {
            return _states[0] with { TimestampUtc = timestampUtc };
        }

        if (timestampUtc >= _states[^1].TimestampUtc)
        {
            return _states[^1] with { TimestampUtc = timestampUtc };
        }

        var high = 1;
        while (high < _states.Count && _states[high].TimestampUtc < timestampUtc)
        {
            high++;
        }

        var low = high - 1;
        var a = _states[low];
        var b = _states[high];
        var spanTicks = (b.TimestampUtc - a.TimestampUtc).TotalSeconds;
        var fraction = spanTicks <= 0.0
            ? 0.0
            : (timestampUtc - a.TimestampUtc).TotalSeconds / spanTicks;
        fraction = Math.Clamp(fraction, 0.0, 1.0);

        return new WeatherState
        {
            TimestampUtc = timestampUtc,
            AmbientTemperatureK = Lerp(a.AmbientTemperatureK, b.AmbientTemperatureK, fraction),
            RelativeHumidityFraction = Math.Clamp(
                Lerp(a.RelativeHumidityFraction, b.RelativeHumidityFraction, fraction),
                0.0,
                1.0),
            AbsolutePressurePa = Lerp(a.AbsolutePressurePa, b.AbsolutePressurePa, fraction),
            WindSpeedMPerSecond = Lerp(a.WindSpeedMPerSecond, b.WindSpeedMPerSecond, fraction),
            GlobalHorizontalIrradianceWPerM2 = Math.Max(
                0.0,
                Lerp(a.GlobalHorizontalIrradianceWPerM2, b.GlobalHorizontalIrradianceWPerM2, fraction)),
            DirectNormalIrradianceWPerM2 = LerpOptional(a.DirectNormalIrradianceWPerM2, b.DirectNormalIrradianceWPerM2, fraction),
            DiffuseHorizontalIrradianceWPerM2 = LerpOptional(
                a.DiffuseHorizontalIrradianceWPerM2,
                b.DiffuseHorizontalIrradianceWPerM2,
                fraction),
            SkyTemperatureK = LerpOptional(a.SkyTemperatureK, b.SkyTemperatureK, fraction),
            GroundTemperatureK = LerpOptional(a.GroundTemperatureK, b.GroundTemperatureK, fraction),
            QualityFlags = a.QualityFlags | b.QualityFlags | WeatherQualityFlags.Interpolated
        }.Validate();
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    private static double? LerpOptional(double? a, double? b, double t)
    {
        if (a is null && b is null)
        {
            return null;
        }

        return Lerp(a ?? b!.Value, b ?? a!.Value, t);
    }
}
