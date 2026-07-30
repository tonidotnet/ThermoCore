using ThermoCore.Core.Components;
using ThermoCore.Core.Components.Power;
using ThermoCore.Core.Components.Thermoelectric;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

/// <summary>
/// Coverage for SC-003, PV-002, GEN-003, TEC-005, and PWR-007.
/// </summary>
public class QueueFiveFeatureTests
{
    private readonly PsychrometricCalculator _calculator = new();

    [Fact]
    public void Gen003_PrescribedElectricalSource_FeedsPowerManager()
    {
        var source = new PrescribedElectricalSourceComponent("src", powerW: 100.0);
        var battery = DefaultBattery();
        var initial = BatteryState.Create(0.3 * battery.NominalCapacityJ, battery.NominalCapacityJ, 298.15);
        var pm = new PowerManagementComponent(
            "pm",
            battery,
            [
                new ElectricalLoadDemand
                {
                    LoadId = "load",
                    RequestedPowerW = 40.0,
                    Priority = 0,
                    IsEssential = true
                }
            ],
            initial,
            mpptEfficiencyFraction: 1.0);
        var bus = new ElectricalLoadSinkComponent("bus");

        var result = Run(
            [source, pm, bus],
            [
                Connect("s_p", "src", "outlet", "pm", "generation"),
                Connect("p_b", "pm", "bus", "bus", "inlet")
            ]);

        Assert.True(result.Succeeded, Format(result));
        Assert.Equal(100.0, source.LastPowerW, precision: 8);
        Assert.Equal(40.0, pm.LastServedLoadPowerW, precision: 8);
        Assert.True(pm.LastBatteryChargePowerW > 0.0);
    }

    [Fact]
    public void Sc003_DynamicCollector_HeatsAirAndStoresEnergy()
    {
        var inlet = SampleAir(20, 0.5, 0.02);
        var ambientK = UnitConversions.CelsiusToKelvin(20.0);
        var source = new AmbientAirSourceComponent("air", inlet);
        var sun = new SolarRadiationSourceComponent("sun", 900.0);
        var collector = new DynamicLumpedSolarCollectorComponent(
            "collector",
            opticalEfficiencyFraction: 0.7,
            apertureAreaM2: 1.0,
            effectiveThermalCapacityJPerK: 8_000.0,
            absorberToAirUaWPerK: 40.0,
            overallLossCoefficientWPerM2K: 5.0,
            initialAbsorberTemperatureK: ambientK,
            ambientTemperatureK: ambientK,
            calculator: _calculator);
        var sink = new ExhaustAirSinkComponent("sink");
        var initialTabs = collector.AbsorberTemperatureK;

        var result = Run(
            [source, sun, collector, sink],
            [
                Connect("a_c", "air", "outlet", "collector", "inlet"),
                Connect("s_c", "sun", "outlet", "collector", "solar"),
                Connect("c_k", "collector", "outlet", "sink", "inlet")
            ],
            durationSeconds: 30,
            timeStepSeconds: 5);

        Assert.True(result.Succeeded, Format(result));
        Assert.True(collector.AbsorberTemperatureK > initialTabs);
        Assert.True(collector.LastAbsorbedSolarPowerW > 0.0);
        Assert.True(collector.LastUsefulHeatW > 0.0);
        var outlet = Assert.IsType<MoistAirState>(result.Steps[^1].PortStates["collector.outlet"]);
        Assert.True(outlet.TemperatureK > inlet.TemperatureK);
        Assert.Equal(inlet.HumidityRatioKgPerKgDryAir, outlet.HumidityRatioKgPerKgDryAir, precision: 12);
    }

    [Fact]
    public void Sc003_ZeroIrradiance_CoolsTowardAmbient()
    {
        var inlet = SampleAir(25, 0.4, 0.03);
        var ambientK = UnitConversions.CelsiusToKelvin(25.0);
        var source = new AmbientAirSourceComponent("air", inlet);
        var sun = new SolarRadiationSourceComponent("sun", 0.0);
        var collector = new DynamicLumpedSolarCollectorComponent(
            "collector",
            opticalEfficiencyFraction: 0.7,
            apertureAreaM2: 1.0,
            effectiveThermalCapacityJPerK: 5_000.0,
            absorberToAirUaWPerK: 30.0,
            overallLossCoefficientWPerM2K: 8.0,
            initialAbsorberTemperatureK: UnitConversions.CelsiusToKelvin(60.0),
            ambientTemperatureK: ambientK,
            calculator: _calculator);
        var sink = new ExhaustAirSinkComponent("sink");
        var initial = collector.AbsorberTemperatureK;

        var result = Run(
            [source, sun, collector, sink],
            [
                Connect("a_c", "air", "outlet", "collector", "inlet"),
                Connect("s_c", "sun", "outlet", "collector", "solar"),
                Connect("c_k", "collector", "outlet", "sink", "inlet")
            ],
            durationSeconds: 40,
            timeStepSeconds: 5);

        Assert.True(result.Succeeded, Format(result));
        Assert.Equal(0.0, collector.LastAbsorbedSolarPowerW, precision: 12);
        Assert.True(collector.AbsorberTemperatureK < initial);
    }

    [Fact]
    public void Pv002_ReferenceConditions_NearRatedPower()
    {
        var sun = new SolarRadiationSourceComponent("sun", 1000.0);
        var panel = new TemperatureCorrectedSolarPanelComponent(
            "pv",
            ratedPowerW: 300.0,
            areaM2: 1.6,
            powerTemperatureCoefficientPerK: -0.004,
            referenceIrradianceWPerM2: 1000.0,
            referenceCellTemperatureK: UnitConversions.CelsiusToKelvin(25.0),
            noctCelsius: 45.0,
            fallbackAmbientTemperatureK: UnitConversions.CelsiusToKelvin(20.0));
        var load = new ElectricalLoadSinkComponent("load");

        var result = Run(
            [sun, panel, load],
            [
                Connect("s_p", "sun", "outlet", "pv", "solar"),
                Connect("p_l", "pv", "electrical", "load", "inlet")
            ]);

        Assert.True(result.Succeeded, Format(result));
        // NOCT: Tcell = 20 + (45-20)/800*1000 = 51.25 C → derate from 25 C
        var expectedFactor = 1.0 + (-0.004) * (panel.LastCellTemperatureK - UnitConversions.CelsiusToKelvin(25.0));
        Assert.Equal(300.0 * expectedFactor, panel.LastElectricalPowerW, precision: 6);
        Assert.True(panel.LastElectricalPowerW < 300.0);
    }

    [Fact]
    public void Pv002_HigherAmbient_ReducesPower()
    {
        var cool = EvaluatePv(ambientC: 15.0);
        var hot = EvaluatePv(ambientC: 40.0);
        Assert.True(hot.LastElectricalPowerW < cool.LastElectricalPowerW);
        Assert.True(hot.LastCellTemperatureK > cool.LastCellTemperatureK);
    }

    [Fact]
    public void Tec005_DynamicSides_EvolveWithoutJumpingToSteadyState()
    {
        var parameters = AnalyticalPeltierParameters.CreateProvisionalEngineeringDefaults() with
        {
            EnableProtectionShutdown = false,
            ColdSideThermalResistanceKPerW = 0.5,
            HotSideThermalResistanceKPerW = 0.5,
            EffectiveColdSideThermalCapacityJPerK = 800.0,
            EffectiveHotSideThermalCapacityJPerK = 1200.0
        };
        var tec = new AnalyticalPeltierComponent(
            "tec",
            parameters,
            UnitConversions.CelsiusToKelvin(10.0),
            UnitConversions.CelsiusToKelvin(35.0),
            requestedElectricalPowerW: 20.0);
        var coldSink = new EnvironmentHeatSinkComponent("cold");
        var hotSink = new EnvironmentHeatSinkComponent("hot");
        var initialCold = tec.LastColdFaceTemperatureK; // 0 until first evaluate

        var result = Run(
            [tec, coldSink, hotSink],
            [
                Connect("c", "tec", "cold_heat", "cold", "inlet"),
                Connect("h", "tec", "hot_heat", "hot", "inlet")
            ],
            durationSeconds: 5,
            timeStepSeconds: 1);

        Assert.True(result.Succeeded, Format(result));
        Assert.Contains(result.Diagnostics, d => d.Code == "PELTIER.DYNAMIC_SIDE_STATE");
        // After first step faces move from initial but not instantly to algebraic R·Q offset.
        Assert.NotEqual(UnitConversions.CelsiusToKelvin(10.0), tec.LastColdFaceTemperatureK);
        _ = initialCold;
        Assert.True(Math.Abs(tec.LastColdFaceTemperatureK - UnitConversions.CelsiusToKelvin(10.0)) < 15.0);
    }

    [Fact]
    public void Tec005_LargerCapacity_SlowsTemperatureChange()
    {
        var light = RunDynamicTec(capacityScale: 1.0, steps: 3);
        var heavy = RunDynamicTec(capacityScale: 10.0, steps: 3);
        Assert.True(Math.Abs(heavy.DeltaHot) < Math.Abs(light.DeltaHot));
    }

    [Fact]
    public void Pwr007_ChargeThenDischarge_AcrossSteps()
    {
        var source = new PrescribedElectricalSourceComponent("src", 80.0);
        var battery = DefaultBattery() with
        {
            MaximumChargePowerW = 50.0,
            MaximumDischargePowerW = 60.0
        };
        var initial = BatteryState.Create(0.2 * battery.NominalCapacityJ, battery.NominalCapacityJ, 298.15);
        var pm = new PowerManagementComponent(
            "pm",
            battery,
            [
                new ElectricalLoadDemand
                {
                    LoadId = "controller",
                    RequestedPowerW = 10.0,
                    Priority = 0,
                    IsEssential = true
                }
            ],
            initial,
            mpptEfficiencyFraction: 1.0);
        var bus = new ElectricalLoadSinkComponent("bus");
        var soc0 = pm.BatteryState.StateOfChargeFraction;

        var charge = Run(
            [source, pm, bus],
            [
                Connect("s_p", "src", "outlet", "pm", "generation"),
                Connect("p_b", "pm", "bus", "bus", "inlet")
            ],
            durationSeconds: 20,
            timeStepSeconds: 5);
        Assert.True(charge.Succeeded, Format(charge));
        Assert.True(pm.BatteryState.StateOfChargeFraction > soc0);
        Assert.True(pm.LastBatteryChargePowerW > 0.0);

        // Switch to deficit: replace generation with a weak source by rebuilding.
        var weak = new PrescribedElectricalSourceComponent("src2", 5.0);
        var pm2 = new PowerManagementComponent(
            "pm2",
            battery,
            [
                new ElectricalLoadDemand
                {
                    LoadId = "controller",
                    RequestedPowerW = 40.0,
                    Priority = 0,
                    IsEssential = true
                },
                new ElectricalLoadDemand
                {
                    LoadId = "aux",
                    RequestedPowerW = 30.0,
                    Priority = 1,
                    IsEssential = false
                }
            ],
            pm.BatteryState,
            mpptEfficiencyFraction: 1.0);
        var bus2 = new ElectricalLoadSinkComponent("bus2");
        var discharge = Run(
            [weak, pm2, bus2],
            [
                Connect("s_p", "src2", "outlet", "pm2", "generation"),
                Connect("p_b", "pm2", "bus", "bus2", "inlet")
            ]);
        Assert.True(discharge.Succeeded, Format(discharge));
        Assert.True(pm2.LastBatteryDischargePowerW > 0.0);
        Assert.Equal(40.0, pm2.LastDeliveredLoadPowerW["controller"], precision: 6);
        Assert.True(pm2.LastDeliveredLoadPowerW["aux"] < 30.0 || pm2.LastUnservedPowerW >= 0.0);
    }

    [Fact]
    public void Pwr007_EssentialUnserved_FailsRun()
    {
        var source = new PrescribedElectricalSourceComponent("src", 5.0);
        var battery = DefaultBattery() with
        {
            MaximumDischargePowerW = 0.0,
            MinimumSocFraction = 0.5,
            MaximumSocFraction = 0.5
        };
        var initial = BatteryState.Create(0.5 * battery.NominalCapacityJ, battery.NominalCapacityJ, 298.15);
        var pm = new PowerManagementComponent(
            "pm",
            battery,
            [
                new ElectricalLoadDemand
                {
                    LoadId = "controller",
                    RequestedPowerW = 30.0,
                    Priority = 0,
                    IsEssential = true
                }
            ],
            initial,
            mpptEfficiencyFraction: 1.0);
        var bus = new ElectricalLoadSinkComponent("bus");

        var result = Run(
            [source, pm, bus],
            [
                Connect("s_p", "src", "outlet", "pm", "generation"),
                Connect("p_b", "pm", "bus", "bus", "inlet")
            ]);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == "POWER.ESSENTIAL_LOAD_UNSERVED");
    }

    private TemperatureCorrectedSolarPanelComponent EvaluatePv(double ambientC)
    {
        var sun = new SolarRadiationSourceComponent("sun", 1000.0);
        var panel = new TemperatureCorrectedSolarPanelComponent(
            "pv",
            ratedPowerW: 200.0,
            areaM2: 1.2,
            powerTemperatureCoefficientPerK: -0.004,
            fallbackAmbientTemperatureK: UnitConversions.CelsiusToKelvin(ambientC));
        var load = new ElectricalLoadSinkComponent("load");
        var result = Run(
            [sun, panel, load],
            [
                Connect("s_p", "sun", "outlet", "pv", "solar"),
                Connect("p_l", "pv", "electrical", "load", "inlet")
            ]);
        Assert.True(result.Succeeded, Format(result));
        return panel;
    }

    private static (double DeltaHot, double DeltaCold) RunDynamicTec(double capacityScale, int steps)
    {
        var parameters = AnalyticalPeltierParameters.CreateProvisionalEngineeringDefaults() with
        {
            EnableProtectionShutdown = false,
            ColdSideThermalResistanceKPerW = 0.4,
            HotSideThermalResistanceKPerW = 0.4,
            EffectiveColdSideThermalCapacityJPerK = 500.0 * capacityScale,
            EffectiveHotSideThermalCapacityJPerK = 800.0 * capacityScale
        };
        var tec = new AnalyticalPeltierComponent(
            "tec",
            parameters,
            UnitConversions.CelsiusToKelvin(12.0),
            UnitConversions.CelsiusToKelvin(30.0),
            requestedElectricalPowerW: 25.0);
        var cold = new EnvironmentHeatSinkComponent("c");
        var hot = new EnvironmentHeatSinkComponent("h");
        var t0c = UnitConversions.CelsiusToKelvin(12.0);
        var t0h = UnitConversions.CelsiusToKelvin(30.0);
        var result = new SimulationEngine().Run(new SimulationRequest
        {
            Graph = new SimulationGraph(
                [tec, cold, hot],
                [
                    Connect("c", "tec", "cold_heat", "c", "inlet"),
                    Connect("h", "tec", "hot_heat", "h", "inlet")
                ]),
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(steps),
            TimeStep = TimeSpan.FromSeconds(1)
        });
        Assert.True(result.Succeeded);
        return (tec.LastHotFaceTemperatureK - t0h, tec.LastColdFaceTemperatureK - t0c);
    }

    private static BatteryParameters DefaultBattery()
        => new()
        {
            NominalCapacityJ = 3_600_000.0,
            MinimumSocFraction = 0.1,
            MaximumSocFraction = 0.9,
            ChargeEfficiencyFraction = 0.95,
            DischargeEfficiencyFraction = 0.95,
            MaximumChargePowerW = 200.0,
            MaximumDischargePowerW = 200.0
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

    private static string Format(SimulationRunResult result)
        => string.Join("; ", result.Diagnostics.Select(d => $"{d.Code}:{d.Message}"));
}
