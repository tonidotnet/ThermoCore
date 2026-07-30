using ThermoCore.AWG.Calibration;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;

namespace ThermoCore.AWG.Optimization;

/// <summary>Runs a Cartesian grid sweep over calibratable AWG parameters (OPT-002).</summary>
public sealed class AwgParameterSweepRunner
{
    private readonly AwgSimulationRunner _runner = new();

    public AwgParameterSweepResult Run(
        AwgSystemConfiguration baseline,
        AwgInitialState initialState,
        AwgSimulationOptions options,
        IReadOnlyList<AwgParameterSweepAxis> axes)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(axes);
        if (axes.Count == 0)
        {
            throw new ArgumentException("At least one sweep axis is required.", nameof(axes));
        }

        if (axes.Count > 3)
        {
            throw new ArgumentException("MVP sweep supports at most three axes.", nameof(axes));
        }

        foreach (var axis in axes)
        {
            axis.Validate();
        }

        var combinations = BuildCombinations(axes);
        var points = new List<AwgParameterSweepPointResult>(combinations.Count);
        foreach (var combination in combinations)
        {
            try
            {
                var configuration = AwgCalibratableParameterCatalog.Apply(baseline, combination);
                var run = _runner.Run(configuration, initialState, options);
                var waterKg = run.Summary.FinalWaterTankContentKg ?? 0.0;
                points.Add(new AwgParameterSweepPointResult
                {
                    ParameterValues = combination,
                    Succeeded = run.EngineResult.Succeeded,
                    CollectedWaterKg = waterKg,
                    LitersPerDay = AwgOptimizationObjectives.LitersPerDay(waterKg, options.Duration),
                    WattHoursPerLiter = AwgOptimizationObjectives.WattHoursPerLiter(run.Summary),
                    AggregatedEnergyResidualJ = run.Summary.AggregatedEnergyResidualJ,
                    AggregatedWaterResidualKg = run.Summary.AggregatedWaterResidualKg,
                    FailureMessage = run.EngineResult.Succeeded
                        ? null
                        : string.Join("; ", run.EngineResult.Diagnostics.Select(d => d.Code))
                });
            }
            catch (Exception ex) when (ex is ArgumentException or AwgConfigurationException)
            {
                points.Add(new AwgParameterSweepPointResult
                {
                    ParameterValues = combination,
                    Succeeded = false,
                    CollectedWaterKg = 0,
                    LitersPerDay = 0,
                    WattHoursPerLiter = null,
                    AggregatedEnergyResidualJ = double.NaN,
                    AggregatedWaterResidualKg = double.NaN,
                    FailureMessage = ex.Message
                });
            }
        }

        return new AwgParameterSweepResult { Points = points };
    }

    private static List<Dictionary<string, double>> BuildCombinations(IReadOnlyList<AwgParameterSweepAxis> axes)
    {
        var combinations = new List<Dictionary<string, double>> { new(StringComparer.Ordinal) };
        foreach (var axis in axes)
        {
            var next = new List<Dictionary<string, double>>();
            foreach (var existing in combinations)
            {
                foreach (var value in axis.Values)
                {
                    var copy = new Dictionary<string, double>(existing, StringComparer.Ordinal)
                    {
                        [axis.ParameterId] = value
                    };
                    next.Add(copy);
                }
            }

            combinations = next;
        }

        return combinations;
    }
}
