using ThermoCore.AWG.Calibration;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Calibration;

namespace ThermoCore.AWG.Optimization;

/// <summary>One-at-a-time local sensitivity analysis over calibratable parameters (OPT-003).</summary>
public sealed class AwgSensitivityAnalysisRunner
{
    private readonly AwgSimulationRunner _runner = new();

    public AwgSensitivityAnalysisResult Run(
        AwgSystemConfiguration baseline,
        AwgInitialState initialState,
        AwgSimulationOptions simulationOptions,
        AwgSensitivityAnalysisOptions? analysisOptions = null,
        IReadOnlyList<string>? parameterIds = null)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(simulationOptions);
        analysisOptions = (analysisOptions ?? new AwgSensitivityAnalysisOptions()).Validate();

        var catalog = parameterIds is null || parameterIds.Count == 0
            ? AwgCalibratableParameterCatalog.CreateDefault(baseline)
            : AwgCalibratableParameterCatalog.Select(baseline, parameterIds);
        if (catalog.Count == 0)
        {
            throw new ArgumentException("No calibratable parameters selected.", nameof(parameterIds));
        }

        var baselineRun = Evaluate(baseline, initialState, simulationOptions);
        var results = new List<AwgSensitivityParameterResult>(catalog.Count);
        foreach (var parameter in catalog)
        {
            results.Add(EvaluateParameter(
                baseline,
                initialState,
                simulationOptions,
                analysisOptions,
                parameter,
                baselineRun));
        }

        return new AwgSensitivityAnalysisResult
        {
            BaselineLitersPerDay = baselineRun.LitersPerDay,
            BaselineCollectedWaterKg = baselineRun.CollectedWaterKg,
            BaselineSucceeded = baselineRun.Succeeded,
            BaselineFailureMessage = baselineRun.FailureMessage,
            Parameters = results
        };
    }

    private AwgSensitivityParameterResult EvaluateParameter(
        AwgSystemConfiguration baseline,
        AwgInitialState initialState,
        AwgSimulationOptions simulationOptions,
        AwgSensitivityAnalysisOptions analysisOptions,
        CalibratableParameter parameter,
        EvaluationPoint baselineRun)
    {
        var x0 = parameter.InitialValue;
        var halfSpan = Math.Abs(x0) * analysisOptions.RelativePerturbationFraction;
        if (halfSpan <= 0.0)
        {
            halfSpan = analysisOptions.RelativePerturbationFraction
                * Math.Max(parameter.UpperBound - parameter.LowerBound, 1e-12);
        }

        var low = Math.Clamp(x0 - halfSpan, parameter.LowerBound, parameter.UpperBound);
        var high = Math.Clamp(x0 + halfSpan, parameter.LowerBound, parameter.UpperBound);
        if (Math.Abs(high - low) < 1e-15)
        {
            return new AwgSensitivityParameterResult
            {
                ParameterId = parameter.Id,
                BaselineValue = x0,
                LowValue = low,
                HighValue = high,
                BaselineLitersPerDay = baselineRun.LitersPerDay,
                LowLitersPerDay = baselineRun.LitersPerDay,
                HighLitersPerDay = baselineRun.LitersPerDay,
                Succeeded = false,
                FailureMessage = "Perturbation collapsed to a single point within bounds."
            };
        }

        try
        {
            var lowRun = Evaluate(
                AwgCalibratableParameterCatalog.Apply(
                    baseline,
                    new Dictionary<string, double>(StringComparer.Ordinal) { [parameter.Id] = low }),
                initialState,
                simulationOptions);
            var highRun = Evaluate(
                AwgCalibratableParameterCatalog.Apply(
                    baseline,
                    new Dictionary<string, double>(StringComparer.Ordinal) { [parameter.Id] = high }),
                initialState,
                simulationOptions);

            var succeeded = baselineRun.Succeeded && lowRun.Succeeded && highRun.Succeeded;
            double? elasticity = null;
            double? derivative = null;
            if (succeeded)
            {
                var dx = high - low;
                var dy = highRun.LitersPerDay - lowRun.LitersPerDay;
                derivative = dy / dx;
                if (Math.Abs(x0) > 1e-15)
                {
                    var relativeX = dx / x0;
                    elasticity = Math.Abs(baselineRun.LitersPerDay) > 1e-15
                        ? (dy / baselineRun.LitersPerDay) / relativeX
                        : dy / relativeX;
                }
            }

            return new AwgSensitivityParameterResult
            {
                ParameterId = parameter.Id,
                BaselineValue = x0,
                LowValue = low,
                HighValue = high,
                BaselineLitersPerDay = baselineRun.LitersPerDay,
                LowLitersPerDay = lowRun.LitersPerDay,
                HighLitersPerDay = highRun.LitersPerDay,
                Succeeded = succeeded,
                FailureMessage = succeeded
                    ? null
                    : JoinFailures(baselineRun.FailureMessage, lowRun.FailureMessage, highRun.FailureMessage),
                LitersPerDayElasticity = elasticity,
                LitersPerDayDerivative = derivative
            };
        }
        catch (Exception ex) when (ex is ArgumentException or AwgConfigurationException)
        {
            return new AwgSensitivityParameterResult
            {
                ParameterId = parameter.Id,
                BaselineValue = x0,
                LowValue = low,
                HighValue = high,
                BaselineLitersPerDay = baselineRun.LitersPerDay,
                LowLitersPerDay = double.NaN,
                HighLitersPerDay = double.NaN,
                Succeeded = false,
                FailureMessage = ex.Message
            };
        }
    }

    private EvaluationPoint Evaluate(
        AwgSystemConfiguration configuration,
        AwgInitialState initialState,
        AwgSimulationOptions options)
    {
        var run = _runner.Run(configuration, initialState, options);
        var waterKg = run.Summary.FinalWaterTankContentKg ?? 0.0;
        return new EvaluationPoint
        {
            Succeeded = run.EngineResult.Succeeded,
            CollectedWaterKg = waterKg,
            LitersPerDay = AwgOptimizationObjectives.LitersPerDay(waterKg, options.Duration),
            FailureMessage = run.EngineResult.Succeeded
                ? null
                : string.Join("; ", run.EngineResult.Diagnostics.Select(d => d.Code))
        };
    }

    private static string JoinFailures(params string?[] messages) =>
        string.Join("; ", messages.Where(m => !string.IsNullOrWhiteSpace(m)));

    private sealed class EvaluationPoint
    {
        public required bool Succeeded { get; init; }
        public required double CollectedWaterKg { get; init; }
        public required double LitersPerDay { get; init; }
        public string? FailureMessage { get; init; }
    }
}
