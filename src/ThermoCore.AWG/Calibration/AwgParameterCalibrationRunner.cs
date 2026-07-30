using ThermoCore.AWG.Configuration;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Calibration;

namespace ThermoCore.AWG.Calibration;

/// <summary>Fits selected AWG parameters by minimizing measurement overall RMSE (CAL-006).</summary>
public sealed class AwgParameterCalibrationRunner
{
    private readonly AwgSimulationRunner _runner = new();

    public AwgParameterCalibrationResult Calibrate(
        MeasurementDataset measurements,
        AwgSystemConfiguration configuration,
        AwgInitialState initialState,
        AwgSimulationOptions options,
        IReadOnlyList<CalibratableParameter>? parameters = null,
        int maximumPasses = 3,
        int maximumEvaluationsPerParameter = 12)
    {
        ArgumentNullException.ThrowIfNull(measurements);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(options);

        var baselineConfiguration = configuration.Validate();
        var selected = parameters is { Count: > 0 }
            ? parameters
            : AwgCalibratableParameterCatalog.CreateDefault(baselineConfiguration);

        var baselineReport = Evaluate(measurements, baselineConfiguration, initialState, options);
        var fitting = BoundedCoordinateDescentFitter.Fit(new ParameterFittingRequest
        {
            Parameters = selected,
            MaximumPasses = maximumPasses,
            MaximumEvaluationsPerParameter = maximumEvaluationsPerParameter,
            Objective = candidate =>
            {
                var trial = AwgCalibratableParameterCatalog.Apply(baselineConfiguration, candidate);
                var report = Evaluate(measurements, trial, initialState, options);
                if (report.Channels.Count == 0 || double.IsNaN(report.OverallRmse))
                {
                    return double.PositiveInfinity;
                }

                return report.OverallRmse;
            }
        });

        var fittedConfiguration = AwgCalibratableParameterCatalog.Apply(
            baselineConfiguration,
            fitting.FittedValues);
        var fittedReport = Evaluate(measurements, fittedConfiguration, initialState, options);

        return new AwgParameterCalibrationResult
        {
            Fitting = fitting,
            BaselineConfiguration = baselineConfiguration,
            FittedConfiguration = fittedConfiguration,
            BaselineReport = baselineReport,
            FittedReport = fittedReport
        };
    }

    public AwgParameterCalibrationResult CalibrateFromFiles(
        string measurementCsvPath,
        string? configurationPath = null,
        double durationSeconds = 30,
        double timeStepSeconds = 1,
        IEnumerable<string>? parameterIds = null)
    {
        var measurements = MeasurementCsvImporter.ImportFromFile(measurementCsvPath);
        var document = string.IsNullOrWhiteSpace(configurationPath)
            ? AwgConfigurationLoader.CreateDefaultDocument(enableElectricalSubsystem: false)
            : AwgConfigurationLoader.LoadFromFile(configurationPath);
        var options = AwgSimulationOptions.CreateDefault(
            TimeSpan.FromSeconds(durationSeconds),
            TimeSpan.FromSeconds(timeStepSeconds));

        IReadOnlyList<CalibratableParameter>? selected = null;
        if (parameterIds is not null)
        {
            selected = AwgCalibratableParameterCatalog.Select(document.System, parameterIds);
            if (selected.Count == 0)
            {
                throw new ArgumentException("No recognized calibratable parameter ids were supplied.");
            }
        }

        return Calibrate(
            measurements,
            document.System,
            document.InitialState,
            options,
            selected);
    }

    private MeasurementComparisonReport Evaluate(
        MeasurementDataset measurements,
        AwgSystemConfiguration configuration,
        AwgInitialState initialState,
        AwgSimulationOptions options)
    {
        var run = _runner.Run(configuration, initialState, options);
        if (!run.EngineResult.Succeeded)
        {
            return new MeasurementComparisonReport
            {
                MeasurementSourcePath = measurements.SourcePath,
                Channels = Array.Empty<ChannelComparisonResult>(),
                MissingChannels = measurements.ChannelIds,
                Warnings = ["Simulation failed during calibration evaluation."]
            };
        }

        var collected = AwgResultExporter.Collect(run);
        return SimulationMeasurementComparer.Compare(
            measurements,
            collected.Channels,
            options.StartTimeUtc,
            options.TimeStep);
    }
}
