using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.Regression;

/// <summary>Builds configuration/state and executes DOC-022 regression scenarios.</summary>
public sealed class AwgRegressionScenarioRunner
{
    private readonly AwgSimulationRunner _simulationRunner;

    public AwgRegressionScenarioRunner(AwgSimulationRunner? simulationRunner = null)
    {
        _simulationRunner = simulationRunner ?? new AwgSimulationRunner();
    }

    public AwgRegressionScenarioResult Run(AwgRegressionScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var (configuration, initial) = Build(scenario);
        var options = new AwgSimulationOptions
        {
            StartTimeUtc = DateTimeOffset.Parse("2026-07-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(scenario.DurationSeconds),
            TimeStep = TimeSpan.FromSeconds(scenario.TimeStepSeconds)
        }.Validate();

        var run = _simulationRunner.Run(configuration, initial, options);
        var failures = new List<string>();

        if (scenario.RequireSuccess && !run.EngineResult.Succeeded)
        {
            failures.Add(
                "Simulation did not succeed: " +
                string.Join(
                    "; ",
                    run.EngineResult.Diagnostics
                        .Where(d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error)
                        .Select(d => $"{d.Code}:{d.Message}")
                        .Take(5)));
        }

        if (scenario.RequireBalancePass && !run.BalanceReport.AllPassed)
        {
            failures.Add(
                $"Balance check failed (water={run.BalanceReport.WaterBalancePassed}, " +
                $"energy={run.BalanceReport.EnergyBalancePassed}, " +
                $"dryAir={run.BalanceReport.DryAirBalancePassed}).");
        }

        return new AwgRegressionScenarioResult
        {
            Scenario = scenario,
            Run = run,
            Passed = failures.Count == 0,
            Failures = failures
        };
    }

    public IReadOnlyList<AwgRegressionScenarioResult> RunAll(IEnumerable<AwgRegressionScenario> scenarios)
        => scenarios.Select(Run).ToArray();

    public static (AwgSystemConfiguration Configuration, AwgInitialState InitialState) Build(
        AwgRegressionScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (scenario.EnableHeatRecovery && scenario.EnableRecirculation)
        {
            throw new ArgumentException(
                "Scenario cannot enable both heat recovery and recirculation in the MVP.",
                nameof(scenario));
        }

        if (scenario.EnablePvRearAirChannel && !scenario.EnableElectricalSubsystem)
        {
            throw new ArgumentException(
                "PV rear-air channel requires the electrical subsystem.",
                nameof(scenario));
        }

        var configuration = AwgSystemDefaults.CreateMvpConfiguration(
            enableElectricalSubsystem: scenario.EnableElectricalSubsystem,
            enableRecirculation: scenario.EnableRecirculation,
            enableHeatRecovery: scenario.EnableHeatRecovery,
            enablePvRearAirChannel: scenario.EnablePvRearAirChannel);

        var ambientTemperatureK = UnitConversions.CelsiusToKelvin(scenario.AmbientTemperatureC);
        configuration = (configuration with
        {
            Ambient = configuration.Ambient with
            {
                TemperatureK = ambientTemperatureK,
                RelativeHumidityFraction = scenario.RelativeHumidityFraction,
                SolarIrradianceWPerSquareMeter = scenario.SolarIrradianceWPerSquareMeter
            },
            SilicaGel = configuration.SilicaGel with
            {
                AmbientTemperatureK = ambientTemperatureK
            },
            WaterTank = configuration.WaterTank with
            {
                CapacityKg = scenario.WaterTankCapacityKg ?? configuration.WaterTank.CapacityKg,
                InitialTemperatureK = ambientTemperatureK
            }
        }).Validate();

        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration) with
        {
            SilicaGelTemperatureK = ambientTemperatureK,
            SolarCollectorAbsorberTemperatureK = ambientTemperatureK,
            WaterTankContentKg = scenario.InitialWaterTankContentKg,
            BatteryStoredEnergyJ = Math.Clamp(scenario.InitialBatterySocFraction, 0.0, 1.0)
                * configuration.Battery.NominalCapacityJ
        };

        return (configuration, initial.Validate(configuration));
    }
}
