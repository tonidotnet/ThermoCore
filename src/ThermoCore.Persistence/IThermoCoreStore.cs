using ThermoCore.AWG.Calibration;
using ThermoCore.AWG.Configuration;
using ThermoCore.AWG.Simulation;

namespace ThermoCore.Persistence;

/// <summary>Application persistence abstraction (DOC-021).</summary>
public interface IThermoCoreStore
{
    void EnsureCreated();

    StoredConfigurationVersion SaveConfiguration(
        AwgConfigurationDocument document,
        string name,
        string schemaVersion = "awg-v3-mvp-1");

    StoredConfigurationVersion? GetConfigurationVersion(Guid id);

    StoredSimulationSummary SaveSimulationSummary(
        AwgSimulationRunResult run,
        Guid configurationVersionId);

    StoredSimulationSummary? GetSimulationSummary(Guid id);

    IReadOnlyList<StoredSimulationSummary> ListSimulationSummaries(int take = 50);

    /// <summary>
    /// Persists channel metadata plus a compressed full-series payload for a saved summary.
    /// </summary>
    StoredResultSeriesBundle SaveResultSeries(
        Guid simulationSummaryId,
        AwgSimulationRunResult run);

    StoredResultSeriesBundle? GetResultSeries(Guid simulationSummaryId, bool loadValues = true);

    StoredCalibrationRun SaveCalibrationRun(
        AwgParameterCalibrationResult calibration,
        string measurementSourcePath,
        Guid? baselineConfigurationVersionId,
        Guid? fittedConfigurationVersionId);

    IReadOnlyList<StoredCalibrationRun> ListCalibrationRuns(int take = 50);
}
