using System.Globalization;
using System.Text;

namespace ThermoCore.AWG.Regression;

/// <summary>Writes CSV/Markdown/SVG summary visualizations for an ambient T×RH matrix.</summary>
public static class AwgAmbientMatrixReportWriter
{
    public static void Write(AwgAmbientMatrixReport report, string directory)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);

        WriteCsv(report, Path.Combine(directory, "results.csv"));
        WriteSummaryTableSvg(report, Path.Combine(directory, "summary-table.svg"));
        WriteHeatmapSvg(report, Path.Combine(directory, "results-heatmap-liters-per-day.svg"));
        WriteSummaryMarkdown(report, Path.Combine(directory, "SUMMARY.md"));
    }

    private static void WriteCsv(AwgAmbientMatrixReport report, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "id,ambientTemperatureC,relativeHumidityPercent,passed,waterKg,litersPerDay,finalBusPowerW,finalBatterySoc");
        foreach (var p in report.Points)
        {
            sb.Append(p.ScenarioId).Append(',')
                .Append(p.AmbientTemperatureC.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                .Append(p.RelativeHumidityPercent.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                .Append(p.Passed ? "true" : "false").Append(',')
                .Append(p.CollectedWaterKg.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                .Append(p.LitersPerDay.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                .Append((p.FinalBusPowerW ?? double.NaN).ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                .Append((p.FinalBatterySocFraction ?? double.NaN).ToString("G17", CultureInfo.InvariantCulture))
                .AppendLine();
        }

        File.WriteAllText(path, sb.ToString());
    }

    private static void WriteSummaryMarkdown(AwgAmbientMatrixReport report, string path)
    {
        var inv = CultureInfo.InvariantCulture;
        var temps = report.Points.Select(p => p.AmbientTemperatureC).Distinct().OrderBy(x => x).ToArray();
        var rhs = report.Points.Select(p => p.RelativeHumidityPercent).Distinct().OrderBy(x => x).ToArray();
        var lookup = report.Points.ToDictionary(p => (p.AmbientTemperatureC, p.RelativeHumidityPercent));

        var sb = new StringBuilder();
        sb.AppendLine("# Full AWG ambient matrix — összefoglaló");
        sb.AppendLine();
        sb.AppendLine(
            "Bemenet: **T = 20–35 °C**, **RH = 30–60%**, controlled Full AWG V3 (Adsorption/Regeneration, " +
            "electrical, no HR), ṁ = 0.02 kg/s, G = 950 W/m², silica = 2 kg regenerated start, SOC = 90%, 2 h.");
        sb.AppendLine();
        sb.AppendLine("## Összefoglaló táblázat (L/nap)");
        sb.AppendLine();
        sb.AppendLine("![Összefoglaló táblázat](summary-table.svg)");
        sb.AppendLine();
        sb.AppendLine("![L/nap heatmap](results-heatmap-liters-per-day.svg)");
        sb.AppendLine();
        sb.AppendLine("## L/nap mátrix");
        sb.AppendLine();
        sb.Append("| T (°C) \\ RH (%) |");
        foreach (var rh in rhs)
        {
            sb.Append(' ').Append(rh.ToString("0", inv)).Append(" |");
        }

        sb.AppendLine();
        sb.Append('|');
        sb.Append("---:|");
        foreach (var _ in rhs)
        {
            sb.Append("---:|");
        }

        sb.AppendLine();
        foreach (var t in temps)
        {
            sb.Append("| **").Append(t.ToString("0", inv)).Append("** |");
            foreach (var rh in rhs)
            {
                var point = lookup[(t, rh)];
                sb.Append(' ').Append(point.LitersPerDay.ToString("0.####", inv)).Append(" |");
            }

            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("## Részletes eredmények");
        sb.AppendLine();
        sb.AppendLine("| T (°C) | RH (%) | Water (kg) | L/nap | Bus (W) | SOC | Pass |");
        sb.AppendLine("|---:|---:|---:|---:|---:|---:|:---:|");
        foreach (var p in report.Points)
        {
            sb.Append("| ").Append(p.AmbientTemperatureC.ToString("0", inv))
                .Append(" | ").Append(p.RelativeHumidityPercent.ToString("0", inv))
                .Append(" | ").Append(p.CollectedWaterKg.ToString("0.######", inv))
                .Append(" | ").Append(p.LitersPerDay.ToString("0.####", inv))
                .Append(" | ").Append((p.FinalBusPowerW ?? 0).ToString("0.##", inv))
                .Append(" | ").Append((p.FinalBatterySocFraction ?? 0).ToString("0.###", inv))
                .Append(" | ").Append(p.Passed ? "yes" : "no")
                .AppendLine(" |");
        }

        if (report.BestLitersPerDay is { } best)
        {
            sb.AppendLine();
            sb.AppendLine(
                $"**Legjobb L/nap:** {best.LitersPerDay.ToString("G6", inv)} " +
                $"@ {best.AmbientTemperatureC.ToString("0", inv)} °C, RH {best.RelativeHumidityPercent.ToString("0", inv)}%");
        }

        sb.AppendLine();
        sb.AppendLine("Nyers adat: [`results.csv`](results.csv).");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("dotnet run --project src/ThermoCore.Console -- full-flow-ambient-matrix");
        sb.AppendLine("```");
        File.WriteAllText(path, sb.ToString());
    }

    private static void WriteSummaryTableSvg(AwgAmbientMatrixReport report, string path)
    {
        var inv = CultureInfo.InvariantCulture;
        var temps = report.Points.Select(p => p.AmbientTemperatureC).Distinct().OrderBy(x => x).ToArray();
        var rhs = report.Points.Select(p => p.RelativeHumidityPercent).Distinct().OrderBy(x => x).ToArray();
        var lookup = report.Points.ToDictionary(p => (p.AmbientTemperatureC, p.RelativeHumidityPercent));
        var values = report.Points.Select(p => p.LitersPerDay).ToArray();
        var min = values.Min();
        var max = values.Max();
        if (Math.Abs(max - min) < 1e-18)
        {
            max = min + 1;
        }

        const int cellW = 92;
        const int cellH = 48;
        const int left = 100;
        const int top = 90;
        var width = left + rhs.Length * cellW + 36;
        var height = top + temps.Length * cellH + 100;

        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine(
            $"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">""");
        sb.AppendLine("""<rect width="100%" height="100%" fill="#f8fafc"/>""");
        sb.AppendLine(
            """<text x="24" y="32" font-family="Segoe UI, Arial, sans-serif" font-size="18" font-weight="700" fill="#0f172a">Full AWG — összefoglaló táblázat (L/nap)</text>""");
        sb.AppendLine(
            """<text x="24" y="54" font-family="Segoe UI, Arial, sans-serif" font-size="12" fill="#475569">Bemenet: T × RH · HR+electrical · ṁ=0.02 kg/s · G=950 · silica 2 kg · 30 s</text>""");
        sb.AppendLine(
            """<text x="24" y="74" font-family="Segoe UI, Arial, sans-serif" font-size="11" fill="#64748b">Sorok = bemeneti hőmérséklet (°C) · Oszlopok = relatív páratartalom (%)</text>""");

        for (var j = 0; j < rhs.Length; j++)
        {
            var x = left + j * cellW + cellW / 2;
            sb.AppendLine(
                $"""<text x="{x}" y="{top - 14}" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="12" font-weight="600" fill="#1e293b">RH {rhs[j].ToString("0", inv)}%</text>""");
        }

        for (var i = 0; i < temps.Length; i++)
        {
            var y = top + i * cellH + cellH / 2 + 4;
            sb.AppendLine(
                $"""<text x="{left - 12}" y="{y}" text-anchor="end" font-family="Segoe UI, Arial, sans-serif" font-size="12" font-weight="600" fill="#1e293b">{temps[i].ToString("0", inv)} °C</text>""");
            for (var j = 0; j < rhs.Length; j++)
            {
                var point = lookup[(temps[i], rhs[j])];
                var t = (point.LitersPerDay - min) / (max - min);
                var x = left + j * cellW;
                var cy = top + i * cellH;
                var fill = HeatColor(t);
                var textFill = t > 0.55 ? "#fff7ed" : "#0f172a";
                sb.AppendLine(
                    $"""<rect x="{x + 3}" y="{cy + 3}" width="{cellW - 6}" height="{cellH - 6}" rx="8" fill="{fill}" stroke="#ffffff" stroke-width="1.5"/>""");
                sb.AppendLine(
                    $"""<text x="{x + cellW / 2}" y="{cy + cellH / 2 + 1}" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="13" font-weight="700" fill="{textFill}">{point.LitersPerDay.ToString("0.###", inv)}</text>""");
                sb.AppendLine(
                    $"""<text x="{x + cellW / 2}" y="{cy + cellH / 2 + 16}" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="9" fill="{textFill}">L/nap</text>""");
            }
        }

        if (report.BestLitersPerDay is { } best)
        {
            sb.AppendLine(
                $"""<text x="24" y="{height - 28}" font-family="Segoe UI, Arial, sans-serif" font-size="12" fill="#0f172a">Maximum: {best.LitersPerDay.ToString("0.####", inv)} L/nap @ {best.AmbientTemperatureC.ToString("0", inv)} °C, RH {best.RelativeHumidityPercent.ToString("0", inv)}%</text>""");
        }

        sb.AppendLine("</svg>");
        File.WriteAllText(path, sb.ToString());
    }

    private static void WriteHeatmapSvg(AwgAmbientMatrixReport report, string path)
    {
        // Reuse the same visual language as the summary table (compact heatmap without "L/nap" subtitle in cells).
        var inv = CultureInfo.InvariantCulture;
        var temps = report.Points.Select(p => p.AmbientTemperatureC).Distinct().OrderBy(x => x).ToArray();
        var rhs = report.Points.Select(p => p.RelativeHumidityPercent).Distinct().OrderBy(x => x).ToArray();
        var lookup = report.Points.ToDictionary(p => (p.AmbientTemperatureC, p.RelativeHumidityPercent));
        var min = report.Points.Min(p => p.LitersPerDay);
        var max = report.Points.Max(p => p.LitersPerDay);
        if (Math.Abs(max - min) < 1e-18)
        {
            max = min + 1;
        }

        const int cellW = 88;
        const int cellH = 48;
        const int left = 90;
        const int top = 70;
        var width = left + rhs.Length * cellW + 40;
        var height = top + temps.Length * cellH + 80;
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine(
            $"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">""");
        sb.AppendLine("""<rect width="100%" height="100%" fill="#f7f5f1"/>""");
        sb.AppendLine(
            """<text x="20" y="28" font-family="Segoe UI, Arial, sans-serif" font-size="16" font-weight="600" fill="#1f2933">Full AWG ambient matrix — L/nap heatmap</text>""");
        sb.AppendLine(
            """<text x="20" y="48" font-family="Segoe UI, Arial, sans-serif" font-size="11" fill="#52606d">T (°C) × RH (%) · 30 s extrapolált L/nap</text>""");
        for (var j = 0; j < rhs.Length; j++)
        {
            sb.AppendLine(
                $"""<text x="{left + j * cellW + cellW / 2}" y="{top - 12}" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="12" fill="#334e68">{rhs[j].ToString("0", inv)}%</text>""");
        }

        for (var i = 0; i < temps.Length; i++)
        {
            sb.AppendLine(
                $"""<text x="{left - 12}" y="{top + i * cellH + cellH / 2 + 4}" text-anchor="end" font-family="Segoe UI, Arial, sans-serif" font-size="12" fill="#334e68">{temps[i].ToString("0", inv)} °C</text>""");
            for (var j = 0; j < rhs.Length; j++)
            {
                var v = lookup[(temps[i], rhs[j])].LitersPerDay;
                var t = (v - min) / (max - min);
                var x = left + j * cellW;
                var y = top + i * cellH;
                sb.AppendLine(
                    $"""<rect x="{x + 4}" y="{y + 4}" width="{cellW - 8}" height="{cellH - 8}" rx="6" fill="{HeatColor(t)}" stroke="#fff" stroke-width="1"/>""");
                sb.AppendLine(
                    $"""<text x="{x + cellW / 2}" y="{y + cellH / 2 + 4}" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="12" font-weight="600" fill="#102a43">{v.ToString("0.###", inv)}</text>""");
            }
        }

        sb.AppendLine("</svg>");
        File.WriteAllText(path, sb.ToString());
    }

    private static string HeatColor(double t)
    {
        t = Math.Clamp(t, 0, 1);
        int r, g, b;
        if (t < 0.5)
        {
            var u = t / 0.5;
            r = (int)(40 + u * 200);
            g = (int)(110 + u * 90);
            b = (int)(200 - u * 150);
        }
        else
        {
            var u = (t - 0.5) / 0.5;
            r = (int)(240 - u * 20);
            g = (int)(200 - u * 150);
            b = (int)(50 - u * 30);
        }

        return $"#{r:X2}{g:X2}{b:X2}";
    }
}
