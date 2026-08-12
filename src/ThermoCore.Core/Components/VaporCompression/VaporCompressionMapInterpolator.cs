using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.VaporCompression;

/// <summary>Clamped / rejected inverse-distance interpolation over VC map points.</summary>
public static class VaporCompressionMapInterpolator
{
    public readonly record struct Query(
        double EvaporatingTemperatureK,
        double CondensingTemperatureK,
        double SpeedFraction);

    public readonly record struct Result(
        VaporCompressionMapPoint Interpolated,
        Query EffectiveQuery,
        bool OutsideValidity,
        bool Rejected);

    public static Result Evaluate(VaporCompressionPerformanceMap map, Query query)
    {
        ArgumentNullException.ThrowIfNull(map);
        map.Validate();
        FiniteNumber.RequirePositive(query.EvaporatingTemperatureK, nameof(query.EvaporatingTemperatureK));
        FiniteNumber.RequirePositive(query.CondensingTemperatureK, nameof(query.CondensingTemperatureK));
        FiniteNumber.Require(query.SpeedFraction, nameof(query.SpeedFraction));

        var validity = map.Validity;
        var outside = false;
        var te = Clamp(query.EvaporatingTemperatureK, validity.MinimumEvaporatingTemperatureK, validity.MaximumEvaporatingTemperatureK, ref outside);
        var tc = Clamp(query.CondensingTemperatureK, validity.MinimumCondensingTemperatureK, validity.MaximumCondensingTemperatureK, ref outside);
        var speed = Clamp(query.SpeedFraction, validity.MinimumSpeedFraction, validity.MaximumSpeedFraction, ref outside);

        if (outside && map.ExtrapolationPolicy == VaporCompressionExtrapolationPolicy.Reject)
        {
            // Placeholder capacities are zero; temperatures are sanitized only so Validate() can run.
            var teSafe = Math.Min(query.EvaporatingTemperatureK, query.CondensingTemperatureK - 1.0);
            if (teSafe <= 0.0)
            {
                teSafe = Math.Max(1.0, query.CondensingTemperatureK * 0.5);
            }

            return new Result(
                new VaporCompressionMapPoint
                {
                    EvaporatingTemperatureK = teSafe,
                    CondensingTemperatureK = Math.Max(query.CondensingTemperatureK, teSafe + 1.0),
                    SpeedFraction = Math.Clamp(query.SpeedFraction, 0.0, 1.0),
                    CoolingCapacityW = 0.0,
                    ElectricalPowerW = 0.0,
                    Cop = 0.0
                }.Validate(),
                query,
                OutsideValidity: true,
                Rejected: true);
        }

        var effective = new Query(te, tc, speed);
        var point = Interpolate(map, effective);
        return new Result(point, effective, outside, Rejected: false);
    }

    private static VaporCompressionMapPoint Interpolate(VaporCompressionPerformanceMap map, Query query)
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

        double qc = 0, pe = 0, te = 0, tc = 0, speed = 0;
        for (var i = 0; i < map.MapPoints.Count; i++)
        {
            var w = weights[i] / weightSum;
            var p = map.MapPoints[i];
            qc += w * p.CoolingCapacityW;
            pe += w * p.ElectricalPowerW;
            te += w * p.EvaporatingTemperatureK;
            tc += w * p.CondensingTemperatureK;
            speed += w * p.SpeedFraction;
        }

        return new VaporCompressionMapPoint
        {
            EvaporatingTemperatureK = te,
            CondensingTemperatureK = tc,
            SpeedFraction = Math.Clamp(speed, 0.0, 1.0),
            CoolingCapacityW = Math.Max(0.0, qc),
            ElectricalPowerW = Math.Max(0.0, pe),
            Cop = pe > 1e-12 ? Math.Max(0.0, qc) / pe : 0.0
        }.Validate();
    }

    private static double NormalizedDistance(
        VaporCompressionPerformanceMap map,
        VaporCompressionMapPoint point,
        Query query)
    {
        var v = map.Validity;
        var dE = SpanNormalize(query.EvaporatingTemperatureK - point.EvaporatingTemperatureK, v.MinimumEvaporatingTemperatureK, v.MaximumEvaporatingTemperatureK);
        var dC = SpanNormalize(query.CondensingTemperatureK - point.CondensingTemperatureK, v.MinimumCondensingTemperatureK, v.MaximumCondensingTemperatureK);
        var dS = SpanNormalize(query.SpeedFraction - point.SpeedFraction, v.MinimumSpeedFraction, v.MaximumSpeedFraction);
        return Math.Sqrt(dE * dE + dC * dC + dS * dS);
    }

    private static double SpanNormalize(double delta, double min, double max)
    {
        var span = Math.Max(max - min, 1e-9);
        return delta / span;
    }

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
