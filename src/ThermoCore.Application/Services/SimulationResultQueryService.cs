using ThermoCore.Api.Contracts;
using ThermoCore.AWG.Simulation;
using ThermoCore.Core.Results;

namespace ThermoCore.Api.Services;

/// <summary>Builds series/diagnostics/export payloads from completed jobs.</summary>
public sealed class SimulationResultQueryService
{
    private readonly ISimulationJobStore _store;
    private readonly ApiResourceLimits _limits;

    public SimulationResultQueryService(ISimulationJobStore store, ApiResourceLimits limits)
    {
        _store = store;
        _limits = limits;
    }

    public CompletedJobLookupResult TryGetCompletedJob(string simulationId)
    {
        var job = _store.Get(simulationId);
        if (job is null)
        {
            return CompletedJobLookupResult.NotFound();
        }

        if (job.Status is not (SimulationJobStatus.Completed or SimulationJobStatus.Failed)
            || job.RunResult is null)
        {
            return CompletedJobLookupResult.Conflict(
                $"Results are available only after completion. Current status: {job.Status}.");
        }

        return CompletedJobLookupResult.Ok(job);
    }

    public SimulationSeriesResponse BuildSeries(
        SimulationJob job,
        string? channels,
        int page,
        int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, _limits.MaximumResultChannels);
        var collected = AwgResultExporter.Collect(job.RunResult!);
        IEnumerable<ResultTimeSeriesChannel> query = collected.Channels;

        if (!string.IsNullOrWhiteSpace(channels))
        {
            var wanted = channels
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.Ordinal);
            query = query.Where(c => wanted.Contains(c.Definition.Id));
        }

        var ordered = query.OrderBy(c => c.Definition.Id, StringComparer.Ordinal).ToArray();
        var total = ordered.Length;
        var pageItems = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new SimulationSeriesChannelDto
            {
                Id = c.Definition.Id,
                DisplayName = c.Definition.DisplayName,
                Unit = c.Definition.Unit,
                ComponentId = c.Definition.ComponentId,
                Values = c.Values
            })
            .ToArray();

        return new SimulationSeriesResponse
        {
            SimulationId = job.SimulationId,
            StartTimeUtc = job.Options.StartTimeUtc,
            IntervalSeconds = job.Options.TimeStep.TotalSeconds,
            Page = page,
            PageSize = pageSize,
            TotalChannels = total,
            Channels = pageItems
        };
    }

    public SimulationDiagnosticsResponse BuildDiagnostics(
        SimulationJob job,
        string? severity,
        string? componentId,
        string? code,
        int? fromStep,
        int? toStep)
    {
        IEnumerable<ThermoCore.Core.Diagnostics.SimulationDiagnostic> query = job.RunResult!.EngineResult.Diagnostics;

        if (!string.IsNullOrWhiteSpace(severity)
            && Enum.TryParse<ThermoCore.Core.Diagnostics.DiagnosticSeverity>(severity, ignoreCase: true, out var sev))
        {
            query = query.Where(d => d.Severity == sev);
        }

        if (!string.IsNullOrWhiteSpace(componentId))
        {
            query = query.Where(d => string.Equals(d.ComponentId, componentId, StringComparison.Ordinal));
        }

        if (!string.IsNullOrWhiteSpace(code))
        {
            query = query.Where(d => string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase));
        }

        if (fromStep is { } from)
        {
            query = query.Where(d => d.StepIndex is null || d.StepIndex >= from);
        }

        if (toStep is { } to)
        {
            query = query.Where(d => d.StepIndex is null || d.StepIndex <= to);
        }

        var filtered = query.ToArray();
        var items = filtered
            .Take(_limits.MaximumDiagnosticsReturned)
            .Select(d => new SimulationDiagnosticDto
            {
                Code = d.Code,
                Severity = d.Severity.ToString(),
                Message = d.Message,
                ComponentId = d.ComponentId,
                PortId = d.PortId,
                StepIndex = d.StepIndex
            })
            .ToArray();

        return new SimulationDiagnosticsResponse
        {
            SimulationId = job.SimulationId,
            TotalCount = filtered.Length,
            Diagnostics = items
        };
    }

    public (byte[] Content, string ContentType, string FileName) BuildExport(SimulationJob job, string format)
    {
        format = string.IsNullOrWhiteSpace(format) ? "json" : format.Trim().ToLowerInvariant();
        var collected = AwgResultExporter.Collect(job.RunResult!);

        switch (format)
        {
            case "json":
            {
                var json = System.Text.Json.JsonSerializer.Serialize(
                    collected,
                    SimulationResultBundleExporter.CreateJsonOptions());
                return (System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"{job.SimulationId}-result.json");
            }
            case "csv":
            {
                var csv = SimulationResultCsvExporter.WriteSeriesWide(collected);
                return (System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"{job.SimulationId}-series-wide.csv");
            }
            case "zip":
            {
                var directory = Path.Combine(Path.GetTempPath(), "thermocore-export-" + Guid.NewGuid().ToString("N"));
                try
                {
                    AwgResultExporter.ExportBundle(job.RunResult!, directory, job.SimulationId);
                    var zipPath = directory + ".zip";
                    if (File.Exists(zipPath))
                    {
                        File.Delete(zipPath);
                    }

                    System.IO.Compression.ZipFile.CreateFromDirectory(directory, zipPath);
                    var bytes = File.ReadAllBytes(zipPath);
                    File.Delete(zipPath);
                    return (bytes, "application/zip", $"{job.SimulationId}-export.zip");
                }
                finally
                {
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
            default:
                throw new ArgumentException("Supported export formats: csv, json, zip.", nameof(format));
        }
    }
}
