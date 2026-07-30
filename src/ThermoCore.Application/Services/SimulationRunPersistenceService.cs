using ThermoCore.Persistence;

namespace ThermoCore.Api.Services;

/// <summary>Persists completed job configuration, summary, and series when a store is registered.</summary>
public sealed class SimulationRunPersistenceService
{
    private readonly IThermoCoreStore? _store;

    public SimulationRunPersistenceService(IThermoCoreStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>Creates a no-op persistence service (store disabled).</summary>
    public static SimulationRunPersistenceService Disabled { get; } = new();

    private SimulationRunPersistenceService()
    {
        _store = null;
    }

    public bool IsEnabled => _store is not null;

    public Guid? TryPersistCompletedJob(SimulationJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (_store is null || job.RunResult is null)
        {
            return null;
        }

        if (job.Status is not (SimulationJobStatus.Completed or SimulationJobStatus.Failed))
        {
            return null;
        }

        var version = _store.SaveConfiguration(job.Configuration, job.SimulationId);
        var summary = _store.SaveSimulationSummary(job.RunResult, version.Id);
        try
        {
            _ = _store.SaveResultSeries(summary.Id, job.RunResult);
        }
        catch
        {
            // Series persistence is best-effort; summary remains the primary record.
        }

        job.PersistedSummaryId = summary.Id;
        return summary.Id;
    }
}
