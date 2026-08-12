using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Absorption;

/// <summary>Clamped / rejected IDW interpolation for absorption research maps.</summary>
public static class AbsorptionMapInterpolator
{
    public readonly record struct Query(
        double GeneratorTemperatureK,
        double HeatSinkTemperatureK,
        double EvaporatorTemperatureK);

    public readonly record struct Result(
        AbsorptionMapPoint Interpolated,
        Query EffectiveQuery,
        bool OutsideValidity,
        bool Rejected);

    public static Result Evaluate(AbsorptionPerformanceMap map, Query query)
    {
        ArgumentNullException.ThrowIfNull(map);
        map.Validate();
        FiniteNumber.RequirePositive(query.GeneratorTemperatureK, nameof(query.GeneratorTemperatureK));
        FiniteNumber.RequirePositive(query.HeatSinkTemperatureK, nameof(query.HeatSinkTemperatureK));
        FiniteNumber.RequirePositive(query.EvaporatorTemperatureK, nameof(query.EvaporatorTemperatureK));

        var validity = map.Validity;
        var outside = false;
        var tg = Clamp(query.GeneratorTemperatureK, validity.MinimumGeneratorTemperatureK, validity.MaximumGeneratorTemperatureK, ref outside);
        var ts = Clamp(query.HeatSinkTemperatureK, validity.MinimumHeatSinkTemperatureK, validity.MaximumHeatSinkTemperatureK, ref outside);
        var te = Clamp(query.EvaporatorTemperatureK, validity.MinimumEvaporatorTemperatureK, validity.MaximumEvaporatorTemperatureK, ref outside);

        if (outside && map.ExtrapolationPolicy == AbsorptionExtrapolationPolicy.Reject)
        {
            var teSafe = Math.Min(query.EvaporatorTemperatureK, query.HeatSinkTemperatureK - 1.0);
            if (teSafe <= 0.0)
            {
                teSafe = Math.Max(1.0, query.HeatSinkTemperatureK * 0.5);
            }

            var tsSafe = Math.Max(query.HeatSinkTemperatureK, teSafe + 1.0);
            var tgSafe = Math.Max(query.GeneratorTemperatureK, tsSafe + 1.0);
            return new Result(
                new AbsorptionMapPoint
                {
                    GeneratorTemperatureK = tgSafe,
                    HeatSinkTemperatureK = tsSafe,
                    EvaporatorTemperatureK = teSafe,
                    ThermalInputW = 0.0,
                    CoolingOutputW = 0.0,
                    ThermalCop = 0.0
                }.Validate(),
                query,
                OutsideValidity: true,
                Rejected: true);
        }

        var effective = new Query(tg, ts, te);
        return new Result(Interpolate(map, effective), effective, outside, Rejected: false);
    }

    private static AbsorptionMapPoint Interpolate(AbsorptionPerformanceMap map, Query query)
    {
        var weights = new double[map.MapPoints.Count];
        var weightSum = 0.0;
        for (var i = 0; i < map.MapPoints.Count; i++)
        {
            var distance = NormalizedDistance(map, map.MapPoints[i], query);
            if (distance < 1e-12)
            {
                return map.MapPoints[i];
            }

            var w = 1.0 / (distance * distance);
            weights[i] = w;
            weightSum += w;
        }

        double qgen = 0, qc = 0, tg = 0, ts = 0, te = 0;
        for (var i = 0; i < map.MapPoints.Count; i++)
        {
            var w = weights[i] / weightSum;
            var p = map.MapPoints[i];
            qgen += w * p.ThermalInputW;
            qc += w * p.CoolingOutputW;
            tg += w * p.GeneratorTemperatureK;
            ts += w * p.HeatSinkTemperatureK;
            te += w * p.EvaporatorTemperatureK;
        }

        return new AbsorptionMapPoint
        {
            GeneratorTemperatureK = tg,
            HeatSinkTemperatureK = ts,
            EvaporatorTemperatureK = te,
            ThermalInputW = Math.Max(0.0, qgen),
            CoolingOutputW = Math.Max(0.0, qc),
            ThermalCop = qgen > 1e-12 ? Math.Max(0.0, qc) / qgen : 0.0
        }.Validate();
    }

    private static double NormalizedDistance(AbsorptionPerformanceMap map, AbsorptionMapPoint point, Query query)
    {
        var v = map.Validity;
        var dG = SpanNormalize(query.GeneratorTemperatureK - point.GeneratorTemperatureK, v.MinimumGeneratorTemperatureK, v.MaximumGeneratorTemperatureK);
        var dS = SpanNormalize(query.HeatSinkTemperatureK - point.HeatSinkTemperatureK, v.MinimumHeatSinkTemperatureK, v.MaximumHeatSinkTemperatureK);
        var dE = SpanNormalize(query.EvaporatorTemperatureK - point.EvaporatorTemperatureK, v.MinimumEvaporatorTemperatureK, v.MaximumEvaporatorTemperatureK);
        return Math.Sqrt(dG * dG + dS * dS + dE * dE);
    }

    private static double SpanNormalize(double delta, double min, double max)
        => delta / Math.Max(max - min, 1e-9);

    private static double Clamp(double value, double min, double max, ref bool outside)
    {
        if (value < min)
        {
            outside = true;
            return min;
        }

        if (value > max)
        {
            outside = true;
            return max;
        }

        return value;
    }
}
