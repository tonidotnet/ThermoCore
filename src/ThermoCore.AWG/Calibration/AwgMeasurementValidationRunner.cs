using ThermoCore.AWG.Configuration;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Calibration;

namespace ThermoCore.AWG.Calibration;

/// <summary>Runs an AWG simulation and compares selected channels to a measurement CSV.</summary>
public sealed class AwgMeasurementValidationRunner
{
    private readonly AwgSimulationRunner _runner = new();

    public AwgMeasurementValidationResult Validate(
        MeasurementDataset measurements,
        AwgSystemConfiguration configuration,
        AwgInitialState initialState,
        AwgSimulationOptions options)
    {
        ArgumentNullException.ThrowIfNull(measurements);
        var run = _runner.Run(configuration, initialState, options);
        var collected = AwgResultExporter.Collect(run);
        var report = SimulationMeasurementComparer.Compare(
            measurements,
            collected.Channels,
            options.StartTimeUtc,
            options.TimeStep);

        return new AwgMeasurementValidationResult
        {
            Run = run,
            Report = report
        };
    }

    public AwgMeasurementValidationResult ValidateFromFiles(
        string measurementCsvPath,
        string? configurationPath = null,
        double durationSeconds = 30,
        double timeStepSeconds = 1)
    {
        var measurements = MeasurementCsvImporter.ImportFromFile(measurementCsvPath);
        var document = string.IsNullOrWhiteSpace(configurationPath)
            ? AwgConfigurationLoader.CreateDefaultDocument(enableElectricalSubsystem: false)
            : AwgConfigurationLoader.LoadFromFile(configurationPath);

        var options = AwgSimulationOptions.CreateDefault(
            TimeSpan.FromSeconds(durationSeconds),
            TimeSpan.FromSeconds(timeStepSeconds));

        return Validate(measurements, document.System, document.InitialState, options);
    }
}
