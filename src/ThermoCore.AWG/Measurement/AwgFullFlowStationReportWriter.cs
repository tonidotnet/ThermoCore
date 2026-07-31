using System.Globalization;
using System.Text;
using ThermoCore.AWG.Optimization;

namespace ThermoCore.AWG.Measurement;

/// <summary>Writes full-flow station diagrams and tables into a scenario directory.</summary>
public static class AwgFullFlowStationReportWriter
{
    public static void Write(AwgFullFlowStationReport report, string directory)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);

        WriteStationsCsv(report, Path.Combine(directory, "stations.csv"));
        WriteFlowMarkdown(report, Path.Combine(directory, "FLOW.md"));
    }

    private static void WriteStationsCsv(AwgFullFlowStationReport report, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "stationId,hungarianName,englishName,temperatureC,relativeHumidityFraction," +
            "humidityRatioKgPerKg,dryAirMassFlowKgPerSecond,waterVaporMassFlowKgPerSecond");
        foreach (var s in report.Stations)
        {
            sb.Append(s.StationId).Append(',')
                .Append(Escape(s.HungarianName)).Append(',')
                .Append(Escape(s.EnglishName)).Append(',')
                .Append(s.TemperatureC.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                .Append(s.RelativeHumidityFraction.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                .Append(s.HumidityRatioKgPerKgDryAir.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                .Append(s.DryAirMassFlowKgPerSecond.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                .Append(s.WaterVaporMassFlowKgPerSecond.ToString("G17", CultureInfo.InvariantCulture))
                .AppendLine();
        }

        File.WriteAllText(path, sb.ToString());
    }

    private static void WriteFlowMarkdown(AwgFullFlowStationReport report, string path)
    {
        var inv = CultureInfo.InvariantCulture;
        var water = report.CollectedWaterKg ?? 0.0;
        var litersPerDay = AwgOptimizationObjectives.LitersPerDay(water, report.Run.Options.Duration);
        var ambient = report.Run.BuiltSystem.Configuration.Ambient;
        var silica = report.Run.BuiltSystem.Configuration.SilicaGel.DryAdsorbentMassKg;
        var ambientC = ambient.TemperatureK - 273.15;

        var sb = new StringBuilder();
        sb.AppendLine("# Full AWG process flow — station results");
        sb.AppendLine();
        sb.AppendLine("ThermoCore AWG **V3** primary path (`15_SystemTopology.md`), with heat recovery and electrical subsystem.");
        sb.AppendLine();
        sb.AppendLine("> Note: process air order is **Peltier hot → napkollektor → szilikagél → kondenzátor → hővisszanyerő → kifújás**.");
        sb.AppendLine("> Solar radiation feeds the collector absorber (energy path), not a reordering of the air train.");
        sb.AppendLine();
        sb.AppendLine("## Boundary");
        sb.AppendLine();
        sb.AppendLine(
            $"- Dry-air mass flow: **{ambient.DryAirMassFlowKgPerSecond.ToString("G4", inv)} kg/s** (same as dry-sunny matrix)");
        sb.AppendLine(
            $"- Ambient: **{ambientC.ToString("0.#", inv)} °C**, RH **{(ambient.RelativeHumidityFraction * 100.0).ToString("0", inv)}%**, G **{ambient.SolarIrradianceWPerSquareMeter.ToString("0", inv)} W/m²**");
        sb.AppendLine($"- Silica dry mass: **{silica.ToString("0.#", inv)} kg**");
        sb.AppendLine(
            $"- Heat recovery: **{(report.HeatRecoveryEnabled ? "on" : "off")}**, electrical: **on**");
        sb.AppendLine(
            $"- Run: **{report.Run.Options.Duration.TotalSeconds.ToString("0", inv)} s**, Δt **{report.Run.Options.TimeStep.TotalSeconds.ToString("0", inv)} s**");
        sb.AppendLine(
            $"- Collected water: **{water.ToString("G6", inv)} kg** (~**{litersPerDay.ToString("G4", inv)} L/day** extrapolated)");
        sb.AppendLine();
        sb.AppendLine("## Process diagram");
        sb.AppendLine();
        sb.AppendLine("```text");
        sb.AppendLine("              NAPSUGÁRZÁS");
        sb.AppendLine("                   │");
        sb.AppendLine("         ┌─────────▼─────────┐");
        sb.AppendLine("         │    Napkollektor   │");
        sb.AppendLine("         └─────────┬─────────┘");
        sb.AppendLine("                   │  (energy into absorber)");
        sb.AppendLine("Ambient → [HR cold] → Fan → Peltier meleg → Napkollektor → Szilikagél");
        sb.AppendLine("                   → Kondenzációs kamra → Hővisszanyerő → Kifújás");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("```mermaid");
        sb.AppendLine("flowchart TD");
        sb.AppendLine("  SOL[Napsugarzas] --> COL[Napkollektor absorber]");
        sb.AppendLine("  AMB[Kornyezeti levego] --> HRc[HR hideg oldal]");
        sb.AppendLine("  HRc --> FAN[Folyamatventilator]");
        sb.AppendLine("  FAN --> HOT[Peltier meleg oldal]");
        sb.AppendLine("  HOT --> COLA[Napkollektor legoldal]");
        sb.AppendLine("  COL -.-> COLA");
        sb.AppendLine("  COLA --> SIL[Szilikagel kazetta]");
        sb.AppendLine("  SIL --> CON[Kondenzacios kamra / Peltier hideg]");
        sb.AppendLine("  CON --> HRh[Hoviszanyero forro oldal]");
        sb.AppendLine("  HRh --> EX[Kifujas]");
        sb.AppendLine("  CON -->|liquid| TANK[Viztartaly]");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Station table (T, RH, W)");
        sb.AppendLine();
        sb.AppendLine("| Id | Állomás | T (°C) | RH | W (kg/kg) | ṁ_da (kg/s) | ṁ_v (kg/s) |");
        sb.AppendLine("|---|---|---:|---:|---:|---:|---:|");
        foreach (var s in report.Stations)
        {
            sb.AppendLine(
                $"| {s.StationId} | {s.HungarianName} | {s.TemperatureC.ToString("0.00", inv)} | " +
                $"{(s.RelativeHumidityFraction * 100.0).ToString("0.0", inv)}% | " +
                $"{s.HumidityRatioKgPerKgDryAir.ToString("0.000000", inv)} | " +
                $"{s.DryAirMassFlowKgPerSecond.ToString("0.000", inv)} | " +
                $"{s.WaterVaporMassFlowKgPerSecond.ToString("0.000000", inv)} |");
        }

        sb.AppendLine();
        sb.AppendLine("Raw: [`stations.csv`](stations.csv).");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("dotnet run --project src/ThermoCore.Console -- full-flow");
        sb.AppendLine("dotnet run --project src/ThermoCore.Console -- regress --dir samples/scenarios/full-awg-flow");
        sb.AppendLine("```");

        File.WriteAllText(path, sb.ToString());
    }

    private static string Escape(string value)
        => value.Contains(',') ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
}
