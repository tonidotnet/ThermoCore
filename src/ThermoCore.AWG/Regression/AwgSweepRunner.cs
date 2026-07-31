using ThermoCore.AWG.Optimization;

namespace ThermoCore.AWG.Regression;

/// <summary>Runs a 1-D controlled AWG parameter sweep and builds a report.</summary>
public sealed class AwgSweepRunner
{
    private readonly AwgRegressionScenarioRunner _runner = new();

    public AwgSweepReport Run(
        string title,
        string parameterName,
        string parameterUnit,
        string boundarySummary,
        string consoleCommand,
        IReadOnlyList<AwgRegressionScenario> scenarios,
        Func<AwgRegressionScenario, double> parameterSelector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterUnit);
        ArgumentException.ThrowIfNullOrWhiteSpace(boundarySummary);
        ArgumentException.ThrowIfNullOrWhiteSpace(consoleCommand);
        ArgumentNullException.ThrowIfNull(scenarios);
        ArgumentNullException.ThrowIfNull(parameterSelector);

        var points = new List<AwgSweepPointResult>(scenarios.Count);
        foreach (var scenario in scenarios)
        {
            var result = _runner.Run(scenario);
            var water = result.Run.Summary.FinalWaterTankContentKg ?? 0.0;
            points.Add(new AwgSweepPointResult
            {
                ScenarioId = scenario.Id,
                ParameterName = parameterName,
                ParameterValue = parameterSelector(scenario),
                ParameterUnit = parameterUnit,
                Passed = result.Passed,
                CollectedWaterKg = water,
                LitersPerDay = AwgOptimizationObjectives.LitersPerDay(water, result.Run.Options.Duration),
                FinalBusPowerW = result.Run.Summary.FinalBusPowerW,
                FinalBatterySocFraction = result.Run.Summary.FinalBatteryStateOfChargeFraction,
                FailureMessage = result.Passed ? null : string.Join("; ", result.Failures)
            });
        }

        return new AwgSweepReport
        {
            Title = title,
            ParameterName = parameterName,
            ParameterUnit = parameterUnit,
            BoundarySummary = boundarySummary,
            ConsoleCommand = consoleCommand,
            Points = points.OrderBy(p => p.ParameterValue).ToArray()
        };
    }
}
