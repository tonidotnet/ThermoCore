using ThermoCore.AWG.Calibration;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Calibration;

namespace ThermoCore.AWG.Optimization;

/// <summary>Uniform random search over calibratable parameter bounds (OPT follow-up).</summary>
public sealed class AwgRandomSearchRunner
{
    private readonly AwgSimulationRunner _runner = new();

    public AwgParameterSweepResult Run(
        AwgSystemConfiguration baseline,
        AwgInitialState initialState,
        AwgSimulationOptions options,
        int sampleCount,
        int? seed = null,
        IReadOnlyList<string>? parameterIds = null)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(options);
        if (sampleCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleCount));
        }

        var catalog = parameterIds is null || parameterIds.Count == 0
            ? AwgCalibratableParameterCatalog.CreateDefault(baseline)
            : AwgCalibratableParameterCatalog.Select(baseline, parameterIds);
        if (catalog.Count == 0)
        {
            throw new ArgumentException("No calibratable parameters selected.", nameof(parameterIds));
        }

        var rng = seed is { } s ? new Random(s) : Random.Shared;
        var points = new List<AwgParameterSweepPointResult>(sampleCount);
        for (var i = 0; i < sampleCount; i++)
        {
            var values = Sample(catalog, rng);
            try
            {
                var configuration = AwgCalibratableParameterCatalog.Apply(baseline, values);
                var run = _runner.Run(configuration, initialState, options);
                points.Add(AwgParameterSweepRunner.CreatePoint(values, run, options));
            }
            catch (Exception ex) when (ex is ArgumentException or AwgConfigurationException)
            {
                points.Add(AwgParameterSweepRunner.CreateFailedPoint(values, ex.Message));
            }
        }

        return new AwgParameterSweepResult { Points = points };
    }

    private static Dictionary<string, double> Sample(
        IReadOnlyList<CalibratableParameter> catalog,
        Random rng)
    {
        var values = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var parameter in catalog)
        {
            var span = parameter.UpperBound - parameter.LowerBound;
            values[parameter.Id] = parameter.LowerBound + (span * rng.NextDouble());
        }

        return values;
    }
}
