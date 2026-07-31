using ThermoCore.AWG.Control;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.Tests;

public class AwgControlIntegrationTests
{
    [Fact]
    public void ControlledRun_EntersAdsorptionThenRegeneration()
    {
        // Heat recovery + hard collector gating currently destabilizes the HR tear;
        // controlled cycling is validated on the acyclic process train.
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(
            enableElectricalSubsystem: true,
            enableHeatRecovery: false);
        configuration = (configuration with
        {
            Ambient = configuration.Ambient with
            {
                TemperatureK = UnitConversions.CelsiusToKelvin(35),
                RelativeHumidityFraction = 0.60,
                SolarIrradianceWPerSquareMeter = 950.0
            },
            SilicaGel = configuration.SilicaGel with
            {
                AmbientTemperatureK = UnitConversions.CelsiusToKelvin(35)
            }
        }).Validate();

        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration) with
        {
            SilicaGelLoadingKgPerKg = 0.02,
            SilicaGelTemperatureK = configuration.Ambient.TemperatureK,
            SolarCollectorAbsorberTemperatureK = configuration.Ambient.TemperatureK,
            BatteryStoredEnergyJ = 0.9 * configuration.Battery.NominalCapacityJ
        };

        var xEq = configuration.SilicaGel.MaximumWaterLoadingKgPerKgDryAdsorbent * 0.60;
        var options = new AwgSimulationOptions
        {
            StartTimeUtc = DateTimeOffset.Parse("2026-07-01T00:00:00Z"),
            Duration = TimeSpan.FromHours(1),
            TimeStep = TimeSpan.FromSeconds(5),
            EnableController = true,
            ControlParameters = RuleBasedAwgController.CreateDefaultParameters() with
            {
                AdsorptionTargetLoadingKgPerKg = 0.40 * xEq,
                RegenerationEntryLoadingKgPerKg = 0.40 * xEq,
                RegenerationExitLoadingKgPerKg = 0.025,
                MinimumAdsorptionDrivingForceKgPerKg = 0.004,
                MinimumModeDwell = TimeSpan.FromMinutes(2),
                CollectorAbsorberTemperatureLimitK = UnitConversions.CelsiusToKelvin(140.0)
            }
        }.Validate();

        var run = new AwgSimulationRunner().Run(configuration, initial.Validate(configuration), options);
        var errors = string.Join(
            " | ",
            run.EngineResult.Diagnostics
                .Where(d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error)
                .Select(d => $"{d.Code}:{d.Message}")
                .Take(10));
        Assert.True(run.EngineResult.Succeeded, errors);
        Assert.NotNull(run.FinalControllerState);
        Assert.Contains(run.ControllerDecisionTrace, t => t.RequestedMode == nameof(AwgOperatingMode.Adsorption));
        Assert.Contains(run.ControllerDecisionTrace, t => t.RequestedMode == nameof(AwgOperatingMode.Regeneration));
        Assert.True((run.Summary.FinalWaterTankContentKg ?? 0.0) > 0.0);
    }
}
