using System.Globalization;
using System.Text;

namespace ThermoCore.AWG.Hybrid;

/// <summary>Writes hybrid comparison reports for offline review.</summary>
public static class HybridComparisonReportWriter
{
    public static void Write(HybridComparisonReport report, string directory)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);
        WriteCsv(report, Path.Combine(directory, "hybrid-comparison.csv"));
        WriteMarkdown(report, Path.Combine(directory, "hybrid-comparison.md"));
    }

    private static void WriteCsv(HybridComparisonReport report, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "scenarioId,variant,ambientTemperatureC,relativeHumidityPercent,inletTemperatureC,inletDewPointC," +
            "condensedKgPerSecond,exhaustedVaporKgPerSecond,desorbedVaporKgPerSecond," +
            "coolingDeliveredW,electricalInputW,bareDeviceCop,coolingPlantCop,litersPerKwhElectric," +
            "dewPointMarginK,passed,failureMessage");

        foreach (var p in report.Points)
        {
            sb.Append(Csv(p.ScenarioId)).Append(',')
                .Append(p.Variant).Append(',')
                .Append(F(p.AmbientTemperatureC)).Append(',')
                .Append(F(p.RelativeHumidityPercent)).Append(',')
                .Append(F(p.InletTemperatureC)).Append(',')
                .Append(F(p.InletDewPointC)).Append(',')
                .Append(F(p.CondensedWaterKgPerSecond)).Append(',')
                .Append(F(p.ExhaustedVaporKgPerSecond)).Append(',')
                .Append(p.DesorbedVaporKgPerSecond is { } d ? F(d) : string.Empty).Append(',')
                .Append(F(p.CoolingDeliveredW)).Append(',')
                .Append(F(p.ElectricalInputW)).Append(',')
                .Append(p.BareDeviceCop is { } b ? F(b) : string.Empty).Append(',')
                .Append(p.CoolingPlantCop is { } c ? F(c) : string.Empty).Append(',')
                .Append(p.LitersPerKwhElectric is { } l ? F(l) : string.Empty).Append(',')
                .Append(p.DewPointMarginK is { } m ? F(m) : string.Empty).Append(',')
                .Append(p.Passed ? "true" : "false").Append(',')
                .Append(Csv(p.FailureMessage ?? string.Empty))
                .AppendLine();
        }

        File.WriteAllText(path, sb.ToString());
    }

    private static void WriteMarkdown(HybridComparisonReport report, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Hybrid comparison report (R6-001)");
        sb.AppendLine();
        sb.AppendLine($"Points: {report.Points.Count}, passed: {report.PassedPoints.Count}");
        if (report.BestLitersPerKwhElectric is { } bestL)
        {
            sb.AppendLine($"Best L/kWh_electric: `{bestL.ScenarioId}` = {bestL.LitersPerKwhElectric:G4}");
        }

        if (report.BestWaterRate is { } bestW)
        {
            sb.AppendLine($"Best water rate: `{bestW.ScenarioId}` = {bestW.CondensedWaterKgPerSecond:G4} kg/s");
        }

        sb.AppendLine();
        sb.AppendLine("| Scenario | Variant | Condensed kg/s | Exhausted kg/s | L/kWh | Plant COP | Pass |");
        sb.AppendLine("|---|---|---:|---:|---:|---:|---|");
        foreach (var p in report.Points)
        {
            sb.Append("| ").Append(p.ScenarioId)
                .Append(" | ").Append(p.Variant)
                .Append(" | ").Append(p.CondensedWaterKgPerSecond.ToString("G4", CultureInfo.InvariantCulture))
                .Append(" | ").Append(p.ExhaustedVaporKgPerSecond.ToString("G4", CultureInfo.InvariantCulture))
                .Append(" | ").Append(p.LitersPerKwhElectric?.ToString("G4", CultureInfo.InvariantCulture) ?? "—")
                .Append(" | ").Append(p.CoolingPlantCop?.ToString("G4", CultureInfo.InvariantCulture) ?? "—")
                .Append(" | ").Append(p.Passed ? "yes" : "no")
                .AppendLine(" |");
        }

        File.WriteAllText(path, sb.ToString());
    }

    private static string F(double value)
        => value.ToString("G17", CultureInfo.InvariantCulture);

    private static string Csv(string value)
        => value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
}
