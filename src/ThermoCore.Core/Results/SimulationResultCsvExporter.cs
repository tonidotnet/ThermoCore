using System.Globalization;
using System.Text;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.Core.Results;

/// <summary>
/// Writes DOC-029 CSV exports (wide/long series, summary, diagnostics, balances).
/// </summary>
public static class SimulationResultCsvExporter
{
    public static void ExportDirectory(
        SimulationResult result,
        string directory,
        SimulationRunResult? run = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);

        File.WriteAllText(
            Path.Combine(directory, "summary.csv"),
            WriteSummary(result),
            Encoding.UTF8);
        File.WriteAllText(
            Path.Combine(directory, "series-wide.csv"),
            WriteSeriesWide(result),
            Encoding.UTF8);
        File.WriteAllText(
            Path.Combine(directory, "series-long.csv"),
            WriteSeriesLong(result),
            Encoding.UTF8);

        if (run is not null)
        {
            File.WriteAllText(
                Path.Combine(directory, "diagnostics.csv"),
                WriteDiagnostics(result.Metadata.StartTimeUtc, run),
                Encoding.UTF8);
            File.WriteAllText(
                Path.Combine(directory, "balances.csv"),
                WriteBalances(result.Metadata.StartTimeUtc, run),
                Encoding.UTF8);
        }
    }

    public static string WriteSummary(SimulationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var sb = new StringBuilder();
        sb.AppendLine("metric_id,value,unit");
        AppendMetric(sb, "status", result.Status.ToString(), "enum");
        AppendMetric(sb, "succeeded", result.Summary.Succeeded ? "1" : "0", "bool");
        AppendMetric(sb, "balance.energy.maximumAbsoluteResidualJ", result.Summary.MaxAbsEnergyResidualJ, "J");
        AppendMetric(sb, "balance.water.maximumAbsoluteResidualKg", result.Summary.MaxAbsWaterResidualKg, "kg");
        AppendMetric(sb, "balance.dryAir.maximumAbsoluteResidualKg", result.Summary.MaxAbsDryAirResidualKg, "kg");
        AppendMetric(sb, "balance.energy.aggregatedResidualJ", result.Summary.AggregatedEnergyResidualJ, "J");
        AppendMetric(sb, "balance.water.aggregatedResidualKg", result.Summary.AggregatedWaterResidualKg, "kg");
        AppendMetric(sb, "diagnostics.warningCount", result.Summary.WarningCount, "count");
        AppendMetric(sb, "diagnostics.errorCount", result.Summary.ErrorCount, "count");
        foreach (var pair in result.Summary.ScalarMetrics.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            AppendMetric(sb, pair.Key, pair.Value, "scalar");
        }

        return sb.ToString();
    }

    public static string WriteSeriesWide(SimulationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var sb = new StringBuilder();
        var channels = result.Channels;
        sb.Append("timestamp_utc");
        foreach (var channel in channels)
        {
            sb.Append(',').Append(SanitizeHeader(channel.Definition.Id));
        }

        sb.AppendLine();

        var rowCount = channels.Count == 0 ? 0 : channels.Max(c => c.Values.Count);
        for (var i = 0; i < rowCount; i++)
        {
            var timestamp = result.Metadata.StartTimeUtc + TimeSpan.FromTicks(result.Metadata.TimeStep.Ticks * i);
            sb.Append(timestamp.UtcDateTime.ToString("o", CultureInfo.InvariantCulture));
            foreach (var channel in channels)
            {
                sb.Append(',');
                if (i < channel.Values.Count && double.IsFinite(channel.Values[i]))
                {
                    sb.Append(Format(channel.Values[i]));
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    public static string WriteSeriesLong(SimulationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var sb = new StringBuilder();
        sb.AppendLine("timestamp_utc,channel_id,value,unit");
        foreach (var channel in result.Channels)
        {
            for (var i = 0; i < channel.Values.Count; i++)
            {
                var value = channel.Values[i];
                if (!double.IsFinite(value))
                {
                    continue;
                }

                var timestamp = result.Metadata.StartTimeUtc + TimeSpan.FromTicks(result.Metadata.TimeStep.Ticks * i);
                sb.Append(timestamp.UtcDateTime.ToString("o", CultureInfo.InvariantCulture)).Append(',')
                    .Append(Escape(channel.Definition.Id)).Append(',')
                    .Append(Format(value)).Append(',')
                    .Append(Escape(channel.Definition.Unit))
                    .AppendLine();
            }
        }

        return sb.ToString();
    }

    public static string WriteDiagnostics(DateTimeOffset startTimeUtc, SimulationRunResult run)
    {
        ArgumentNullException.ThrowIfNull(run);
        var sb = new StringBuilder();
        sb.AppendLine("step_index,simulation_time_utc,severity,code,component_id,port_id,message,numeric_context_json");
        foreach (var diagnostic in run.Diagnostics)
        {
            var stepIndex = diagnostic.StepIndex ?? -1;
            var time = diagnostic.SimulationTime is { } elapsed
                ? startTimeUtc + elapsed
                : startTimeUtc;
            sb.Append(stepIndex.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(time.UtcDateTime.ToString("o", CultureInfo.InvariantCulture)).Append(',')
                .Append(diagnostic.Severity).Append(',')
                .Append(Escape(diagnostic.Code)).Append(',')
                .Append(Escape(diagnostic.ComponentId ?? string.Empty)).Append(',')
                .Append(Escape(diagnostic.PortId ?? string.Empty)).Append(',')
                .Append(Escape(diagnostic.Message)).Append(',')
                .Append(Escape(FormatNumericContext(diagnostic)))
                .AppendLine();
        }

        return sb.ToString();
    }

    public static string WriteBalances(DateTimeOffset startTimeUtc, SimulationRunResult run)
    {
        ArgumentNullException.ThrowIfNull(run);
        var sb = new StringBuilder();
        sb.AppendLine(
            "step_index,simulation_time_utc,balance_type,input,output,storage_change,residual,absolute_tolerance,relative_tolerance,status");
        foreach (var step in run.Steps)
        {
            var time = startTimeUtc + step.ElapsedTime;
            AppendBalanceRow(sb, step.StepIndex, time, "energy",
                step.SystemBalance.EnergyInputJ,
                step.SystemBalance.EnergyOutputJ,
                step.SystemBalance.StoredEnergyChangeJ,
                step.SystemBalance.EnergyResidualJ);
            AppendBalanceRow(sb, step.StepIndex, time, "water",
                step.SystemBalance.WaterMassInputKg,
                step.SystemBalance.WaterMassOutputKg,
                step.SystemBalance.WaterMassStorageChangeKg,
                step.SystemBalance.WaterMassResidualKg);
            AppendBalanceRow(sb, step.StepIndex, time, "dry_air",
                step.SystemBalance.DryAirMassInputKg,
                step.SystemBalance.DryAirMassOutputKg,
                step.SystemBalance.DryAirMassStorageChangeKg,
                step.SystemBalance.DryAirMassResidualKg);
            AppendBalanceRow(sb, step.StepIndex, time, "electrical",
                step.SystemBalance.ElectricalEnergyInputJ,
                step.SystemBalance.ElectricalEnergyOutputJ,
                step.SystemBalance.StoredElectricalEnergyChangeJ,
                step.SystemBalance.ElectricalEnergyResidualJ);
        }

        return sb.ToString();
    }

    private static void AppendBalanceRow(
        StringBuilder sb,
        int stepIndex,
        DateTimeOffset time,
        string balanceType,
        double input,
        double output,
        double storageChange,
        double residual)
    {
        sb.Append(stepIndex.ToString(CultureInfo.InvariantCulture)).Append(',')
            .Append(time.UtcDateTime.ToString("o", CultureInfo.InvariantCulture)).Append(',')
            .Append(balanceType).Append(',')
            .Append(Format(input)).Append(',')
            .Append(Format(output)).Append(',')
            .Append(Format(storageChange)).Append(',')
            .Append(Format(residual)).Append(',')
            .Append(',')
            .Append(',')
            .Append(double.IsFinite(residual) ? "ok" : "invalid")
            .AppendLine();
    }

    private static void AppendMetric(StringBuilder sb, string id, double value, string unit)
        => AppendMetric(sb, id, Format(value), unit);

    private static void AppendMetric(StringBuilder sb, string id, string value, string unit)
        => sb.Append(Escape(id)).Append(',').Append(Escape(value)).Append(',').Append(Escape(unit)).AppendLine();

    private static string FormatNumericContext(SimulationDiagnostic diagnostic)
    {
        if (diagnostic.Values is null || diagnostic.Values.Count == 0)
        {
            return string.Empty;
        }

        var parts = diagnostic.Values
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => $"\"{p.Key}\":{Format(p.Value)}");
        return "{" + string.Join(',', parts) + "}";
    }

    private static string SanitizeHeader(string id)
        => id.Replace(".", "_", StringComparison.Ordinal);

    private static string Format(double value)
        => value.ToString("G17", CultureInfo.InvariantCulture);

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return value;
    }
}
