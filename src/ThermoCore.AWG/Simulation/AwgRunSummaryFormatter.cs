namespace ThermoCore.AWG.Simulation;

/// <summary>Formats an <see cref="AwgRunSummary"/> for console output.</summary>
public static class AwgRunSummaryFormatter
{
    public static string Format(AwgRunSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        var lines = new List<string>
        {
            "=== ThermoCore AWG Simulation Summary ===",
            $"Succeeded: {summary.Succeeded}",
            $"Topology: {summary.TopologyId} v{summary.TopologyVersion}",
            $"Fingerprint: {summary.GraphFingerprint}",
            $"Components / connections: {summary.ComponentCount} / {summary.ConnectionCount}",
            $"Steps: {summary.CompletedSteps}  Duration: {summary.Duration.TotalSeconds:F0} s  dt: {summary.TimeStep.TotalSeconds:F0} s",
            $"Residuals: E={summary.AggregatedEnergyResidualJ:E3} J  W={summary.AggregatedWaterResidualKg:E3} kg  DA={summary.AggregatedDryAirResidualKg:E3} kg",
            $"Diagnostics: warnings={summary.WarningCount} errors={summary.ErrorCount}"
        };

        if (summary.FinalBusPowerW is { } bus)
        {
            lines.Add($"Electrical bus power: {bus:F2} W");
        }

        if (summary.FinalCurtailedPowerW is { } curtail)
        {
            lines.Add($"Curtailed power: {curtail:F2} W");
        }

        if (summary.FinalWaterTankContentKg is { } tankKg)
        {
            lines.Add(
                $"Water tank: {tankKg:F4} kg" +
                (summary.FinalWaterTankLevelFraction is { } level
                    ? $" ({level:P1} full)"
                    : string.Empty));
        }

        if (summary.FinalMoistAirTemperaturesC.Count > 0)
        {
            lines.Add("Final moist-air temperatures (°C):");
            foreach (var pair in summary.FinalMoistAirTemperaturesC.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                var humidity = summary.FinalHumidityRatiosKgPerKg.TryGetValue(pair.Key, out var w) ? w : null;
                lines.Add(
                    humidity is null
                        ? $"  {pair.Key}: T={pair.Value:F2}"
                        : $"  {pair.Key}: T={pair.Value:F2}, W={humidity:F6} kg/kg");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }
}
