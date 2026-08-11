namespace ThermoCore.AWG.Simulation;

/// <summary>Formats an <see cref="AwgRunSummary"/> for console output.</summary>
public static class AwgRunSummaryFormatter
{
    public static string Format(AwgRunSummary summary, AwgSystemBalanceReport? balanceReport = null)
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

        if (balanceReport is not null)
        {
            lines.Add(
                $"Balance check: water={(balanceReport.WaterBalancePassed ? "PASS" : "FAIL")} " +
                $"(max|{balanceReport.MaxAbsWaterResidualKg:E3}| kg)  " +
                $"energy={(balanceReport.EnergyBalancePassed ? "PASS" : "FAIL")} " +
                $"(max|{balanceReport.MaxAbsEnergyResidualJ:E3}| J)  " +
                $"dry-air={(balanceReport.DryAirBalancePassed ? "PASS" : "FAIL")}");
        }

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

        if (summary.LitersPerDay is { } lpd)
        {
            lines.Add($"Water yield: {lpd:G4} L/day");
        }

        if (summary.LitersPerKwhElectric is { } lPerKwhE)
        {
            lines.Add(
                $"L/kWh_electric: {lPerKwhE:G4}" +
                (summary.WattHoursElectricPerLiter is { } whL
                    ? $" ({whL:G4} Wh_e/L)"
                    : string.Empty));
        }

        if (summary.LitersPerKwhSolarPrimary is { } lPerKwhS)
        {
            lines.Add(
                $"L/kWh_solar_primary: {lPerKwhS:G4}" +
                " (denominator = incident collector-aperture solar; recovered heat excluded)");
        }

        if (summary.LitersPerDayPerSquareMeterAperture is { } lPerM2)
        {
            lines.Add($"L/day/m² aperture: {lPerM2:G4}");
        }

        if (summary.WaterRecoveryFraction is { } recovery)
        {
            lines.Add($"WaterRecoveryFraction: {recovery:P2}");
        }

        if (summary.DesorptionCaptureFraction is { } capture)
        {
            lines.Add($"DesorptionCaptureFraction: {capture:P2}");
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
