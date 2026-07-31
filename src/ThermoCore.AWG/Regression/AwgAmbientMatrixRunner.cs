using ThermoCore.AWG.Optimization;

namespace ThermoCore.AWG.Regression;

/// <summary>Runs full-AWG ambient temperature × humidity matrix scenarios.</summary>
public sealed class AwgAmbientMatrixRunner
{
    private readonly AwgRegressionScenarioRunner _runner = new();

    public AwgAmbientMatrixReport Run(IReadOnlyList<AwgRegressionScenario>? scenarios = null)
    {
        scenarios ??= AwgRegressionScenarioCatalog.CreateFullAwgFlowAmbientMatrixScenarios();
        var points = new List<AwgAmbientMatrixPointResult>(scenarios.Count);
        foreach (var scenario in scenarios)
        {
            var result = _runner.Run(scenario);
            var water = result.Run.Summary.FinalWaterTankContentKg ?? 0.0;
            points.Add(new AwgAmbientMatrixPointResult
            {
                ScenarioId = scenario.Id,
                AmbientTemperatureC = scenario.AmbientTemperatureC,
                RelativeHumidityPercent = scenario.RelativeHumidityFraction * 100.0,
                Passed = result.Passed,
                CollectedWaterKg = water,
                LitersPerDay = AwgOptimizationObjectives.LitersPerDay(water, result.Run.Options.Duration),
                FinalBusPowerW = result.Run.Summary.FinalBusPowerW,
                FinalBatterySocFraction = result.Run.Summary.FinalBatteryStateOfChargeFraction,
                FailureMessage = result.Passed ? null : string.Join("; ", result.Failures)
            });
        }

        return new AwgAmbientMatrixReport
        {
            Points = points.OrderBy(p => p.AmbientTemperatureC).ThenBy(p => p.RelativeHumidityPercent).ToArray()
        };
    }
}
