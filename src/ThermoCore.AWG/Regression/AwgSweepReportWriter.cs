using System.Globalization;
using System.Text;

namespace ThermoCore.AWG.Regression;

/// <summary>Writes CSV/Markdown/SVG summary visualizations for a 1-D sweep.</summary>
public static class AwgSweepReportWriter
{
    public static void Write(AwgSweepReport report, string directory)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);

        WriteCsv(report, Path.Combine(directory, "results.csv"));
        WriteSummaryTableSvg(report, Path.Combine(directory, "summary-table.svg"));
        WriteBarChartSvg(report, Path.Combine(directory, "results-bars-liters-per-day.svg"));
        WriteSummaryMarkdown(report, Path.Combine(directory, "SUMMARY.md"));
    }

    private static void WriteCsv(AwgSweepReport report, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "id,parameterName,parameterValue,parameterUnit,passed,waterKg,litersPerDay,finalBusPowerW,finalBatterySoc");
        foreach (var p in report.Points)
        {
            sb.Append(p.ScenarioId).Append(',')
                .Append(p.ParameterName).Append(',')
                .Append(p.ParameterValue.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                .Append(p.ParameterUnit).Append(',')
                .Append(p.Passed ? "true" : "false").Append(',')
                .Append(p.CollectedWaterKg.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                .Append(p.LitersPerDay.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                .Append((p.FinalBusPowerW ?? double.NaN).ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                .Append((p.FinalBatterySocFraction ?? double.NaN).ToString("G17", CultureInfo.InvariantCulture))
                .AppendLine();
        }

        File.WriteAllText(path, sb.ToString());
    }

    private static void WriteSummaryMarkdown(AwgSweepReport report, string path)
    {
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("# " + report.Title);
        sb.AppendLine();
        sb.AppendLine(report.BoundarySummary);
        sb.AppendLine();
        sb.AppendLine("## Összefoglaló táblázat (L/nap)");
        sb.AppendLine();
        sb.AppendLine("![Összefoglaló táblázat](summary-table.svg)");
        sb.AppendLine();
        sb.AppendLine("![L/nap oszlopdiagram](results-bars-liters-per-day.svg)");
        sb.AppendLine();
        sb.AppendLine($"| {report.ParameterName} ({report.ParameterUnit}) | Water (kg) | L/nap | Pass |");
        sb.AppendLine("|---:|---:|---:|:---:|");
        foreach (var p in report.Points)
        {
            sb.Append("| ").Append(p.ParameterValue.ToString("0.##", inv)).Append(" | ")
                .Append(p.CollectedWaterKg.ToString("G6", inv)).Append(" | ")
                .Append(p.LitersPerDay.ToString("0.####", inv)).Append(" | ")
                .Append(p.Passed ? "yes" : "no").AppendLine(" |");
        }

        sb.AppendLine();
        if (report.BestLitersPerDay is { } best)
        {
            sb.AppendLine(
                $"**Legjobb L/nap:** {best.LitersPerDay.ToString("G6", inv)} @ " +
                $"{best.ParameterValue.ToString("0.##", inv)} {report.ParameterUnit}");
            sb.AppendLine();
        }

        sb.AppendLine("Nyers adat: [`results.csv`](results.csv).");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine(report.ConsoleCommand);
        sb.AppendLine("```");

        File.WriteAllText(path, sb.ToString());
    }

    private static void WriteSummaryTableSvg(AwgSweepReport report, string path)
    {
        var inv = CultureInfo.InvariantCulture;
        var rowH = 34;
        var headerH = 56;
        var width = 520;
        var height = headerH + 48 + report.Points.Count * rowH + 36;
        var maxL = Math.Max(1e-9, report.Points.Max(p => p.LitersPerDay));

        var sb = new StringBuilder();
        sb.AppendLine($"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">""");
        sb.AppendLine("""<rect width="100%" height="100%" fill="#f8fafc"/>""");
        sb.AppendLine(
            $"""<text x="24" y="32" font-family="Segoe UI, Arial, sans-serif" font-size="16" font-weight="700" fill="#0f172a">{Escape(report.Title)}</text>""");
        sb.AppendLine(
            $"""<text x="24" y="52" font-family="Segoe UI, Arial, sans-serif" font-size="11" fill="#64748b">{Escape(report.BoundarySummary)}</text>""");

        var y0 = 72;
        sb.AppendLine(
            $"""<text x="32" y="{y0}" font-family="Segoe UI, Arial, sans-serif" font-size="12" font-weight="600" fill="#334155">{Escape(report.ParameterName)} ({Escape(report.ParameterUnit)})</text>""");
        sb.AppendLine(
            $"""<text x="220" y="{y0}" font-family="Segoe UI, Arial, sans-serif" font-size="12" font-weight="600" fill="#334155">L/nap</text>""");
        sb.AppendLine(
            $"""<text x="320" y="{y0}" font-family="Segoe UI, Arial, sans-serif" font-size="12" font-weight="600" fill="#334155">Water (kg)</text>""");

        for (var i = 0; i < report.Points.Count; i++)
        {
            var p = report.Points[i];
            var y = y0 + 18 + i * rowH;
            var barW = 140.0 * p.LitersPerDay / maxL;
            var fill = HeatColor(p.LitersPerDay / maxL);
            sb.AppendLine($"""<rect x="24" y="{y - 16}" width="{width - 48}" height="{rowH - 4}" rx="6" fill="#ffffff" stroke="#e2e8f0"/>""");
            sb.AppendLine(
                $"""<text x="40" y="{y + 4}" font-family="Segoe UI, Arial, sans-serif" font-size="13" fill="#0f172a">{p.ParameterValue.ToString("0.##", inv)}</text>""");
            sb.AppendLine($"""<rect x="220" y="{y - 8}" width="{barW.ToString("0.###", inv)}" height="14" rx="3" fill="{fill}"/>""");
            sb.AppendLine(
                $"""<text x="220" y="{y + 4}" font-family="Segoe UI, Arial, sans-serif" font-size="12" font-weight="600" fill="#0f172a">{p.LitersPerDay.ToString("0.####", inv)}</text>""");
            sb.AppendLine(
                $"""<text x="320" y="{y + 4}" font-family="Segoe UI, Arial, sans-serif" font-size="12" fill="#334155">{p.CollectedWaterKg.ToString("G5", inv)}</text>""");
        }

        sb.AppendLine("</svg>");
        File.WriteAllText(path, sb.ToString());
    }

    private static void WriteBarChartSvg(AwgSweepReport report, string path)
    {
        var inv = CultureInfo.InvariantCulture;
        const int width = 640;
        const int height = 360;
        const int left = 64;
        const int right = 24;
        const int top = 48;
        const int bottom = 56;
        var plotW = width - left - right;
        var plotH = height - top - bottom;
        var maxL = Math.Max(1e-9, report.Points.Max(p => p.LitersPerDay));
        var n = Math.Max(1, report.Points.Count);
        var slot = plotW / (double)n;
        var barW = slot * 0.62;

        var sb = new StringBuilder();
        sb.AppendLine($"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">""");
        sb.AppendLine("""<rect width="100%" height="100%" fill="#f8fafc"/>""");
        sb.AppendLine(
            $"""<text x="20" y="28" font-family="Segoe UI, Arial, sans-serif" font-size="15" font-weight="600" fill="#1f2933">{Escape(report.Title)} — L/nap</text>""");
        sb.AppendLine($"""<line x1="{left}" y1="{top}" x2="{left}" y2="{top + plotH}" stroke="#94a3b8"/>""");
        sb.AppendLine($"""<line x1="{left}" y1="{top + plotH}" x2="{left + plotW}" y2="{top + plotH}" stroke="#94a3b8"/>""");

        for (var tick = 0; tick <= 4; tick++)
        {
            var frac = tick / 4.0;
            var y = top + plotH * (1.0 - frac);
            var val = maxL * frac;
            sb.AppendLine($"""<line x1="{left}" y1="{y.ToString("0.###", inv)}" x2="{left + plotW}" y2="{y.ToString("0.###", inv)}" stroke="#e2e8f0"/>""");
            sb.AppendLine(
                $"""<text x="{left - 8}" y="{(y + 4).ToString("0.###", inv)}" text-anchor="end" font-family="Segoe UI, Arial, sans-serif" font-size="11" fill="#64748b">{val.ToString("0.###", inv)}</text>""");
        }

        for (var i = 0; i < report.Points.Count; i++)
        {
            var p = report.Points[i];
            var h = plotH * p.LitersPerDay / maxL;
            var x = left + i * slot + (slot - barW) / 2.0;
            var y = top + plotH - h;
            var fill = HeatColor(p.LitersPerDay / maxL);
            sb.AppendLine(
                $"""<rect x="{x.ToString("0.###", inv)}" y="{y.ToString("0.###", inv)}" width="{barW.ToString("0.###", inv)}" height="{Math.Max(0.5, h).ToString("0.###", inv)}" rx="4" fill="{fill}"/>""");
            sb.AppendLine(
                $"""<text x="{(x + barW / 2).ToString("0.###", inv)}" y="{top + plotH + 18}" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="12" fill="#334155">{p.ParameterValue.ToString("0.##", inv)}</text>""");
        }

        sb.AppendLine(
            $"""<text x="{left + plotW / 2}" y="{height - 12}" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="12" fill="#475569">{Escape(report.ParameterName)} ({Escape(report.ParameterUnit)})</text>""");
        sb.AppendLine("</svg>");
        File.WriteAllText(path, sb.ToString());
    }

    private static string HeatColor(double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        var r = (int)Math.Round(30 + 200 * t);
        var g = (int)Math.Round(120 + 40 * (1.0 - t));
        var b = (int)Math.Round(180 - 120 * t);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private static string Escape(string value)
        => value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
}
