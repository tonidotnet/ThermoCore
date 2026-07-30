namespace ThermoCore.AWG.Optimization;

/// <summary>Multi-objective Pareto helpers for sweep points (OPT-006).</summary>
public static class AwgParetoFront
{
    /// <summary>
    /// Non-dominated points maximizing liters/day and minimizing Wh/liter.
    /// Points without a positive Wh/liter value are ignored for this bi-objective front.
    /// </summary>
    public static IReadOnlyList<AwgParameterSweepPointResult> LitersPerDayVsWattHoursPerLiter(
        IEnumerable<AwgParameterSweepPointResult> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        var candidates = points
            .Where(p => p.Succeeded && p.WattHoursPerLiter is > 0)
            .ToArray();
        if (candidates.Length == 0)
        {
            return Array.Empty<AwgParameterSweepPointResult>();
        }

        var front = new List<AwgParameterSweepPointResult>();
        foreach (var candidate in candidates)
        {
            var dominated = candidates.Any(other =>
                !ReferenceEquals(other, candidate) && Dominates(other, candidate));
            if (!dominated)
            {
                front.Add(candidate);
            }
        }

        return front
            .OrderByDescending(p => p.LitersPerDay)
            .ThenBy(p => p.WattHoursPerLiter!.Value)
            .ToArray();
    }

    private static bool Dominates(
        AwgParameterSweepPointResult a,
        AwgParameterSweepPointResult b)
    {
        var aWh = a.WattHoursPerLiter!.Value;
        var bWh = b.WattHoursPerLiter!.Value;
        var notWorse = a.LitersPerDay >= b.LitersPerDay && aWh <= bWh;
        var better = a.LitersPerDay > b.LitersPerDay || aWh < bWh;
        return notWorse && better;
    }
}
