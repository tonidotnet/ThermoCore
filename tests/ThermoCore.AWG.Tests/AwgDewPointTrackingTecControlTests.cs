using ThermoCore.AWG.Control;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.Tests;

public class AwgDewPointTrackingTecControlTests
{
    [Fact]
    public void HighDewPoint_RequestsLessDriveThanLowDewPoint_AtSameSurface()
    {
        var parameters = RuleBasedAwgController.CreateDefaultParameters();
        var request = EnabledRequest(120);

        var highDp = AwgPeltierController.Resolve(
            request,
            Observation(surfaceC: 12, dewPointC: 22, powerW: 200),
            parameters);
        var lowDp = AwgPeltierController.Resolve(
            request,
            Observation(surfaceC: 12, dewPointC: 13, powerW: 200),
            parameters);

        Assert.True(highDp.PowerRequestW < lowDp.PowerRequestW);
        Assert.True(highDp.TargetSurfaceTemperatureK > lowDp.TargetSurfaceTemperatureK);
        Assert.Equal(
            UnitConversions.CelsiusToKelvin(22.0) - parameters.TargetDewPointApproachK,
            highDp.TargetSurfaceTemperatureK,
            precision: 10);
    }

    [Fact]
    public void ConfigurableMargin_ShiftsTargetSurface()
    {
        var tight = RuleBasedAwgController.CreateDefaultParameters() with { TargetDewPointApproachK = 2.0 };
        var loose = RuleBasedAwgController.CreateDefaultParameters() with { TargetDewPointApproachK = 5.0 };
        var request = EnabledRequest(120);
        var observation = Observation(surfaceC: 15, dewPointC: 18, powerW: 200);

        var tightResult = AwgPeltierController.Resolve(request, observation, tight);
        var looseResult = AwgPeltierController.Resolve(request, observation, loose);

        Assert.Equal(
            observation.CondenserInletDewPointTemperatureK - 2.0,
            tightResult.TargetSurfaceTemperatureK);
        Assert.Equal(
            observation.CondenserInletDewPointTemperatureK - 5.0,
            looseResult.TargetSurfaceTemperatureK);
        // Looser approach (colder target) needs more drive for the same warm surface.
        Assert.True(looseResult.PowerRequestW > tightResult.PowerRequestW);
    }

    [Fact]
    public void MaximumPowerLimit_SaturatesAndEmitsDiagnostic()
    {
        var parameters = RuleBasedAwgController.CreateDefaultParameters() with
        {
            NominalPeltierPowerRequestW = 200.0,
            MaximumPeltierPowerRequestW = 40.0
        };
        var result = AwgPeltierController.Resolve(
            EnabledRequest(200),
            Observation(surfaceC: 25, dewPointC: 14, powerW: 500),
            parameters);

        Assert.Equal(40.0, result.PowerRequestW);
        Assert.True(result.PowerSaturated);
        Assert.Contains(result.Diagnostics, d => d.Code == "CTRL.PELTIER_POWER_SATURATED");
    }

    [Fact]
    public void CurrentAndVoltageLimit_CapsPower()
    {
        var parameters = RuleBasedAwgController.CreateDefaultParameters() with
        {
            NominalPeltierPowerRequestW = 200.0,
            MaximumPeltierCurrentA = 2.0,
            TecOperatingVoltageV = 12.0 // → 24 W electrical cap
        };
        var result = AwgPeltierController.Resolve(
            EnabledRequest(200),
            Observation(surfaceC: 25, dewPointC: 14, powerW: 500),
            parameters);

        Assert.Equal(24.0, result.PowerRequestW);
        Assert.True(result.PowerSaturated);
    }

    [Fact]
    public void NeverExceedsAvailableElectricalPower()
    {
        var parameters = RuleBasedAwgController.CreateDefaultParameters();
        var result = AwgPeltierController.Resolve(
            EnabledRequest(200),
            Observation(surfaceC: 25, dewPointC: 10, powerW: 35),
            parameters);

        Assert.True(result.PowerRequestW <= 35.0 + 1e-9);
    }

    [Fact]
    public void RampLimit_ClampsStepChange()
    {
        var parameters = RuleBasedAwgController.CreateDefaultParameters() with
        {
            PeltierPowerRampLimitWPerSecond = 10.0
        };
        var result = AwgPeltierController.Resolve(
            EnabledRequest(200),
            Observation(surfaceC: 25, dewPointC: 10, powerW: 500),
            parameters,
            previousPowerRequestW: 0.0,
            timeStep: TimeSpan.FromSeconds(2)); // max ΔP = 20 W

        Assert.Equal(20.0, result.PowerRequestW, precision: 6);
        Assert.Equal("power-ramp-limit", result.ActiveLimitingConstraint);
        Assert.Contains(result.Diagnostics, d => d.Code == "CTRL.PELTIER_RAMP_LIMIT");
    }

    [Fact]
    public void UnreachableTarget_EmitsDiagnostic_WhenTargetBelowSurfaceFloor()
    {
        var parameters = RuleBasedAwgController.CreateDefaultParameters() with
        {
            TargetDewPointApproachK = 5.0,
            MinimumCondenserSurfaceTemperatureK = UnitConversions.CelsiusToKelvin(5.0)
        };
        // Dew point 2 °C → target −3 °C, below 5 °C floor; surface still warm.
        var result = AwgPeltierController.Resolve(
            EnabledRequest(120),
            Observation(surfaceC: 12, dewPointC: 2, powerW: 200),
            parameters);

        Assert.True(result.TargetUnreachable);
        Assert.Contains(result.Diagnostics, d =>
            d.Code == "CTRL.PELTIER_TARGET_UNREACHABLE"
            && d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void HotSideLimit_StillZerosDrive()
    {
        var parameters = RuleBasedAwgController.CreateDefaultParameters();
        var result = AwgPeltierController.Resolve(
            EnabledRequest(100),
            Observation(
                surfaceC: 5,
                dewPointC: 14,
                powerW: 200,
                peltierHotC: UnitConversions.KelvinToCelsius(parameters.PeltierHotSideLimitK)),
            parameters);

        Assert.Equal(0.0, result.PowerRequestW);
        Assert.Contains(result.Diagnostics, d => d.Code == "CTRL.PELTIER_HOT_SIDE_LIMIT");
    }

    private static AwgControlRequest EnabledRequest(double peltierW)
        => new()
        {
            RequestedMode = AwgOperatingMode.Condensation,
            FanControlFraction = 1.0,
            PeltierPowerRequestW = peltierW,
            RecirculationFraction = 0,
            HeatRecoveryBypassOpen = false,
            AdsorptionBedEnabled = false,
            RegenerationHeatEnabled = false,
            CondenserEnabled = true,
            ReasonCode = "TEST"
        };

    private static AwgSystemObservation Observation(
        double surfaceC,
        double dewPointC,
        double powerW,
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
            ProcessDryAirMassFlowKgPerSecond = 0.02,
            WaterTankLevelFraction = 0.1,
            FanOperatingPointValid = true,
            ComponentDiagnostics = Array.Empty<SimulationDiagnostic>()
        };
}
