using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Thermoelectric;

/// <summary>Clamped inverse-distance interpolation over empirical map points.</summary>
public static class CommercialPeltierMapInterpolator
{
    public readonly record struct Query(
        double InletTemperatureK,
        double InletRelativeHumidityFraction,
        double ElectricalPowerW,
        double? DryAirMassFlowKgPerSecond);

    public readonly record struct Result(
        CommercialPeltierMapPoint Interpolated,
        Query ClampedQuery,
        bool OutsideValidity);

    public static Result Evaluate(CommercialPeltierDehumidifierProfile profile, Query query)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.Validate();
        FiniteNumber.RequirePositive(query.InletTemperatureK, nameof(query.InletTemperatureK));
        FiniteNumber.Require(query.InletRelativeHumidityFraction, nameof(query.InletRelativeHumidityFraction));
        FiniteNumber.RequireNonNegative(query.ElectricalPowerW, nameof(query.ElectricalPowerW));

        var validity = profile.Validity;
        var outside = false;
        var t = Clamp(query.InletTemperatureK, validity.MinimumInletTemperatureK, validity.MaximumInletTemperatureK, ref outside);
        var rh = Clamp(query.InletRelativeHumidityFraction, validity.MinimumInletRelativeHumidityFraction, validity.MaximumInletRelativeHumidityFraction, ref outside);
        var pe = Clamp(query.ElectricalPowerW, validity.MinimumElectricalPowerW, validity.MaximumElectricalPowerW, ref outside);
        double? flow = query.DryAirMassFlowKgPerSecond;
        if (profile.SupportsAirflowAxis
            && flow is { } f
            && validity.MinimumDryAirMassFlowKgPerSecond is { } fMin
            && validity.MaximumDryAirMassFlowKgPerSecond is { } fMax)
        {
            flow = Clamp(f, fMin, fMax, ref outside);
        }

        var clamped = new Query(t, rh, pe, flow);
        var point = Interpolate(profile, clamped);
        return new Result(point, clamped, outside);
    }

    private static CommercialPeltierMapPoint Interpolate(
        CommercialPeltierDehumidifierProfile profile,
        Query query)
    {
        var weights = new double[profile.MapPoints.Count];
        var weightSum = 0.0;
        for (var i = 0; i < profile.MapPoints.Count; i++)
        {
            var distance = NormalizedDistance(profile, profile.MapPoints[i], query);
            if (distance < 1e-12)
            {
                return profile.MapPoints[i];
            }

            var w = 1.0 / (distance * distance);
            weights[i] = w;
            weightSum += w;
        }

        double Water = 0, Power = 0, Tin = 0, RHin = 0;
        double? Tout = 0, RHout = 0, Flow = 0, Tcold = 0, Thot = 0;
        var hasTout = false;
        var hasRHout = false;
        var hasFlow = false;
        var hasTcold = false;
        var hasThot = false;

        for (var i = 0; i < profile.MapPoints.Count; i++)
        {
            var w = weights[i] / weightSum;
            var p = profile.MapPoints[i];
            Water += w * p.WaterProductionRateKgPerSecond;
            Power += w * p.ElectricalPowerW;
            Tin += w * p.InletTemperatureK;
            RHin += w * p.InletRelativeHumidityFraction;
            if (p.OutletTemperatureK is { } ot)
            {
                Tout = (Tout ?? 0) + w * ot;
                hasTout = true;
            }

            if (p.OutletRelativeHumidityFraction is { } orh)
            {
                RHout = (RHout ?? 0) + w * orh;
                hasRHout = true;
            }

            if (p.DryAirMassFlowKgPerSecond is { } fl)
            {
                Flow = (Flow ?? 0) + w * fl;
                hasFlow = true;
            }

            if (p.ColdSurfaceTemperatureK is { } tc)
            {
                Tcold = (Tcold ?? 0) + w * tc;
                hasTcold = true;
            }

            if (p.HotSideTemperatureK is { } th)
            {
                Thot = (Thot ?? 0) + w * th;
                hasThot = true;
            }
        }

        return new CommercialPeltierMapPoint
        {
            InletTemperatureK = Tin,
            InletRelativeHumidityFraction = RHin,
            ElectricalPowerW = Power,
            WaterProductionRateKgPerSecond = Math.Max(0.0, Water),
            DryAirMassFlowKgPerSecond = hasFlow ? Flow : query.DryAirMassFlowKgPerSecond,
            OutletTemperatureK = hasTout ? Tout : null,
            OutletRelativeHumidityFraction = hasRHout ? RHout : null,
            ColdSurfaceTemperatureK = hasTcold ? Tcold : null,
            HotSideTemperatureK = hasThot ? Thot : null
        }.Validate();
    }

    private static double NormalizedDistance(
        CommercialPeltierDehumidifierProfile profile,
        CommercialPeltierMapPoint point,
        Query query)
    {
        var v = profile.Validity;
        var dT = SpanNormalize(query.InletTemperatureK - point.InletTemperatureK, v.MinimumInletTemperatureK, v.MaximumInletTemperatureK);
        var dRh = SpanNormalize(query.InletRelativeHumidityFraction - point.InletRelativeHumidityFraction, v.MinimumInletRelativeHumidityFraction, v.MaximumInletRelativeHumidityFraction);
        var dP = SpanNormalize(query.ElectricalPowerW - point.ElectricalPowerW, v.MinimumElectricalPowerW, v.MaximumElectricalPowerW);
        var sum = dT * dT + dRh * dRh + dP * dP;
        if (profile.SupportsAirflowAxis
            && query.DryAirMassFlowKgPerSecond is { } qf
            && point.DryAirMassFlowKgPerSecond is { } pf
            && v.MinimumDryAirMassFlowKgPerSecond is { } fMin
            && v.MaximumDryAirMassFlowKgPerSecond is { } fMax)
        {
            var dF = SpanNormalize(qf - pf, fMin, fMax);
            sum += dF * dF;
        }

        return Math.Sqrt(sum);
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
