using ThermoCore.Core.Components;
using ThermoCore.Core.Components.Adsorption;
using ThermoCore.Core.Components.Power;
using ThermoCore.Core.Components.Thermoelectric;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

/// <summary>
/// Graph-level integration coverage for TEC-008, SG-010, COND-007, PWR-006, and SC-002.
/// </summary>
public class ComponentIntegrationTests
{
    private readonly PsychrometricCalculator _calculator = new();

    [Fact]
    public void Tec008_ConstantCop_GraphEnergyIdentity_AndHeatSinks()
    {
        var tec = new ConstantCopPeltierComponent(
            "tec",
            coolingCop: 1.2,
            electricalPowerW: 50.0,
            coldSideTemperatureK: UnitConversions.CelsiusToKelvin(10.0),
            hotSideTemperatureK: UnitConversions.CelsiusToKelvin(40.0));
        var coldSink = new EnvironmentHeatSinkComponent("cold_sink");
        var hotSink = new EnvironmentHeatSinkComponent("hot_sink");

        var result = Run(
            [tec, coldSink, hotSink],
            [
                Connect("c", "tec", "cold_heat", "cold_sink", "inlet"),
                Connect("h", "tec", "hot_heat", "hot_sink", "inlet")
            ]);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Equal(60.0, tec.LastColdSideHeatW, precision: 8);
        Assert.Equal(110.0, tec.LastHotSideHeatW, precision: 8);
        Assert.Equal(60.0, coldSink.LastHeatFlowW, precision: 8);
        Assert.Equal(110.0, hotSink.LastHeatFlowW, precision: 8);
    }

    [Fact]
    public void Tec008_Analytical_OffStateConduction_InGraph()
    {
        var parameters = AnalyticalPeltierParameters.CreateProvisionalEngineeringDefaults() with
        {
            EnableProtectionShutdown = false
        };
        var tec = new AnalyticalPeltierComponent(
            "tec",
            parameters,
            UnitConversions.CelsiusToKelvin(10.0),
            UnitConversions.CelsiusToKelvin(40.0),
            requestedElectricalPowerW: 0.0);
        var coldSink = new EnvironmentHeatSinkComponent("cold_sink");
        var hotSink = new EnvironmentHeatSinkComponent("hot_sink");

        var result = Run(
            [tec, coldSink, hotSink],
            [
                Connect("c", "tec", "cold_heat", "cold_sink", "inlet"),
                Connect("h", "tec", "hot_heat", "hot_sink", "inlet")
            ]);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Equal(0.0, tec.LastElectricalPowerW, precision: 12);
        // Off-state conduction dumps heat toward the cold face: Qc = Qh = -K·ΔT (< 0 when Th > Tc).
        Assert.True(tec.LastColdSideHeatW < 0.0);
        Assert.Equal(tec.LastHotSideHeatW, tec.LastColdSideHeatW, precision: 8);
        Assert.Contains(result.Diagnostics, d => d.Code == "PELTIER.OFF_STATE_CONDUCTION");
    }

    [Fact]
    public void Tec008_Analytical_PoweredOperation_SatisfiesQhEqualsQcPlusPe()
    {
        var parameters = AnalyticalPeltierParameters.CreateProvisionalEngineeringDefaults() with
        {
            EnableProtectionShutdown = false,
            HotSideThermalResistanceKPerW = 0.0,
            ColdSideThermalResistanceKPerW = 0.0
        };
        var tec = new AnalyticalPeltierComponent(
            "tec",
            parameters,
            UnitConversions.CelsiusToKelvin(10.0),
            UnitConversions.CelsiusToKelvin(35.0),
            requestedElectricalPowerW: 25.0);
        var coldSink = new EnvironmentHeatSinkComponent("cold_sink");
        var hotSink = new EnvironmentHeatSinkComponent("hot_sink");

        var result = Run(
            [tec, coldSink, hotSink],
            [
                Connect("c", "tec", "cold_heat", "cold_sink", "inlet"),
                Connect("h", "tec", "hot_heat", "hot_sink", "inlet")
            ]);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.True(tec.LastElectricalPowerW > 0.0);
        Assert.Equal(
            tec.LastColdSideHeatW + tec.LastElectricalPowerW,
            tec.LastHotSideHeatW,
            precision: 6);
    }

    [Fact]
    public void Sg010_Adsorption_OverMultipleSteps_ConservesWater()
    {
        var parameters = DefaultSilica();
        var isotherm = GenericPolynomialIsotherm.CreateLinear(parameters.MaximumWaterLoadingKgPerKgDryAdsorbent);
        var initial = SilicaGelState.Create(
            dryAdsorbentMassKg: parameters.DryAdsorbentMassKg,
            waterLoadingKgPerKgDryAdsorbent: 0.05,
            bedTemperatureK: UnitConversions.CelsiusToKelvin(25.0),
            maximumWaterLoadingKgPerKgDryAdsorbent: parameters.MaximumWaterLoadingKgPerKgDryAdsorbent,
            minimumRegeneratedLoadingKgPerKgDryAdsorbent: parameters.MinimumRegeneratedLoadingKgPerKgDryAdsorbent,
            effectiveSpecificHeatJPerKgK: parameters.EffectiveSpecificHeatJPerKgK,
            bedHousingThermalCapacityJPerK: parameters.BedHousingThermalCapacityJPerK);
        var inlet = SampleAir(25, 0.70, 0.02);
        var source = new AmbientAirSourceComponent("air", inlet);
        var bed = new SilicaGelBedComponent("sg", parameters, isotherm, initial, _calculator);
        var sink = new ExhaustAirSinkComponent("sink");

        var result = Run(
            [source, bed, sink],
            [
                Connect("a_s", "air", "outlet", "sg", "inlet"),
                Connect("s_k", "sg", "outlet", "sink", "inlet")
            ],
            durationSeconds: 20,
            timeStepSeconds: 5);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.True(bed.State.WaterLoadingKgPerKgDryAdsorbent > initial.WaterLoadingKgPerKgDryAdsorbent);
        Assert.Equal(SilicaGelOperatingRegime.Adsorption, bed.State.OperatingRegime);
        var outlet = Assert.IsType<MoistAirState>(result.Steps[^1].PortStates["sg.outlet"]);
        Assert.True(outlet.HumidityRatioKgPerKgDryAir < inlet.HumidityRatioKgPerKgDryAir);
        Assert.All(result.Steps, step => Assert.True(Math.Abs(step.SystemBalance.WaterMassResidualKg) < 1e-6));
    }

    [Fact]
    public void Sg010_Desorption_WithExternalHeat_InGraph()
    {
        var parameters = DefaultSilica() with
        {
            AmbientTemperatureK = UnitConversions.CelsiusToKelvin(70.0),
            ReferenceMassTransferCoefficientPerSecond = 0.05,
            EnableEnergyLimitedDesorption = true,
            MinimumDesorptionBedTemperatureK = UnitConversions.CelsiusToKelvin(65.0)
        };
        var isotherm = GenericPolynomialIsotherm.CreateLinear(parameters.MaximumWaterLoadingKgPerKgDryAdsorbent);
        var initial = SilicaGelState.Create(
            dryAdsorbentMassKg: parameters.DryAdsorbentMassKg,
            waterLoadingKgPerKgDryAdsorbent: 0.28,
            bedTemperatureK: UnitConversions.CelsiusToKelvin(70.0),
            maximumWaterLoadingKgPerKgDryAdsorbent: parameters.MaximumWaterLoadingKgPerKgDryAdsorbent,
            minimumRegeneratedLoadingKgPerKgDryAdsorbent: parameters.MinimumRegeneratedLoadingKgPerKgDryAdsorbent,
            effectiveSpecificHeatJPerKgK: parameters.EffectiveSpecificHeatJPerKgK,
            bedHousingThermalCapacityJPerK: parameters.BedHousingThermalCapacityJPerK);
        var inlet = SampleAir(70, 0.10, 0.02);
        var source = new AmbientAirSourceComponent("air", inlet);
        var heat = new PrescribedHeatSourceComponent(
            "heat",
            heatFlowW: 500.0,
            temperatureK: UnitConversions.CelsiusToKelvin(90.0));
        var bed = new SilicaGelBedComponent("sg", parameters, isotherm, initial, _calculator);
        var sink = new ExhaustAirSinkComponent("sink");

        var result = Run(
            [source, heat, bed, sink],
            [
                Connect("a_s", "air", "outlet", "sg", "inlet"),
                Connect("h_s", "heat", "outlet", "sg", "external_heat"),
                Connect("s_k", "sg", "outlet", "sink", "inlet")
            ],
            durationSeconds: 10,
            timeStepSeconds: 5);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.True(bed.State.WaterLoadingKgPerKgDryAdsorbent < initial.WaterLoadingKgPerKgDryAdsorbent);
        var outlet = Assert.IsType<MoistAirState>(result.Steps[^1].PortStates["sg.outlet"]);
        Assert.True(outlet.HumidityRatioKgPerKgDryAir > inlet.HumidityRatioKgPerKgDryAir);
    }

    [Fact]
    public void Cond007_PeltierCooledCondenser_ProducesWater()
    {
        var inlet = SampleAir(30, 0.85, 0.02);
        var source = new AmbientAirSourceComponent("air", inlet);
        var tec = new ConstantCopPeltierComponent(
            "tec",
            coolingCop: 1.5,
            electricalPowerW: 80.0,
            coldSideTemperatureK: UnitConversions.CelsiusToKelvin(5.0),
            hotSideTemperatureK: UnitConversions.CelsiusToKelvin(40.0));
        var condenser = new CondenserComponent(
            "cond",
            bypassFactor: 0.1,
            drainageEfficiency: 0.95,
            fallbackSurfaceTemperatureK: UnitConversions.CelsiusToKelvin(20.0),
            fallbackAvailableCoolingPowerW: 1.0,
            calculator: _calculator);
        var hotSink = new EnvironmentHeatSinkComponent("hot_sink");
        var airSink = new ExhaustAirSinkComponent("sink");
        var drain = new LiquidWaterSinkComponent("drain");

        var result = Run(
            [source, tec, condenser, hotSink, airSink, drain],
            [
                Connect("a_c", "air", "outlet", "cond", "inlet"),
                Connect("t_c", "tec", "cold_heat", "cond", "cooling"),
                Connect("t_h", "tec", "hot_heat", "hot_sink", "inlet"),
                Connect("c_s", "cond", "outlet", "sink", "inlet"),
                Connect("c_d", "cond", "liquid_out", "drain", "inlet")
            ]);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.True(condenser.LastCondensedWaterRateKgPerSecond > 0.0);
        Assert.True(condenser.LastCollectedWaterRateKgPerSecond > 0.0);
        Assert.True(drain.LastMassFlowKgPerSecond > 0.0);
        Assert.True(condenser.LastTotalCoolingPowerW <= tec.LastColdSideHeatW + 1e-6);
        var outlet = Assert.IsType<MoistAirState>(result.Steps[0].PortStates["cond.outlet"]);
        Assert.True(outlet.HumidityRatioKgPerKgDryAir < inlet.HumidityRatioKgPerKgDryAir);
    }

    [Fact]
    public void Cond007_DrainageAndFilm_AcrossSteps()
    {
        var inlet = SampleAir(32, 0.90, 0.025);
        var source = new AmbientAirSourceComponent("air", inlet);
        var condenser = new CondenserComponent(
            "cond",
            bypassFactor: 0.05,
            drainageEfficiency: 0.6,
            fallbackSurfaceTemperatureK: UnitConversions.CelsiusToKelvin(6.0),
            fallbackAvailableCoolingPowerW: 3000.0,
            maximumRetainedFilmKg: 0.002,
            calculator: _calculator);
        var sink = new ExhaustAirSinkComponent("sink");
        var drain = new LiquidWaterSinkComponent("drain");

        var result = Run(
            [source, condenser, sink, drain],
            [
                Connect("a_c", "air", "outlet", "cond", "inlet"),
                Connect("c_s", "cond", "outlet", "sink", "inlet"),
                Connect("c_d", "cond", "liquid_out", "drain", "inlet")
            ],
            durationSeconds: 5,
            timeStepSeconds: 1);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.True(condenser.LastCondensedWaterRateKgPerSecond > 0.0);
        Assert.True(condenser.LastRetainedFilmKg >= 0.0);
        Assert.True(condenser.LastRetainedFilmKg <= 0.002 + 1e-12);
        Assert.True(condenser.LastEffectiveDrainageEfficiency >= 0.6);
    }

    [Fact]
    public void Pwr006_PvToPowerManager_CurtailsWhenBatteryFull()
    {
        var sun = new SolarRadiationSourceComponent("sun", irradianceWPerM2: 1000.0);
        var pv = new ConstantEfficiencySolarPanelComponent("pv", efficiency: 0.2, areaM2: 2.0);
        var battery = new BatteryParameters
        {
            NominalCapacityJ = 3_600_000.0,
            MinimumSocFraction = 0.1,
            MaximumSocFraction = 0.9,
            ChargeEfficiencyFraction = 0.95,
            DischargeEfficiencyFraction = 0.95,
            MaximumChargePowerW = 50.0,
            MaximumDischargePowerW = 50.0
        };
        // Near maximum SOC so charge headroom is small and surplus must curtail.
        var initial = BatteryState.Create(0.899 * battery.NominalCapacityJ, battery.NominalCapacityJ, 298.15);
        var pm = new PowerManagementComponent(
            "pm",
            battery,
            [
                new ElectricalLoadDemand
                {
                    LoadId = "controller",
                    RequestedPowerW = 20.0,
                    Priority = 0,
                    IsEssential = true
                }
            ],
            initial,
            mpptEfficiencyFraction: 1.0);
        var bus = new ElectricalLoadSinkComponent("bus");
        var curtailed = new ElectricalLoadSinkComponent("curtailed");

        var result = Run(
            [sun, pv, pm, bus, curtailed],
            [
                Connect("s_p", "sun", "outlet", "pv", "solar"),
                Connect("p_m", "pv", "electrical", "pm", "generation"),
                Connect("m_b", "pm", "bus", "bus", "inlet"),
                Connect("m_c", "pm", "curtailed", "curtailed", "inlet")
            ]);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Equal(400.0, pv.LastElectricalPowerW, precision: 8);
        Assert.Equal(20.0, pm.LastServedLoadPowerW, precision: 8);
        Assert.True(pm.LastCurtailedPowerW > 0.0);
        Assert.Equal(pm.LastCurtailedPowerW, curtailed.LastPowerW, precision: 8);
        Assert.Contains(result.Diagnostics, d => d.Code == "POWER.SOLAR_CURTAILED");
    }

    [Fact]
    public void Sc002_OpticalAbsorption_ScalesWithEfficiencyAndIam()
    {
        var inlet = SampleAir(25, 0.5, 0.02);
        var source = new AmbientAirSourceComponent("air", inlet);
        var sun = new SolarRadiationSourceComponent("sun", irradianceWPerM2: 800.0);
        var collector = new OpticalAbsorptionSolarCollectorComponent(
            "collector",
            opticalEfficiencyFraction: 0.75,
            apertureAreaM2: 1.0,
            incidenceAngleModifierFraction: 1.0,
            calculator: _calculator);
        var sink = new ExhaustAirSinkComponent("sink");

        var result = Run(
            [source, sun, collector, sink],
            [
                Connect("a_c", "air", "outlet", "collector", "inlet"),
                Connect("s_c", "sun", "outlet", "collector", "solar"),
                Connect("c_k", "collector", "outlet", "sink", "inlet")
            ]);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Equal(600.0, collector.LastAbsorbedSolarPowerW, precision: 8);
        Assert.Equal(collector.LastAbsorbedSolarPowerW, collector.LastUsefulHeatW, precision: 12);
        var outlet = Assert.IsType<MoistAirState>(result.Steps[0].PortStates["collector.outlet"]);
        Assert.True(outlet.TemperatureK > inlet.TemperatureK);
        Assert.Equal(inlet.HumidityRatioKgPerKgDryAir, outlet.HumidityRatioKgPerKgDryAir, precision: 12);
    }

    [Fact]
    public void Sc002_IncidenceAngleModifier_ReducesAbsorbedPower()
    {
        var iam = OpticalAbsorptionSolarCollectorComponent.IncidenceAngleModifierFromAngleRadians(Math.PI / 3.0);
        Assert.Equal(0.5, iam, precision: 8);

        var inlet = SampleAir(25, 0.5, 0.02);
        var source = new AmbientAirSourceComponent("air", inlet);
        var sun = new SolarRadiationSourceComponent("sun", 1000.0);
        var collector = OpticalAbsorptionSolarCollectorComponent.CreateFromCoverAndAbsorber(
            "collector",
            coverSolarTransmittanceFraction: 0.9,
            absorberSolarAbsorptanceFraction: 0.95,
            apertureAreaM2: 1.0,
            incidenceAngleModifierFraction: iam,
            calculator: _calculator);
        var sink = new ExhaustAirSinkComponent("sink");

        var result = Run(
            [source, sun, collector, sink],
            [
                Connect("a_c", "air", "outlet", "collector", "inlet"),
                Connect("s_c", "sun", "outlet", "collector", "solar"),
                Connect("c_k", "collector", "outlet", "sink", "inlet")
            ]);

        Assert.True(result.Succeeded, FormatDiagnostics(result));
        Assert.Equal(0.9 * 0.95, collector.LastOpticalEfficiencyFraction, precision: 12);
        Assert.Equal(1000.0 * 0.9 * 0.95 * 0.5, collector.LastAbsorbedSolarPowerW, precision: 8);
        Assert.Contains(result.Diagnostics, d => d.Code == "COLLECTOR.INCIDENCE_ANGLE_MODIFIER");
    }

    private static SilicaGelParameters DefaultSilica()
        => new()
        {
            DryAdsorbentMassKg = 2.0,
            MaximumWaterLoadingKgPerKgDryAdsorbent = 0.35,
            MinimumRegeneratedLoadingKgPerKgDryAdsorbent = 0.02,
            EffectiveSpecificHeatJPerKgK = 920.0,
            BedHousingThermalCapacityJPerK = 500.0,
            EffectiveHeatOfAdsorptionJPerKgWater = 2_600_000.0,
            BedHeatLossCoefficientWPerK = 0.5,
            ReferenceMassTransferCoefficientPerSecond = 0.02,
            AmbientTemperatureK = UnitConversions.CelsiusToKelvin(25.0),
            AirBedHeatTransferCoefficientWPerK = 80.0
        };

    private MoistAirState SampleAir(double temperatureC, double relativeHumidity, double dryAirMassFlow)
        => _calculator.CreateFromRelativeHumidity(
            UnitConversions.CelsiusToKelvin(temperatureC),
            PhysicalConstants.StandardAtmosphericPressurePa,
            relativeHumidity,
            dryAirMassFlow);

    private static SimulationRunResult Run(
        IReadOnlyList<ISimulationComponent> components,
        IReadOnlyList<PhysicalConnection> connections,
        double durationSeconds = 1,
        double timeStepSeconds = 1)
        => new SimulationEngine().Run(new SimulationRequest
        {
            Graph = new SimulationGraph(components, connections),
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(durationSeconds),
            TimeStep = TimeSpan.FromSeconds(timeStepSeconds)
        });

    private static PhysicalConnection Connect(
        string id,
        string sourceComponent,
        string sourcePort,
        string targetComponent,
        string targetPort)
        => new()
        {
            Id = id,
            SourceComponentId = sourceComponent,
            SourcePortId = sourcePort,
            TargetComponentId = targetComponent,
            TargetPortId = targetPort
        };

    private static string FormatDiagnostics(SimulationRunResult result)
        => string.Join("; ", result.Diagnostics.Select(d => $"{d.Code}:{d.Message}"));
}
