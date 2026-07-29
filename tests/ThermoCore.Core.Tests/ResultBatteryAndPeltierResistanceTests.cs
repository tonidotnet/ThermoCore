using ThermoCore.Core.Components;
using ThermoCore.Core.Components.Power;
using ThermoCore.Core.Components.Thermoelectric;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Results;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

public class ResultBatteryAndPeltierResistanceTests
{
    private readonly PsychrometricCalculator _calculator = new();

    [Fact]
    public void ResultCollector_ExtractsChannelsFromSuccessfulRun()
    {
        var inlet = SampleAir(25, 0.5, 0.02);
        var source = new AmbientAirSourceComponent("air", inlet);
        var heater = new SensibleHeaterComponent("heater", heatRateW: 200.0, calculator: _calculator);
        var sink = new ExhaustAirSinkComponent("sink");

        var request = new SimulationRequest
        {
            Graph = new SimulationGraph(
                [source, heater, sink],
                [
                    new PhysicalConnection
                    {
                        Id = "a_h",
                        SourceComponentId = "air",
                        SourcePortId = "outlet",
                        TargetComponentId = "heater",
                        TargetPortId = "inlet"
                    },
                    new PhysicalConnection
                    {
                        Id = "h_s",
                        SourceComponentId = "heater",
                        SourcePortId = "outlet",
                        TargetComponentId = "sink",
                        TargetPortId = "inlet"
                    }
                ]),
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(3),
            TimeStep = TimeSpan.FromSeconds(1)
        };

        var run = new SimulationEngine().Run(request);
        Assert.True(run.Succeeded);

        var result = SimulationResultCollector.Collect(run, request);
        Assert.Equal(SimulationRunStatus.Completed, result.Status);
        Assert.Equal(3, result.Metadata.CapturedStepCount);
        Assert.Contains(result.Channels, c => c.Definition.Id == "heater.outlet.temperature");
        Assert.Contains(result.Channels, c => c.Definition.Id == "balance.energy.residual");
        var temperature = result.Channels.Single(c => c.Definition.Id == "heater.outlet.temperature");
        Assert.Equal(3, temperature.Values.Count);
        Assert.True(temperature.Values[0] > inlet.TemperatureK);
    }

    [Fact]
    public void ResultCollector_FixedInterval_Downsamples()
    {
        var inlet = SampleAir(20, 0.4, 0.01);
        var source = new AmbientAirSourceComponent("air", inlet);
        var sink = new ExhaustAirSinkComponent("sink");
        var request = new SimulationRequest
        {
            Graph = new SimulationGraph(
                [source, sink],
                [new PhysicalConnection
                {
                    Id = "a_s",
                    SourceComponentId = "air",
                    SourcePortId = "outlet",
                    TargetComponentId = "sink",
                    TargetPortId = "inlet"
                }]),
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(5),
            TimeStep = TimeSpan.FromSeconds(1)
        };

        var run = new SimulationEngine().Run(request);
        var result = SimulationResultCollector.Collect(
            run,
            request,
            ResultCapturePolicy.FixedInterval,
            fixedIntervalSteps: 2);

        Assert.Equal(ResultCapturePolicy.FixedInterval, result.Metadata.CapturePolicy);
        Assert.True(result.Metadata.CapturedStepCount < result.Metadata.TotalStepCount);
        Assert.Equal(3, result.Metadata.CapturedStepCount); // steps 0,2,4
    }

    [Fact]
    public void ResultCollector_SummaryOnly_OmitsChannels()
    {
        var inlet = SampleAir(20, 0.4, 0.01);
        var source = new AmbientAirSourceComponent("air", inlet);
        var sink = new ExhaustAirSinkComponent("sink");
        var request = new SimulationRequest
        {
            Graph = new SimulationGraph(
                [source, sink],
                [new PhysicalConnection
                {
                    Id = "a_s",
                    SourceComponentId = "air",
                    SourcePortId = "outlet",
                    TargetComponentId = "sink",
                    TargetPortId = "inlet"
                }]),
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(2),
            TimeStep = TimeSpan.FromSeconds(1)
        };

        var run = new SimulationEngine().Run(request);
        var result = SimulationResultCollector.Collect(run, request, ResultCapturePolicy.SummaryOnly);
        Assert.Empty(result.Channels);
        Assert.Equal(0, result.Metadata.CapturedStepCount);
        Assert.True(result.Summary.Succeeded);
    }

    [Fact]
    public void Battery_ChargeIncreasesSoc()
    {
        var parameters = DefaultBattery();
        var initial = BatteryState.Create(
            storedEnergyJ: 0.4 * parameters.NominalCapacityJ,
            nominalCapacityJ: parameters.NominalCapacityJ,
            batteryTemperatureK: 298.15);
        var battery = new BatteryStorageComponent("bat", parameters, initial);

        var result = EvaluateBattery(battery, chargeW: 100.0, dischargeRequestW: 0.0, dtSeconds: 10);
        battery.Commit(result);

        Assert.True(battery.State.StateOfChargeFraction > initial.StateOfChargeFraction);
        Assert.Equal(100.0, battery.LastChargePowerW, precision: 8);
        Assert.Equal(0.0, result.Balance.ElectricalEnergyResidualJ, precision: 6);
    }

    [Fact]
    public void Battery_DischargeDecreasesSoc()
    {
        var parameters = DefaultBattery();
        var initial = BatteryState.Create(
            storedEnergyJ: 0.6 * parameters.NominalCapacityJ,
            nominalCapacityJ: parameters.NominalCapacityJ,
            batteryTemperatureK: 298.15);
        var battery = new BatteryStorageComponent("bat", parameters, initial);

        var result = EvaluateBattery(battery, chargeW: 0.0, dischargeRequestW: 80.0, dtSeconds: 10);
        battery.Commit(result);

        Assert.True(battery.State.StateOfChargeFraction < initial.StateOfChargeFraction);
        Assert.Equal(80.0, battery.LastDischargePowerW, precision: 8);
        var delivered = Assert.IsType<ElectricalPowerState>(result.OutputStates["discharge"]);
        Assert.Equal(80.0, delivered.PowerW, precision: 8);
    }

    [Fact]
    public void Battery_RespectsMaximumSoc()
    {
        var parameters = DefaultBattery() with { MaximumSocFraction = 0.8, MaximumChargePowerW = 500.0 };
        var initial = BatteryState.Create(
            storedEnergyJ: 0.799 * parameters.NominalCapacityJ,
            nominalCapacityJ: parameters.NominalCapacityJ,
            batteryTemperatureK: 298.15);
        var battery = new BatteryStorageComponent("bat", parameters, initial);

        var result = EvaluateBattery(battery, chargeW: 500.0, dischargeRequestW: 0.0, dtSeconds: 60);
        battery.Commit(result);

        Assert.True(battery.State.StateOfChargeFraction <= parameters.MaximumSocFraction + 1e-12);
        Assert.Contains(result.Diagnostics, d => d.Code == "BATTERY.AT_MAXIMUM_SOC");
        Assert.True(battery.LastRejectedChargePowerW > 0.0);
    }

    [Fact]
    public void Battery_RespectsMinimumSoc()
    {
        var parameters = DefaultBattery() with { MinimumSocFraction = 0.2, MaximumDischargePowerW = 500.0 };
        var initial = BatteryState.Create(
            storedEnergyJ: 0.205 * parameters.NominalCapacityJ,
            nominalCapacityJ: parameters.NominalCapacityJ,
            batteryTemperatureK: 298.15);
        var battery = new BatteryStorageComponent("bat", parameters, initial);

        var result = EvaluateBattery(battery, chargeW: 0.0, dischargeRequestW: 500.0, dtSeconds: 60);
        battery.Commit(result);

        Assert.True(battery.State.StateOfChargeFraction >= parameters.MinimumSocFraction - 1e-12);
        Assert.Contains(result.Diagnostics, d => d.Code == "BATTERY.AT_MINIMUM_SOC");
        Assert.True(battery.LastUnservedDischargePowerW > 0.0);
    }

    [Fact]
    public void Peltier_ExternalHotResistance_RaisesHotFaceTemperature()
    {
        var baseParams = AnalyticalPeltierParameters.CreateProvisionalEngineeringDefaults();
        var withResistance = baseParams with { HotSideThermalResistanceKPerW = 0.5 };
        var loadK = UnitConversions.CelsiusToKelvin(10.0);
        var sinkK = UnitConversions.CelsiusToKelvin(30.0);

        var direct = new AnalyticalPeltierComponent("tec0", baseParams, loadK, sinkK, requestedElectricalPowerW: 20.0);
        var resisted = new AnalyticalPeltierComponent("tec1", withResistance, loadK, sinkK, requestedElectricalPowerW: 20.0);

        EvaluatePeltier(direct);
        EvaluatePeltier(resisted);

        Assert.Equal(sinkK, direct.LastHotFaceTemperatureK, precision: 8);
        Assert.True(resisted.LastHotFaceTemperatureK > sinkK);
        Assert.True(resisted.LastColdSideHeatW < direct.LastColdSideHeatW);
        Assert.Equal(0.0, resisted.EvaluateBalanceResidual(), precision: 6);
    }

    [Fact]
    public void Peltier_ExternalColdResistance_LowersColdFaceTemperature()
    {
        var parameters = AnalyticalPeltierParameters.CreateProvisionalEngineeringDefaults() with
        {
            ColdSideThermalResistanceKPerW = 0.4
        };
        var loadK = UnitConversions.CelsiusToKelvin(15.0);
        var sinkK = UnitConversions.CelsiusToKelvin(35.0);
        var peltier = new AnalyticalPeltierComponent("tec", parameters, loadK, sinkK, requestedElectricalPowerW: 18.0);

        var result = EvaluatePeltier(peltier);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error);
        Assert.True(peltier.LastColdFaceTemperatureK < loadK);
        Assert.Equal(loadK, peltier.LastLoadTemperatureK, precision: 12);
        Assert.Equal(
            loadK - peltier.LastColdSideHeatW * parameters.ColdSideThermalResistanceKPerW,
            peltier.LastColdFaceTemperatureK,
            precision: 4);
    }

    private static BatteryParameters DefaultBattery()
        => new()
        {
            NominalCapacityJ = 3_600_000.0, // 1 kWh
            MinimumSocFraction = 0.1,
            MaximumSocFraction = 0.9,
            ChargeEfficiencyFraction = 0.95,
            DischargeEfficiencyFraction = 0.95,
            MaximumChargePowerW = 200.0,
            MaximumDischargePowerW = 200.0,
            SelfDischargePowerW = 0.0
        };

    private MoistAirState SampleAir(double temperatureC, double relativeHumidity, double dryAirMassFlow)
        => _calculator.CreateFromRelativeHumidity(
            UnitConversions.CelsiusToKelvin(temperatureC),
            PhysicalConstants.StandardAtmosphericPressurePa,
            relativeHumidity,
            dryAirMassFlow);

    private static ComponentStepResult EvaluateBattery(
        BatteryStorageComponent battery,
        double chargeW,
        double dischargeRequestW,
        double dtSeconds)
    {
        var context = new ComponentStepContext
        {
            Simulation = new SimulationContext
            {
                SimulationStart = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                TimeStep = TimeSpan.FromSeconds(dtSeconds),
                ElapsedTime = TimeSpan.Zero,
                StepIndex = 0
            },
            InputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["charge"] = new ElectricalPowerState { PowerW = chargeW },
                ["discharge_request"] = new ElectricalPowerState { PowerW = dischargeRequestW }
            }
        };

        battery.Initialize(context.Simulation);
        return battery.Evaluate(context);
    }

    private static ComponentStepResult EvaluatePeltier(AnalyticalPeltierComponent peltier)
    {
        var context = new ComponentStepContext
        {
            Simulation = new SimulationContext
            {
                SimulationStart = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                TimeStep = TimeSpan.FromSeconds(1),
                ElapsedTime = TimeSpan.Zero,
                StepIndex = 0
            }
        };

        peltier.Initialize(context.Simulation);
        var result = peltier.Evaluate(context);
        peltier.Commit(result);
        return result;
    }
}

internal static class AnalyticalPeltierTestExtensions
{
    public static double EvaluateBalanceResidual(this AnalyticalPeltierComponent peltier)
    {
        var context = new ComponentStepContext
        {
            Simulation = new SimulationContext
            {
                SimulationStart = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                TimeStep = TimeSpan.FromSeconds(1),
                ElapsedTime = TimeSpan.Zero,
                StepIndex = 0
            }
        };

        return peltier.Evaluate(context).Balance.EnergyResidualJ;
    }
}
