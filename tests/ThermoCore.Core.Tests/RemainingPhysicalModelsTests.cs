using ThermoCore.Core.Components;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

/// <summary>
/// Remaining Core physical models: SC-004..007, PV-003..006, COND-005, HR-004..006, AIR-004..008.
/// </summary>
public class RemainingPhysicalModelsTests
{
    private readonly PsychrometricCalculator _calculator = new();

    [Fact]
    public void Sc004_WindIncreasesEnvironmentalLoss()
    {
        var calm = RunCollector(windSpeed: 0.0, windCoeff: 2.0);
        var windy = RunCollector(windSpeed: 5.0, windCoeff: 2.0);
        Assert.True(windy.LastEffectiveLossCoefficientWPerM2K > calm.LastEffectiveLossCoefficientWPerM2K);
        Assert.True(windy.AbsorberTemperatureK < calm.AbsorberTemperatureK);
    }

    [Fact]
    public void Sc005_ZeroFlow_StagnationAndOvertemperature()
    {
        var inlet = _calculator.CreateFromRelativeHumidity(
            UnitConversions.CelsiusToKelvin(25.0),
            PhysicalConstants.StandardAtmosphericPressurePa,
            0.4,
            dryAirMassFlowKgPerSecond: 0.0);
        var ambientK = UnitConversions.CelsiusToKelvin(25.0);
        var source = new AmbientAirSourceComponent("air", inlet);
        var sun = new SolarRadiationSourceComponent("sun", 1000.0);
        var collector = new DynamicLumpedSolarCollectorComponent(
            "collector",
            opticalEfficiencyFraction: 0.75,
            apertureAreaM2: 1.0,
            effectiveThermalCapacityJPerK: 4_000.0,
            absorberToAirUaWPerK: 40.0,
            overallLossCoefficientWPerM2K: 4.0,
            initialAbsorberTemperatureK: ambientK,
            ambientTemperatureK: ambientK,
            maximumAllowedAbsorberTemperatureK: UnitConversions.CelsiusToKelvin(45.0),
            calculator: _calculator);
        var sink = new ExhaustAirSinkComponent("sink");

        var result = Run(
            [source, sun, collector, sink],
            [
                Connect("a", "air", "outlet", "collector", "inlet"),
                Connect("s", "sun", "outlet", "collector", "solar"),
                Connect("k", "collector", "outlet", "sink", "inlet")
            ],
            durationSeconds: 120,
            timeStepSeconds: 10);

        Assert.True(result.Succeeded, Format(result));
        Assert.Contains(result.Diagnostics, d => d.Code == "COLLECTOR.STAGNATION");
        Assert.Equal(0.0, collector.LastUsefulHeatW, precision: 12);
        Assert.True(collector.AbsorberTemperatureK > ambientK);
        Assert.Contains(result.Diagnostics, d => d.Code == "COLLECTOR.OVERTEMPERATURE");
    }

    [Fact]
    public void Sc006_PressureDrop_ScalesWithFlowSquared()
    {
        var low = RunCollector(flow: 0.01, dPref: 50.0, vRef: 0.01);
        var high = RunCollector(flow: 0.02, dPref: 50.0, vRef: 0.01);
        Assert.True(low.LastPressureDropPa > 0.0);
        Assert.Equal(4.0, high.LastPressureDropPa / low.LastPressureDropPa, precision: 2);
    }

    [Fact]
    public void Pv003_DynamicCellTemperature_RisesWithIrradiance()
    {
        var panel = CreateDynamicPv();
        var sun = new SolarRadiationSourceComponent("sun", 900.0);
        var load = new ElectricalLoadSinkComponent("load");
        var t0 = panel.CellTemperatureK;
        var result = Run(
            [sun, panel, load],
            [
                Connect("s", "sun", "outlet", "pv", "solar"),
                Connect("p", "pv", "electrical", "load", "inlet")
            ],
            durationSeconds: 30,
            timeStepSeconds: 5);
        Assert.True(result.Succeeded, Format(result));
        Assert.True(panel.CellTemperatureK > t0);
        Assert.True(panel.LastElectricalPowerW > 0.0);
        Assert.True(panel.LastAbsorbedSolarPowerW > panel.LastElectricalPowerW);
    }

    [Fact]
    public void Pv004_RearAirCooling_LowersCellTemperature()
    {
        var without = CreateDynamicPv(rearUa: 0.0);
        var with = CreateDynamicPv(rearUa: 50.0);
        var inlet = SampleAir(25, 0.4, 0.03);
        RunPvWithRear(without, inlet, connectRear: false);
        RunPvWithRear(with, inlet, connectRear: true);
        Assert.True(with.CellTemperatureK < without.CellTemperatureK);
        Assert.True(with.LastRearAirHeatW > 0.0);
    }

    [Fact]
    public void Pv005_RearAirPressureDrop_IsPositive()
    {
        var panel = CreateDynamicPv(rearUa: 40.0, dPref: 20.0, vRef: 0.03);
        var inlet = SampleAir(25, 0.4, 0.03);
        RunPvWithRear(panel, inlet, connectRear: true);
        Assert.True(panel.LastRearAirPressureDropPa > 0.0);
    }

    [Fact]
    public void Cond005_UaEffectiveness_IncreasesApproachToSurface()
    {
        var inlet = SampleAir(32, 0.85, 0.02);
        var weak = EvaluateCondenser(inlet, ua: 5.0);
        var strong = EvaluateCondenser(inlet, ua: 200.0);
        Assert.True(strong.LastCondensedWaterRateKgPerSecond >= weak.LastCondensedWaterRateKgPerSecond - 1e-12);
        Assert.True(strong.LastTotalCoolingPowerW >= weak.LastTotalCoolingPowerW - 1e-6);
    }

    [Fact]
    public void Hr004_IndependentSidePressureDrops()
    {
        var hx = new SensibleHeatRecoveryComponent(
            "hr",
            effectivenessFraction: 0.6,
            hotReferencePressureDropPa: 40.0,
            coldReferencePressureDropPa: 10.0,
            hotReferenceVolumetricFlowM3PerSecond: 0.02,
            coldReferenceVolumetricFlowM3PerSecond: 0.02,
            calculator: _calculator);
        var hot = SampleAir(40, 0.3, 0.02);
        var cold = SampleAir(20, 0.5, 0.02);
        var ctx = new ComponentStepContext
        {
            Simulation = new SimulationContext
            {
                SimulationStart = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                TimeStep = TimeSpan.FromSeconds(1),
                ElapsedTime = TimeSpan.Zero,
                StepIndex = 0
            },
            InputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["hot_in"] = hot,
                ["cold_in"] = cold
            }
        };
        hx.Initialize(ctx.Simulation);
        var result = hx.Evaluate(ctx);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error);
        Assert.True(hx.LastHotPressureDropPa > hx.LastColdPressureDropPa);
        var hotOut = Assert.IsType<MoistAirState>(result.OutputStates["hot_out"]);
        Assert.True(hotOut.PressurePa < hot.PressurePa);
    }

    [Fact]
    public void Hr006_CounterFlowNtu_WithBypassAndDeltaP_InGraph()
    {
        var hotSrc = new AmbientAirSourceComponent("hot", SampleAir(45, 0.25, 0.025));
        var coldSrc = new AmbientAirSourceComponent("cold", SampleAir(18, 0.55, 0.02));
        var hx = SensibleHeatRecoveryComponent.CreateCounterFlowNtu(
            "hr",
            uaWPerK: 80.0,
            bypassFraction: 0.2,
            hotReferencePressureDropPa: 15.0,
            coldReferencePressureDropPa: 12.0,
            calculator: _calculator);
        var hotSink = new ExhaustAirSinkComponent("hotsink");
        var coldSink = new ExhaustAirSinkComponent("coldsink");
        var result = Run(
            [hotSrc, coldSrc, hx, hotSink, coldSink],
            [
                Connect("h_i", "hot", "outlet", "hr", "hot_in"),
                Connect("c_i", "cold", "outlet", "hr", "cold_in"),
                Connect("h_o", "hr", "hot_out", "hotsink", "inlet"),
                Connect("c_o", "hr", "cold_out", "coldsink", "inlet")
            ]);
        Assert.True(result.Succeeded, Format(result));
        Assert.True(hx.LastRecoveredHeatW > 0.0);
        Assert.True(hx.LastHotPressureDropPa > 0.0);
    }

    [Fact]
    public void Air004_FanCurve_ShutoffAndFreeDelivery()
    {
        var fan = new CurveBasedFanComponent(
            "fan",
            shutoffPressureRisePa: 200.0,
            linearCoefficientPaPerM3s: -500.0,
            quadraticCoefficientPaPerM3s2: -2000.0,
            controlFraction: 1.0);
        Assert.Equal(200.0, fan.FanPressureRisePa(0.0), precision: 8);
        Assert.True(fan.FanPressureRisePa(0.05) < 200.0);
    }

    [Fact]
    public void Air006_OperatingPoint_IntersectsSystemCurve()
    {
        double Fan(double v) => 150.0 - 3000.0 * v * v;
        double Sys(double v) => 50.0 + 2000.0 * v * v;
        Assert.True(FanSystemOperatingPointSolver.TrySolve(Fan, Sys, 0.05, out var v, out var dp));
        Assert.True(v > 0.0);
        Assert.Equal(Fan(v), Sys(v), precision: 3);
        Assert.Equal(dp, Sys(v), precision: 6);
    }

    [Fact]
    public void Air006_NoIntersection_ReturnsFalse()
    {
        double Fan(double v) => 10.0 - 100.0 * v;
        double Sys(double v) => 50.0 + 10.0 * v;
        Assert.False(FanSystemOperatingPointSolver.TrySolve(Fan, Sys, 0.01, out _, out _));
    }

    [Fact]
    public void Air007_008_TwoFans_MixInGraph()
    {
        var a = SampleAir(20, 0.4, 0.01);
        var b = SampleAir(20, 0.4, 0.015);
        var srcA = new AmbientAirSourceComponent("a", a);
        var srcB = new AmbientAirSourceComponent("b", b);
        var fanA = new PrescribedFlowFanComponent("fa", 0.01, pressureRisePa: 50.0, calculator: _calculator);
        var fanB = new PrescribedFlowFanComponent("fb", 0.015, pressureRisePa: 50.0, calculator: _calculator);
        var mixer = new MoistAirMixerComponent("mix", ["inlet_a", "inlet_b"], _calculator);
        var sink = new ExhaustAirSinkComponent("sink");
        var result = Run(
            [srcA, srcB, fanA, fanB, mixer, sink],
            [
                Connect("a_f", "a", "outlet", "fa", "inlet"),
                Connect("b_f", "b", "outlet", "fb", "inlet"),
                Connect("fa_m", "fa", "outlet", "mix", "inlet_a"),
                Connect("fb_m", "fb", "outlet", "mix", "inlet_b"),
                Connect("m_s", "mix", "outlet", "sink", "inlet")
            ]);
        Assert.True(result.Succeeded, Format(result));
        var mixed = Assert.IsType<MoistAirState>(result.Steps[0].PortStates["mix.outlet"]);
        Assert.Equal(0.025, mixed.DryAirMassFlowKgPerSecond, precision: 8);
    }

    [Fact]
    public void Air005_SeriesDucts_AccumulatePressureDrop()
    {
        var inlet = SampleAir(22, 0.45, 0.02);
        var src = new AmbientAirSourceComponent("air", inlet);
        var d1 = new DuctPressureLossComponent("d1", 30.0, 0.02, calculator: _calculator);
        var d2 = new DuctPressureLossComponent("d2", 20.0, 0.02, calculator: _calculator);
        var sink = new ExhaustAirSinkComponent("sink");
        var result = Run(
            [src, d1, d2, sink],
            [
                Connect("a", "air", "outlet", "d1", "inlet"),
                Connect("12", "d1", "outlet", "d2", "inlet"),
                Connect("2s", "d2", "outlet", "sink", "inlet")
            ]);
        Assert.True(result.Succeeded, Format(result));
        var outState = Assert.IsType<MoistAirState>(result.Steps[0].PortStates["d2.outlet"]);
        Assert.True(d1.LastPressureDropPa > 0.0);
        Assert.True(d2.LastPressureDropPa > 0.0);
        Assert.True(outState.PressurePa < inlet.PressurePa - 20.0);
    }

    private DynamicLumpedSolarCollectorComponent RunCollector(
        double windSpeed = 0.0,
        double windCoeff = 0.0,
        double flow = 0.02,
        double dPref = 0.0,
        double vRef = 0.01)
    {
        var ambientK = UnitConversions.CelsiusToKelvin(20.0);
        var inlet = SampleAir(20, 0.5, flow);
        var source = new AmbientAirSourceComponent("air", inlet);
        var sun = new SolarRadiationSourceComponent("sun", 800.0);
        var collector = new DynamicLumpedSolarCollectorComponent(
            "collector",
            opticalEfficiencyFraction: 0.7,
            apertureAreaM2: 1.0,
            effectiveThermalCapacityJPerK: 6_000.0,
            absorberToAirUaWPerK: 35.0,
            overallLossCoefficientWPerM2K: 5.0,
            initialAbsorberTemperatureK: ambientK,
            ambientTemperatureK: ambientK,
            windSpeedMPerSecond: windSpeed,
            windLossCoefficientWPerM2KPerMps: windCoeff,
            referencePressureDropPa: dPref,
            referenceVolumetricFlowM3PerSecond: vRef,
            calculator: _calculator);
        var sink = new ExhaustAirSinkComponent("sink");
        var result = Run(
            [source, sun, collector, sink],
            [
                Connect("a", "air", "outlet", "collector", "inlet"),
                Connect("s", "sun", "outlet", "collector", "solar"),
                Connect("k", "collector", "outlet", "sink", "inlet")
            ],
            durationSeconds: 20,
            timeStepSeconds: 5);
        Assert.True(result.Succeeded, Format(result));
        return collector;
    }

    private static DynamicElectrothermalSolarPanelComponent CreateDynamicPv(
        double rearUa = 0.0,
        double dPref = 0.0,
        double vRef = 0.01)
        => new(
            "pv",
            ratedPowerW: 250.0,
            areaM2: 1.5,
            effectiveThermalCapacityJPerK: 8_000.0,
            opticalAbsorptanceFraction: 0.9,
            environmentalLossUaWPerK: 12.0,
            initialCellTemperatureK: UnitConversions.CelsiusToKelvin(25.0),
            ambientTemperatureK: UnitConversions.CelsiusToKelvin(25.0),
            rearAirUaWPerK: rearUa,
            referencePressureDropPa: dPref,
            referenceVolumetricFlowM3PerSecond: vRef);

    private void RunPvWithRear(DynamicElectrothermalSolarPanelComponent panel, MoistAirState rearInlet, bool connectRear)
    {
        var sun = new SolarRadiationSourceComponent("sun", 1000.0);
        var load = new ElectricalLoadSinkComponent("load");
        var components = new List<ISimulationComponent> { sun, panel, load };
        var connections = new List<PhysicalConnection>
        {
            Connect("s", "sun", "outlet", "pv", "solar"),
            Connect("p", "pv", "electrical", "load", "inlet")
        };
        if (connectRear)
        {
            var air = new AmbientAirSourceComponent("rear", rearInlet);
            var sink = new ExhaustAirSinkComponent("rearsink");
            components.Add(air);
            components.Add(sink);
            connections.Add(Connect("ri", "rear", "outlet", "pv", "rear_air_in"));
            connections.Add(Connect("ro", "pv", "rear_air_out", "rearsink", "inlet"));
        }

        var result = Run(components, connections, durationSeconds: 25, timeStepSeconds: 5);
        Assert.True(result.Succeeded, Format(result));
    }

    private CondenserComponent EvaluateCondenser(MoistAirState inlet, double ua)
    {
        var condenser = new CondenserComponent(
            "cond",
            bypassFactor: 0.2,
            drainageEfficiency: 1.0,
            fallbackSurfaceTemperatureK: UnitConversions.CelsiusToKelvin(8.0),
            fallbackAvailableCoolingPowerW: 5000.0,
            heatTransferUaWPerK: ua,
            calculator: _calculator);
        var ctx = new ComponentStepContext
        {
            Simulation = new SimulationContext
            {
                SimulationStart = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                TimeStep = TimeSpan.FromSeconds(1),
                ElapsedTime = TimeSpan.Zero,
                StepIndex = 0
            },
            InputStates = new Dictionary<string, object?>(StringComparer.Ordinal) { ["inlet"] = inlet }
        };
        condenser.Initialize(ctx.Simulation);
        _ = condenser.Evaluate(ctx);
        return condenser;
    }

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
