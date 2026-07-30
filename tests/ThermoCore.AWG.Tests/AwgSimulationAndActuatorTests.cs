using ThermoCore.AWG.Control;
using ThermoCore.AWG.Measurement;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.Tests;

public class AwgSimulationAndActuatorTests
{
    [Fact]
    public void SimulationRunner_RunsDefaultConfigAndBuildsSummary()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration();
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
        var options = AwgSimulationOptions.CreateDefault(
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(1));

        var run = new AwgSimulationRunner().Run(configuration, initial, options);

        Assert.True(run.EngineResult.Succeeded, string.Join("; ", run.EngineResult.Diagnostics.Select(d => d.Message)));
        Assert.True(run.Summary.Succeeded);
        Assert.Equal(5, run.Summary.CompletedSteps);
        Assert.Equal(AwgV3TopologyIds.TopologyId, run.Summary.TopologyId);
        Assert.Contains("MP-08", run.Summary.FinalMoistAirTemperaturesC.Keys);
        Assert.False(string.IsNullOrWhiteSpace(AwgRunSummaryFormatter.Format(run.Summary)));
    }

    [Fact]
    public void MeasurementSampler_ReturnsCoreV3Points()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false);
        var built = new AwgV3SystemGraphBuilder().Build(
            configuration,
            AwgSystemDefaults.CreateMvpInitialState(configuration));
        var run = new AwgSimulationRunner().Run(
            configuration,
            AwgSystemDefaults.CreateMvpInitialState(configuration),
            AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));

        var samples = AwgMeasurementSampler.SampleMoistAir(built, run.EngineResult.Steps[^1]);
        Assert.Contains(samples, s => s.PointId == "MP-01");
        Assert.Contains(samples, s => s.PointId == "MP-05");
        Assert.Contains(samples, s => s.PointId == "MP-08");
        Assert.Contains(samples, s => s.PointId == "MP-10");
        Assert.DoesNotContain(samples, s => s.PointId == "MP-02"); // mixer optional / absent
    }

    [Fact]
    public void FanController_ForcesFullWhenBelowMinimumSafeFlow()
    {
        var parameters = RuleBasedAwgController.CreateDefaultParameters();
        var request = new AwgControlRequest
        {
            RequestedMode = AwgOperatingMode.Adsorption,
            FanControlFraction = 0.2,
            PeltierPowerRequestW = 0,
            RecirculationFraction = 0,
            HeatRecoveryBypassOpen = false,
            AdsorptionBedEnabled = true,
            RegenerationHeatEnabled = false,
            CondenserEnabled = false,
            ReasonCode = "TEST"
        };
        var observation = Observation(airflow: 0.001);

        var fraction = AwgFanController.ResolveControlFraction(request, observation, parameters);
        Assert.Equal(1.0, fraction);
    }

    [Fact]
    public void PeltierController_ZerosWhenHotSideAtLimit()
    {
        var parameters = RuleBasedAwgController.CreateDefaultParameters();
        var request = new AwgControlRequest
        {
            RequestedMode = AwgOperatingMode.Condensation,
            FanControlFraction = 1.0,
            PeltierPowerRequestW = 100,
            RecirculationFraction = 0,
            HeatRecoveryBypassOpen = false,
            AdsorptionBedEnabled = false,
            RegenerationHeatEnabled = false,
            CondenserEnabled = true,
            ReasonCode = "TEST"
        };
        var observation = Observation(
            surfaceC: 5,
            dewPointC: 14,
            powerW: 200,
            peltierHotC: UnitConversions.KelvinToCelsius(parameters.PeltierHotSideLimitK));

        var power = AwgPeltierController.ResolvePowerRequestW(request, observation, parameters);
        Assert.Equal(0.0, power);
    }

    [Fact]
    public void PeltierController_TargetDewPointApproach_ScalesWithError()
    {
        var parameters = RuleBasedAwgController.CreateDefaultParameters();
        var request = new AwgControlRequest
        {
            RequestedMode = AwgOperatingMode.Condensation,
            FanControlFraction = 1.0,
            PeltierPowerRequestW = 120,
            RecirculationFraction = 0,
            HeatRecoveryBypassOpen = false,
            AdsorptionBedEnabled = false,
            RegenerationHeatEnabled = false,
            CondenserEnabled = true,
            ReasonCode = "TEST"
        };

        var far = AwgPeltierController.ResolvePowerRequestW(
            request,
            Observation(surfaceC: 20, dewPointC: 14, powerW: 200),
            parameters,
            AwgPeltierControlStrategy.TargetDewPointApproach);
        var near = AwgPeltierController.ResolvePowerRequestW(
            request,
            Observation(surfaceC: 10, dewPointC: 14, powerW: 200),
            parameters,
            AwgPeltierControlStrategy.TargetDewPointApproach);

        Assert.True(far > near);
        Assert.True(far <= 200.0);
    }

    [Fact]
    public void RecirculationController_ClampsAndClearsDuringRegeneration()
    {
        var parameters = RuleBasedAwgController.CreateDefaultParameters() with
        {
            MaximumRecirculationFraction = 0.4,
            DefaultRecirculationFraction = 0.4
        };
        var request = new AwgControlRequest
        {
            RequestedMode = AwgOperatingMode.Regeneration,
            FanControlFraction = 1.0,
            PeltierPowerRequestW = 0,
            RecirculationFraction = 0.9,
            HeatRecoveryBypassOpen = false,
            AdsorptionBedEnabled = true,
            RegenerationHeatEnabled = true,
            CondenserEnabled = false,
            ReasonCode = "TEST"
        };

        var fraction = AwgRecirculationController.ResolveRecirculationFraction(
            request,
            Observation(),
            parameters);
        Assert.Equal(0.0, fraction);
    }

    private static AwgSystemObservation Observation(
        double airflow = 0.02,
        double surfaceC = 8,
        double dewPointC = 14,
        double powerW = 150,
        double peltierHotC = 40)
        => new()
        {
            SimulationTimeUtc = DateTimeOffset.Parse("2026-01-01T12:00:00Z"),
            AmbientTemperatureK = UnitConversions.CelsiusToKelvin(25.0),
            AmbientRelativeHumidityFraction = 0.50,
            AmbientVaporPressurePa = 1600.0,
            SolarIrradianceWPerSquareMeter = 800.0,
            BatteryStateOfChargeFraction = 0.60,
            AvailableElectricalPowerW = powerW,
            SilicaGelLoadingKgPerKg = 0.05,
            SilicaGelTemperatureK = UnitConversions.CelsiusToKelvin(30.0),
            SilicaGelEquilibriumLoadingKgPerKg = 0.25,
            CondenserSurfaceTemperatureK = UnitConversions.CelsiusToKelvin(surfaceC),
            InletDewPointTemperatureK = UnitConversions.CelsiusToKelvin(dewPointC),
            CondenserInletDewPointTemperatureK = UnitConversions.CelsiusToKelvin(dewPointC),
            PeltierHotSideTemperatureK = UnitConversions.CelsiusToKelvin(peltierHotC),
            PeltierColdSideTemperatureK = UnitConversions.CelsiusToKelvin(surfaceC),
            CollectorAbsorberTemperatureK = UnitConversions.CelsiusToKelvin(55.0),
            ProcessDryAirMassFlowKgPerSecond = airflow,
            WaterTankLevelFraction = 0.1,
            FanOperatingPointValid = true,
            ComponentDiagnostics = Array.Empty<SimulationDiagnostic>()
        };
}
