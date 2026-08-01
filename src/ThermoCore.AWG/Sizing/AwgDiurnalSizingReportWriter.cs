using System.Globalization;
using System.Text;

namespace ThermoCore.AWG.Sizing;

/// <summary>Writes summer-diurnal sizing CSV/SVG/Markdown artifacts.</summary>
public static class AwgDiurnalSizingReportWriter
{
    public static void Write(AwgDiurnalSizingReport report, string directory)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);

        WriteSummaryMarkdown(report, Path.Combine(directory, "SUMMARY.md"));
        if (!report.SimulationSucceeded)
        {
            return;
        }

        WriteCsv(report, Path.Combine(directory, "sizing-results.csv"));
        WriteHourlyCsv(report, Path.Combine(directory, "hourly-profile.csv"));
        WriteSizingTableSvg(report, Path.Combine(directory, "sizing-table.svg"));
        WriteHourlyBarsSvg(report, Path.Combine(directory, "hourly-water-bars.svg"));
    }

    private static void WriteCsv(AwgDiurnalSizingReport report, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "targetLPerDay,scale,dailyWh,whPerLiter,pvRatedW,pvAreaM2,batteryWh,nightWh,feasible,notes");
        foreach (var t in report.Targets)
        {
            sb.Append(F(t.TargetLitersPerDay)).Append(',')
                .Append(F(t.ScaleFactorVersusBaseline)).Append(',')
                .Append(F(t.DailyElectricalEnergyWh)).Append(',')
                .Append(F(t.SpecificEnergyWhPerLiter)).Append(',')
                .Append(F(t.RecommendedPvRatedPowerW)).Append(',')
                .Append(F(t.RecommendedPvAreaM2)).Append(',')
                .Append(F(t.RecommendedBatteryCapacityWh)).Append(',')
                .Append(F(t.NightElectricalEnergyWh)).Append(',')
                .Append(t.Feasible ? "true" : "false").Append(',')
                .Append('"').Append((t.Notes ?? string.Empty).Replace("\"", "'", StringComparison.Ordinal))
                .Append('"').AppendLine();
        }

        File.WriteAllText(path, sb.ToString());
    }

    private static void WriteHourlyCsv(AwgDiurnalSizingReport report, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("hour,temperatureC,rhPercent,ghiWPerM2,waterKg,dominantMode,meanPeltierW,fanOnFraction");
        foreach (var h in report.HourlySamples)
        {
            sb.Append(h.HourOfDay).Append(',')
                .Append(F(h.AmbientTemperatureC)).Append(',')
                .Append(F(h.RelativeHumidityPercent)).Append(',')
                .Append(F(h.IrradianceWPerM2)).Append(',')
                .Append(F(h.WaterProducedKg)).Append(',')
                .Append(h.DominantMode).Append(',')
                .Append(F(h.MeanPeltierW)).Append(',')
                .Append(F(h.MeanFanOnFraction)).AppendLine();
        }

        File.WriteAllText(path, sb.ToString());
    }

    private static void WriteSummaryMarkdown(AwgDiurnalSizingReport report, string path)
    {
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("# Nyári diurnal AWG — szimuláció és méretezés");
        sb.AppendLine();
        sb.AppendLine(
            "Átlagos nyári nap: **nappal ~32 °C / 30% RH**, **éjjel ~20 °C / 60% RH**, " +
            "GHI csúcs 950 W/m² (06–18 h). Kontrollált Adsorption ↔ Regeneration, 24 h.");
        sb.AppendLine();
        if (!report.SimulationSucceeded)
        {
            sb.AppendLine("**Szimuláció sikertelen:** " + (report.FailureMessage ?? "ismeretlen hiba"));
            File.WriteAllText(path, sb.ToString());
            return;
        }

        sb.AppendLine("## Baseline (24 h)");
        sb.AppendLine();
        sb.AppendLine($"| Metrika | Érték |");
        sb.AppendLine("|---|---:|");
        sb.AppendLine($"| Termelt víz | {report.BaselineWaterLiters.ToString("0.####", inv)} L/nap |");
        sb.AppendLine($"| Napi villamos energia | {report.BaselineDailyElectricalWh.ToString("0.#", inv)} Wh |");
        sb.AppendLine($"| Ebből busz (ventilátor+ctrl) | {report.BaselineBusLoadWh.ToString("0.#", inv)} Wh |");
        sb.AppendLine($"| Ebből Peltier (proxy) | {report.BaselinePeltierElectricalWh.ToString("0.#", inv)} Wh |");
        sb.AppendLine($"| PV termelés | {report.BaselinePvGenerationWh.ToString("0.#", inv)} Wh |");
        sb.AppendLine($"| Éjszakai energia (GHI&lt;50) | {report.BaselineNightElectricalWh.ToString("0.#", inv)} Wh |");
        sb.AppendLine($"| Fajlagos energia | {report.SpecificEnergyWhPerLiter.ToString("0.#", inv)} Wh/L |");
        sb.AppendLine($"| Peak-sun-hours (profil) | {report.PeakSunHours.ToString("0.##", inv)} h |");
        sb.AppendLine();
        sb.AppendLine("## Méretezés célhoz (0.5 / 1 / 2 / 3 L/nap)");
        sb.AppendLine();
        sb.AppendLine("![Méretezési táblázat](sizing-table.svg)");
        sb.AppendLine();
        sb.AppendLine("| Cél (L/nap) | Napi Wh | Wh/L | PV (W) | PV (m²) | Akkumulátor (Wh) | Éjszakai Wh |");
        sb.AppendLine("|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var t in report.Targets)
        {
            sb.Append("| ").Append(t.TargetLitersPerDay.ToString("0.#", inv)).Append(" | ")
                .Append(t.DailyElectricalEnergyWh.ToString("0.#", inv)).Append(" | ")
                .Append(t.SpecificEnergyWhPerLiter.ToString("0.#", inv)).Append(" | ")
                .Append(t.RecommendedPvRatedPowerW.ToString("0", inv)).Append(" | ")
                .Append(t.RecommendedPvAreaM2.ToString("0.00", inv)).Append(" | ")
                .Append(t.RecommendedBatteryCapacityWh.ToString("0", inv)).Append(" | ")
                .Append(t.NightElectricalEnergyWh.ToString("0.#", inv)).AppendLine(" |");
        }

        sb.AppendLine();
        sb.AppendLine("A méretezés a baseline fajlagos energia **lineáris skálázása**; beszerzés előtt célpontra újra kell szimulálni.");
        sb.AppendLine();
        sb.AppendLine("## Órás profil");
        sb.AppendLine();
        sb.AppendLine("![Órás vízhozam](hourly-water-bars.svg)");
        sb.AppendLine();
        sb.AppendLine("| Óra | T (°C) | RH (%) | GHI | Víz (kg) | Mód | Peltier (W) | Fan on |");
        sb.AppendLine("|---:|---:|---:|---:|---:|:---|---:|---:|");
        foreach (var h in report.HourlySamples)
        {
            sb.Append("| ").Append(h.HourOfDay).Append(" | ")
                .Append(h.AmbientTemperatureC.ToString("0.0", inv)).Append(" | ")
                .Append(h.RelativeHumidityPercent.ToString("0", inv)).Append(" | ")
                .Append(h.IrradianceWPerM2.ToString("0", inv)).Append(" | ")
                .Append(h.WaterProducedKg.ToString("0.####", inv)).Append(" | ")
                .Append(h.DominantMode).Append(" | ")
                .Append(h.MeanPeltierW.ToString("0", inv)).Append(" | ")
                .Append(h.MeanFanOnFraction.ToString("0.00", inv)).AppendLine(" |");
        }

        sb.AppendLine();
        sb.AppendLine("Üzemmódok / döntési táblák: [`docs/07_Applications/31_AwgSummerDiurnalOperation.md`](../../../docs/07_Applications/31_AwgSummerDiurnalOperation.md)");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("dotnet run --project src/ThermoCore.Console -- summer-diurnal");
        sb.AppendLine("```");

        File.WriteAllText(path, sb.ToString());
    }

    private static void WriteSizingTableSvg(AwgDiurnalSizingReport report, string path)
    {
        var inv = CultureInfo.InvariantCulture;
        var width = 720;
        var rowH = 36;
        var height = 90 + report.Targets.Count * rowH;
        var sb = new StringBuilder();
        sb.AppendLine($"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">""");
        sb.AppendLine("""<rect width="100%" height="100%" fill="#f8fafc"/>""");
        sb.AppendLine("""<text x="24" y="28" font-family="Segoe UI, Arial, sans-serif" font-size="16" font-weight="700" fill="#0f172a">AWG méretezés — nyári diurnal (PV / akku / Wh)</text>""");
        sb.AppendLine($"""<text x="24" y="48" font-family="Segoe UI, Arial, sans-serif" font-size="11" fill="#64748b">Baseline {report.BaselineWaterLiters.ToString("0.###", inv)} L/nap · {report.SpecificEnergyWhPerLiter.ToString("0.#", inv)} Wh/L · PSH {report.PeakSunHours.ToString("0.##", inv)} h</text>""");

        string[] headers = ["Cél L/nap", "Napi Wh", "PV W", "PV m²", "Akku Wh", "Éjjel Wh"];
        int[] xs = [32, 130, 240, 340, 440, 560];
        for (var i = 0; i < headers.Length; i++)
        {
            sb.AppendLine($"""<text x="{xs[i]}" y="72" font-family="Segoe UI, Arial, sans-serif" font-size="12" font-weight="600" fill="#334155">{headers[i]}</text>""");
        }

        for (var i = 0; i < report.Targets.Count; i++)
        {
            var t = report.Targets[i];
            var y = 96 + i * rowH;
            sb.AppendLine($"""<rect x="20" y="{y - 20}" width="{width - 40}" height="{rowH - 6}" rx="6" fill="#ffffff" stroke="#e2e8f0"/>""");
            sb.AppendLine($"""<text x="{xs[0]}" y="{y}" font-family="Segoe UI, Arial, sans-serif" font-size="13" fill="#0f172a">{t.TargetLitersPerDay.ToString("0.#", inv)}</text>""");
            sb.AppendLine($"""<text x="{xs[1]}" y="{y}" font-family="Segoe UI, Arial, sans-serif" font-size="13" fill="#0f172a">{t.DailyElectricalEnergyWh.ToString("0", inv)}</text>""");
            sb.AppendLine($"""<text x="{xs[2]}" y="{y}" font-family="Segoe UI, Arial, sans-serif" font-size="13" font-weight="600" fill="#0369a1">{t.RecommendedPvRatedPowerW.ToString("0", inv)}</text>""");
            sb.AppendLine($"""<text x="{xs[3]}" y="{y}" font-family="Segoe UI, Arial, sans-serif" font-size="13" fill="#0f172a">{t.RecommendedPvAreaM2.ToString("0.00", inv)}</text>""");
            sb.AppendLine($"""<text x="{xs[4]}" y="{y}" font-family="Segoe UI, Arial, sans-serif" font-size="13" font-weight="600" fill="#b45309">{t.RecommendedBatteryCapacityWh.ToString("0", inv)}</text>""");
            sb.AppendLine($"""<text x="{xs[5]}" y="{y}" font-family="Segoe UI, Arial, sans-serif" font-size="13" fill="#0f172a">{t.NightElectricalEnergyWh.ToString("0", inv)}</text>""");
        }

        sb.AppendLine("</svg>");
        File.WriteAllText(path, sb.ToString());
    }

    private static void WriteHourlyBarsSvg(AwgDiurnalSizingReport report, string path)
    {
        var inv = CultureInfo.InvariantCulture;
        const int width = 720;
        const int height = 280;
        const int left = 48;
        const int top = 40;
        const int plotW = 640;
        const int plotH = 180;
        if (report.HourlySamples.Count == 0)
        {
            File.WriteAllText(path, """<svg xmlns="http://www.w3.org/2000/svg" width="720" height="80"><text x="20" y="40" font-family="Segoe UI, Arial, sans-serif" font-size="14" fill="#64748b">No hourly samples</text></svg>""");
            return;
        }

        var maxW = Math.Max(1e-9, report.HourlySamples.Max(h => h.WaterProducedKg));
        var sb = new StringBuilder();
        sb.AppendLine($"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">""");
        sb.AppendLine("""<rect width="100%" height="100%" fill="#f8fafc"/>""");
        sb.AppendLine("""<text x="20" y="24" font-family="Segoe UI, Arial, sans-serif" font-size="14" font-weight="600" fill="#1f2933">Órás kondenzátum (kg) — nyári diurnal</text>""");
        var slot = plotW / 24.0;
        for (var i = 0; i < report.HourlySamples.Count; i++)
        {
            var h = report.HourlySamples[i];
            var barH = plotH * h.WaterProducedKg / maxW;
            var x = left + i * slot + 2;
            var y = top + plotH - barH;
            var night = h.IrradianceWPerM2 < 50;
            var fill = night ? "#0284c7" : "#ea580c";
            sb.AppendLine($"""<rect x="{x.ToString("0.##", inv)}" y="{y.ToString("0.##", inv)}" width="{(slot - 4).ToString("0.##", inv)}" height="{Math.Max(0.5, barH).ToString("0.##", inv)}" fill="{fill}"/>""");
            if (i % 3 == 0)
            {
                sb.AppendLine($"""<text x="{(x + slot / 2).ToString("0.##", inv)}" y="{top + plotH + 16}" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="10" fill="#64748b">{h.HourOfDay}</text>""");
            }
        }

        sb.AppendLine("""<text x="48" y="260" font-family="Segoe UI, Arial, sans-serif" font-size="11" fill="#0284c7">kék = éjszaka/alacsony GHI</text>""");
        sb.AppendLine("""<text x="220" y="260" font-family="Segoe UI, Arial, sans-serif" font-size="11" fill="#ea580c">narancs = nappali napsütés</text>""");
        sb.AppendLine("</svg>");
        File.WriteAllText(path, sb.ToString());
    }

    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
