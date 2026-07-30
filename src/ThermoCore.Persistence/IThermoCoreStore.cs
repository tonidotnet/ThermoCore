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

    StoredCalibrationRun SaveCalibrationRun(
        AwgParameterCalibrationResult calibration,
        string measurementSourcePath,
        Guid? baselineConfigurationVersionId,
        Guid? fittedConfigurationVersionId);

    IReadOnlyList<StoredCalibrationRun> ListCalibrationRuns(int take = 50);
}
