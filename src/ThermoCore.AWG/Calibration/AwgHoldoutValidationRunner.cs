using ThermoCore.AWG.Configuration;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Calibration;

namespace ThermoCore.AWG.Calibration;

/// <summary>
/// Fits parameters on an earlier measurement window and scores the fitted model on holdout data.
/// </summary>
public sealed class AwgHoldoutValidationRunner
{
    private readonly AwgParameterCalibrationRunner _calibrationRunner = new();
    private readonly AwgMeasurementValidationRunner _validationRunner = new();

    public AwgHoldoutValidationResult Validate(
        MeasurementDataset measurements,
        AwgSystemConfiguration configuration,
        AwgInitialState initialState,
        AwgSimulationOptions options,
        double trainFraction = 0.7,
        IReadOnlyList<CalibratableParameter>? parameters = null,
        int maximumPasses = 3,
        int maximumEvaluationsPerParameter = 12)
    {
        ArgumentNullException.ThrowIfNull(measurements);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(options);

        var split = MeasurementDatasetSplitter.SplitChronologically(measurements, trainFraction);
        var training = _calibrationRunner.Calibrate(
            split.Train,
            configuration,
            initialState,
            options,
            parameters,
            maximumPasses,
            maximumEvaluationsPerParameter);

        var holdoutBaseline = _validationRunner.Validate(
            split.Holdout,
            configuration,
            initialState,
            options);
        var holdoutFitted = _validationRunner.Validate(
            split.Holdout,
            training.FittedConfiguration,
            initialState,
            options);

        return new AwgHoldoutValidationResult
        {
            Split = split,
            Training = training,
            HoldoutBaselineReport = holdoutBaseline.Report,
            HoldoutFittedReport = holdoutFitted.Report,
            FittedConfiguration = training.FittedConfiguration
        };
    }

    public AwgHoldoutValidationResult ValidateFromFiles(
        string measurementCsvPath,
        string? configurationPath = null,
        double durationSeconds = 30,
        double timeStepSeconds = 1,
        double trainFraction = 0.7,
        IEnumerable<string>? parameterIds = null)
    {
        var measurements = MeasurementCsvImporter.ImportFromFile(measurementCsvPath);
        var document = string.IsNullOrWhiteSpace(configurationPath)
            ? AwgConfigurationLoader.CreateDefaultDocument(enableElectricalSubsystem: false)
            : AwgConfigurationLoader.LoadFromFile(configurationPath);
        var options = AwgSimulationOptions.CreateDefault(
            TimeSpan.FromSeconds(durationSeconds),
            TimeSpan.FromSeconds(timeStepSeconds));

        IReadOnlyList<CalibratableParameter>? parameters = null;
        if (parameterIds is not null)
        {
            var ids = parameterIds.ToArray();
            if (ids.Length > 0)
            {
                parameters = AwgCalibratableParameterCatalog.Select(document.System, ids);
            }
        }

        return Validate(
            measurements,
            document.System,
            document.InitialState,
            options,
            trainFraction,
            parameters);
    }
}
