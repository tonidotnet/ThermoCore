using ThermoCore.AWG.Control;
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
            TimeStep = TimeSpan.FromSeconds(scenario.TimeStepSeconds),
            EnableController = scenario.EnableController,
            ControlParameters = scenario.EnableController
                ? CreateControlParameters(scenario, configuration)
                : null
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
                AmbientTemperatureK = ambientTemperatureK,
                DryAdsorbentMassKg = scenario.SilicaGelDryAdsorbentMassKg
                    ?? configuration.SilicaGel.DryAdsorbentMassKg
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
        if (scenario.InitialSilicaGelLoadingKgPerKg is double loading)
        {
            initial = initial with { SilicaGelLoadingKgPerKg = loading };
        }

        return (configuration, initial.Validate(configuration));
    }

    /// <summary>
    /// RH-aware loading thresholds so low-humidity cases can still complete an adsorb/regen cycle
    /// (absolute 0.20 kg/kg entry is unreachable when X_eq = 0.35·RH).
    /// </summary>
    public static AwgControlParameters CreateControlParameters(
        AwgRegressionScenario scenario,
        AwgSystemConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(configuration);

        var xMax = configuration.SilicaGel.MaximumWaterLoadingKgPerKgDryAdsorbent;
        var xFloor = configuration.SilicaGel.MinimumRegeneratedLoadingKgPerKgDryAdsorbent;
        var xEqAmbient = xMax * Math.Clamp(scenario.RelativeHumidityFraction, 0.0, 1.0);
        // Bed warms during adsorption (heat of adsorption), so reachable X_eq is well below ambient Henry loading.
        var adsorbTarget = Math.Max(xFloor + 0.025, 0.40 * xEqAmbient);
        var regenExit = Math.Max(xFloor, Math.Min(adsorbTarget - 0.015, 0.025));

        var peltierW = scenario.NominalPeltierPowerRequestW
            ?? RuleBasedAwgController.CreateDefaultParameters().NominalPeltierPowerRequestW;

        return RuleBasedAwgController.CreateDefaultParameters() with
        {
            AdsorptionTargetLoadingKgPerKg = adsorbTarget,
            RegenerationEntryLoadingKgPerKg = adsorbTarget,
            RegenerationExitLoadingKgPerKg = regenExit,
            MinimumAdsorptionDrivingForceKgPerKg = 0.004,
            MinimumModeDwell = TimeSpan.FromMinutes(2),
            CollectorAbsorberTemperatureLimitK = UnitConversions.CelsiusToKelvin(140.0),
            NominalPeltierPowerRequestW = peltierW
        };
    }
}
