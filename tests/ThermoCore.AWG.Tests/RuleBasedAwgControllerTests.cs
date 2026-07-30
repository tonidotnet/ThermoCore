using ThermoCore.AWG.Control;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.Tests;

public class RuleBasedAwgControllerTests
{
    private readonly RuleBasedAwgController _controller = new();
    private readonly AwgControlParameters _parameters = RuleBasedAwgController.CreateDefaultParameters() with
    {
        MinimumModeDwell = TimeSpan.FromMinutes(5)
    };

    [Fact]
    public void Off_TransitionsToStartup()
    {
        var result = Evaluate(Observation(), AwgControllerState.CreateInitial());

        Assert.Equal(AwgOperatingMode.Startup, result.ProposedState.CurrentMode);
        Assert.Equal("ENTER_STARTUP", result.Request.ReasonCode);
        Assert.Contains(result.DecisionTrace, t => t.ReasonCode == "ENTER_STARTUP");
    }

    [Fact]
    public void Startup_EntersAdsorptionWhenDrivingForceAvailable()
    {
        var startup = Evaluate(Observation(loading: 0.05, equilibrium: 0.25), AwgControllerState.CreateInitial());
        Assert.Equal(AwgOperatingMode.Startup, startup.ProposedState.CurrentMode);

        var adsorption = Evaluate(Observation(loading: 0.05, equilibrium: 0.25), startup.ProposedState);
        Assert.Equal(AwgOperatingMode.Adsorption, adsorption.ProposedState.CurrentMode);
        Assert.Contains("ADSORPTION", adsorption.Request.ReasonCode, StringComparison.Ordinal);
    }

    [Fact]
    public void Adsorption_ExitsToRegenerationAtEntryLoadingWithSolar()
    {
        var adsorption = ModeState(AwgOperatingMode.Adsorption, TimeSpan.FromMinutes(10));
        var result = Evaluate(
            Observation(loading: 0.22, equilibrium: 0.10, solar: 600),
            adsorption);

        Assert.Equal(AwgOperatingMode.Regeneration, result.ProposedState.CurrentMode);
        Assert.True(result.Request.RegenerationHeatEnabled);
        Assert.True(result.Request.AdsorptionBedEnabled);
    }

    [Fact]
    public void Regeneration_UsesHysteresis_DoesNotExitAboveExitLoading()
    {
        var regen = ModeState(AwgOperatingMode.Regeneration, TimeSpan.FromMinutes(10));
        var hold = Evaluate(
            Observation(loading: 0.12, equilibrium: 0.05, solar: 600),
            regen);

        Assert.Equal(AwgOperatingMode.Regeneration, hold.ProposedState.CurrentMode);

        var exit = Evaluate(
            Observation(loading: 0.07, equilibrium: 0.05, solar: 600, surfaceC: 2, dewPointC: 12),
            regen);

        Assert.Equal(AwgOperatingMode.Condensation, exit.ProposedState.CurrentMode);
    }

    [Fact]
    public void Condensation_RequiresDewPointMargin()
    {
        var condensation = ModeState(AwgOperatingMode.Condensation, TimeSpan.FromMinutes(10));

        var disabled = Evaluate(
            Observation(loading: 0.05, equilibrium: 0.20, surfaceC: 14, dewPointC: 12, powerW: 200),
            condensation);
        Assert.False(disabled.Request.CondenserEnabled);

        var enabled = Evaluate(
            Observation(loading: 0.05, equilibrium: 0.20, surfaceC: 5, dewPointC: 12, powerW: 200),
            condensation);
        Assert.True(enabled.Request.CondenserEnabled);
        Assert.True(enabled.Request.PeltierPowerRequestW > 0.0);
    }

    [Fact]
    public void BatteryReserve_DeratesPeltierAndHoldsStandby()
    {
        var condensation = ModeState(AwgOperatingMode.Condensation, TimeSpan.FromMinutes(10));
        var result = Evaluate(
            Observation(loading: 0.05, equilibrium: 0.20, surfaceC: 5, dewPointC: 12, powerW: 200, soc: 0.20),
            condensation);

        Assert.Equal(AwgOperatingMode.Standby, result.ProposedState.CurrentMode);
        Assert.Contains("RESERVE", result.Request.ReasonCode, StringComparison.Ordinal);
    }

    [Fact]
    public void BatteryCritical_EntersControlledShutdown()
    {
        var result = Evaluate(
            Observation(soc: 0.05),
            ModeState(AwgOperatingMode.Adsorption, TimeSpan.FromMinutes(10)));

        Assert.Equal(AwgOperatingMode.ControlledShutdown, result.ProposedState.CurrentMode);
        Assert.Equal(AwgFaultCode.BatteryBelowCriticalSoc, result.Request.ActiveFaultCode);
        Assert.Equal(0.0, result.Request.PeltierPowerRequestW);
    }

    [Fact]
    public void ThermalProtection_FaultsOnPeltierHotSide()
    {
        var result = Evaluate(
            Observation(peltierHotC: 80),
            ModeState(AwgOperatingMode.Condensation, TimeSpan.FromMinutes(10)));

        Assert.Equal(AwgOperatingMode.Fault, result.ProposedState.CurrentMode);
        Assert.True(result.ProposedState.IsLatchedFault);
        Assert.Equal(AwgFaultCode.PeltierHotSideOverTemperature, result.Request.ActiveFaultCode);
    }

    [Fact]
    public void MinimumDwell_BlocksNonSafetyTransition()
    {
        var adsorption = ModeState(AwgOperatingMode.Adsorption, TimeSpan.FromMinutes(1));
        var result = Evaluate(
            Observation(loading: 0.22, equilibrium: 0.10, solar: 600),
            adsorption);

        Assert.Equal(AwgOperatingMode.Adsorption, result.ProposedState.CurrentMode);
        Assert.Equal("DWELL_HOLD", result.Request.ReasonCode);
        Assert.Contains(result.Diagnostics, d => d.Code == "CTRL.DWELL_HOLD");
    }

    [Fact]
    public void RecirculationFraction_IsBoundedByMaximum()
    {
        var parameters = _parameters with
        {
            DefaultRecirculationFraction = 0.4,
            MaximumRecirculationFraction = 0.4
        };
        var result = _controller.Evaluate(
            Observation(),
            ModeState(AwgOperatingMode.Recirculation, TimeSpan.FromMinutes(10)),
            parameters,
            TimeSpan.FromSeconds(60));

        Assert.InRange(result.Request.RecirculationFraction, 0.0, parameters.MaximumRecirculationFraction);
    }

    [Fact]
    public void WaterTankFull_DisablesCondensation()
    {
        var result = Evaluate(
            Observation(loading: 0.05, equilibrium: 0.05, surfaceC: 5, dewPointC: 12, powerW: 200, tank: 1.0),
            ModeState(AwgOperatingMode.Condensation, TimeSpan.FromMinutes(10)));

        Assert.False(result.Request.CondenserEnabled);
        Assert.Equal(0.0, result.Request.PeltierPowerRequestW);
        Assert.Contains(result.Diagnostics, d => d.Code == "CTRL.WATER_TANK_FULL");
    }

    [Fact]
    public void FanOperatingPointFailure_LatchesFault()
    {
        var result = Evaluate(
            Observation(fanValid: false),
            ModeState(AwgOperatingMode.Adsorption, TimeSpan.FromMinutes(10)));

        Assert.Equal(AwgOperatingMode.Fault, result.ProposedState.CurrentMode);
        Assert.True(result.ProposedState.IsLatchedFault);
        Assert.Equal(AwgFaultCode.FanOperatingPointUnavailable, result.Request.ActiveFaultCode);
    }

    [Fact]
    public void LatchedFault_RemainsFaultOnSubsequentSteps()
    {
        var faulted = Evaluate(Observation(fanValid: false), ModeState(AwgOperatingMode.Adsorption, TimeSpan.FromMinutes(10)));
        var held = Evaluate(Observation(), faulted.ProposedState);

        Assert.Equal(AwgOperatingMode.Fault, held.ProposedState.CurrentMode);
        Assert.Equal("LATCHED_FAULT", held.Request.ReasonCode);
    }

    [Fact]
    public void DecisionTrace_IsDeterministicForIdenticalInputs()
    {
        var observation = Observation(loading: 0.05, equilibrium: 0.25);
        var state = AwgControllerState.CreateInitial();
        var a = Evaluate(observation, state);
        var b = Evaluate(observation, state);

        Assert.Equal(a.Request, b.Request);
        Assert.Equal(a.ProposedState, b.ProposedState);
        Assert.Equal(a.DecisionTrace.Count, b.DecisionTrace.Count);
        Assert.Equal(a.DecisionTrace.First().ReasonCode, b.DecisionTrace.First().ReasonCode);
    }

    private AwgControlStepResult Evaluate(AwgSystemObservation observation, AwgControllerState state)
        => _controller.Evaluate(observation, state, _parameters, TimeSpan.FromSeconds(60));

    private static AwgControllerState ModeState(AwgOperatingMode mode, TimeSpan dwell)
        => new()
        {
            CurrentMode = mode,
            TimeInCurrentMode = dwell,
            LastModeChangeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            ConsecutiveFaultCount = 0,
            IsLatchedFault = false,
            LastTransitionReasonCode = "TEST",
            ActiveFaultCode = AwgFaultCode.None
        };

    private static AwgSystemObservation Observation(
        double loading = 0.05,
        double equilibrium = 0.25,
        double solar = 800,
        double soc = 0.60,
        double powerW = 150,
        double surfaceC = 8,
        double dewPointC = 14,
        double peltierHotC = 40,
        double tank = 0.1,
        bool fanValid = true)
        => new()
        {
            SimulationTimeUtc = DateTimeOffset.Parse("2026-01-01T12:00:00Z"),
            AmbientTemperatureK = UnitConversions.CelsiusToKelvin(25.0),
            AmbientRelativeHumidityFraction = 0.50,
            AmbientVaporPressurePa = 1600.0,
            SolarIrradianceWPerSquareMeter = solar,
            BatteryStateOfChargeFraction = soc,
            AvailableElectricalPowerW = powerW,
            SilicaGelLoadingKgPerKg = loading,
            SilicaGelTemperatureK = UnitConversions.CelsiusToKelvin(30.0),
            SilicaGelEquilibriumLoadingKgPerKg = equilibrium,
            CondenserSurfaceTemperatureK = UnitConversions.CelsiusToKelvin(surfaceC),
            InletDewPointTemperatureK = UnitConversions.CelsiusToKelvin(dewPointC),
            CondenserInletDewPointTemperatureK = UnitConversions.CelsiusToKelvin(dewPointC),
            PeltierHotSideTemperatureK = UnitConversions.CelsiusToKelvin(peltierHotC),
            PeltierColdSideTemperatureK = UnitConversions.CelsiusToKelvin(surfaceC),
            CollectorAbsorberTemperatureK = UnitConversions.CelsiusToKelvin(55.0),
            ProcessDryAirMassFlowKgPerSecond = 0.02,
            WaterTankLevelFraction = tank,
            FanOperatingPointValid = fanValid,
            ComponentDiagnostics = Array.Empty<SimulationDiagnostic>()
        };
}
