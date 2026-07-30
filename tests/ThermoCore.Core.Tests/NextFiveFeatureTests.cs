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

public class NextFiveFeatureTests
{
    private readonly PsychrometricCalculator _calculator = new();

    [Fact]
    public void SilicaGel_ReferencePressureDrop_ScalesWithFlowSquared()
    {
        var parameters = BaseSilica() with
        {
            ReferencePressureDropPa = 100.0,
            ReferenceVolumetricFlowM3PerSecond = 0.01,
            PressureDropFlowExponent = 2.0,
            EnableEnergyLimitedDesorption = false
        };
        var isotherm = GenericPolynomialIsotherm.CreateLinear(parameters.MaximumWaterLoadingKgPerKgDryAdsorbent);
        var bed = new SilicaGelBedComponent("sg", parameters, isotherm, calculator: _calculator);
        var inlet = SampleAir(25, 0.5, 0.02);
        var result = EvaluateSilica(bed, inlet);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error);
        Assert.True(bed.LastPressureDropPa > 0.0);
        var outlet = Assert.IsType<MoistAirState>(result.OutputStates["outlet"]);
        Assert.True(outlet.PressurePa < inlet.PressurePa);
        Assert.Equal(inlet.PressurePa - bed.LastPressureDropPa, outlet.PressurePa, precision: 6);
    }

    [Fact]
    public void SilicaGel_ErgunPressureDrop_IsPositive()
    {
        var parameters = BaseSilica() with
        {
            EnableErgunPressureDrop = true,
            BedCrossSectionAreaM2 = 0.05,
            BedLengthM = 0.3,
            ParticleDiameterM = 0.003,
            BedVoidFraction = 0.4,
            EnableEnergyLimitedDesorption = false
        };
        var isotherm = GenericPolynomialIsotherm.CreateLinear(parameters.MaximumWaterLoadingKgPerKgDryAdsorbent);
        var bed = new SilicaGelBedComponent("sg", parameters, isotherm, calculator: _calculator);
        var result = EvaluateSilica(bed, SampleAir(25, 0.5, 0.03));
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error);
        Assert.True(bed.LastPressureDropPa > 0.0);
    }

    [Fact]
    public void Condenser_FilmCapacityOverflow_ForcesDrainage()
    {
        var inlet = SampleAir(30, 0.85, 0.03);
        var condenser = new CondenserComponent(
            id: "cond",
            bypassFactor: 0.05,
            drainageEfficiency: 0.5,
            fallbackSurfaceTemperatureK: UnitConversions.CelsiusToKelvin(5.0),
            fallbackAvailableCoolingPowerW: 5000.0,
            maximumRetainedFilmKg: 1e-9,
            calculator: _calculator);

        var result = EvaluateCondenser(condenser, inlet);
        condenser.Commit(result);

        Assert.True(condenser.LastRetainedFilmKg <= 1e-9 + 1e-18);
        Assert.Contains(result.Diagnostics, d => d.Code == "CONDENSER.FILM_CAPACITY_OVERFLOW");
        Assert.True(condenser.LastEffectiveDrainageEfficiency >= 0.5);
        Assert.Equal(0.0, result.Balance.WaterMassResidualKg, precision: 8);
    }

    [Fact]
    public void Condenser_ReportsUncollectedAndRetainedFilm()
    {
        var inlet = SampleAir(28, 0.8, 0.02);
        var condenser = new CondenserComponent(
            "cond",
            bypassFactor: 0.1,
            drainageEfficiency: 0.7,
            fallbackSurfaceTemperatureK: UnitConversions.CelsiusToKelvin(8.0),
            fallbackAvailableCoolingPowerW: 3000.0,
            maximumRetainedFilmKg: 0.1,
            calculator: _calculator);

        var result = EvaluateCondenser(condenser, inlet);
        condenser.Commit(result);
        Assert.True(condenser.LastCondensedWaterRateKgPerSecond > 0.0);
        Assert.True(condenser.LastUncollectedWaterRateKgPerSecond >= 0.0);
        Assert.True(condenser.LastRetainedFilmKg >= 0.0);
        Assert.Contains(result.Diagnostics, d => d.Code == "CONDENSER.DRAINAGE_LOSS");
    }

    [Fact]
    public void Peltier_ProtectionShutdown_ZerosDriveOnOvertemperature()
    {
        var parameters = AnalyticalPeltierParameters.CreateProvisionalEngineeringDefaults() with
        {
            MaximumHotSideTemperatureK = UnitConversions.CelsiusToKelvin(35.0),
            EnableProtectionShutdown = true,
            HotSideThermalResistanceKPerW = 1.0
        };
        var peltier = new AnalyticalPeltierComponent(
            "tec",
            parameters,
            UnitConversions.CelsiusToKelvin(10.0),
            UnitConversions.CelsiusToKelvin(40.0),
            requestedElectricalPowerW: 25.0);

        var result = EvaluatePeltier(peltier);
        Assert.True(peltier.LastProtectionTripped);
        Assert.Equal(0.0, peltier.LastElectricalPowerW, precision: 12);
        Assert.Equal(0.0, peltier.LastCurrentA, precision: 12);
        Assert.Contains(result.Diagnostics, d => d.Code == "PELTIER.MODULE_DISABLED_BY_PROTECTION");
        Assert.Contains(result.Diagnostics, d => d.Code == "PELTIER.HOT_SIDE_OVERTEMPERATURE");
    }

    [Fact]
    public void Peltier_LowCop_EmitsDiagnostic()
    {
        var parameters = AnalyticalPeltierParameters.CreateProvisionalEngineeringDefaults() with
        {
            MinimumUsefulCoolingCop = 5.0,
            EnableProtectionShutdown = false
        };
        var peltier = new AnalyticalPeltierComponent(
            "tec",
            parameters,
            UnitConversions.CelsiusToKelvin(10.0),
            UnitConversions.CelsiusToKelvin(50.0),
            requestedElectricalPowerW: 30.0);

        var result = EvaluatePeltier(peltier);
        Assert.Contains(result.Diagnostics, d => d.Code == "PELTIER.COP_BELOW_USEFUL_THRESHOLD");
    }

    [Fact]
    public void PowerManager_ServesHigherPriorityFirst_AndShedsOptional()
    {
        // Cap discharge so battery cannot cover the full deficit (forces shedding).
        var battery = DefaultBattery() with { MaximumDischargePowerW = 0.0 };
        var initial = BatteryState.Create(0.5 * battery.NominalCapacityJ, battery.NominalCapacityJ, 298.15);
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
                },
                new ElectricalLoadDemand
                {
                    LoadId = "peltier",
                    RequestedPowerW = 40.0,
                    Priority = 1,
                    IsEssential = false
                },
                new ElectricalLoadDemand
                {
                    LoadId = "aux",
                    RequestedPowerW = 20.0,
                    Priority = 2,
                    IsEssential = false
                }
            ],
            initial,
            mpptEfficiencyFraction: 1.0);

        var result = EvaluatePower(pm, generationW: 45.0, dtSeconds: 1);
        pm.Commit(result);

        Assert.Equal(10.0, pm.LastDeliveredLoadPowerW["controller"], precision: 8);
        Assert.Equal(35.0, pm.LastDeliveredLoadPowerW["peltier"], precision: 8);
        Assert.Equal(0.0, pm.LastDeliveredLoadPowerW["aux"], precision: 8);
        Assert.Contains(result.Diagnostics, d => d.Code == "POWER.LOAD_SHED");
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == "POWER.ESSENTIAL_LOAD_UNSERVED");
    }

    [Fact]
    public void PowerManager_EssentialUnserved_IsCritical()
    {
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
                    RequestedPowerW = 20.0,
                    Priority = 0,
                    IsEssential = true
                }
            ],
            initial,
            mpptEfficiencyFraction: 1.0);

        var result = EvaluatePower(pm, generationW: 5.0, dtSeconds: 1);
        Assert.Contains(result.Diagnostics, d => d.Code == "POWER.ESSENTIAL_LOAD_UNSERVED");
        Assert.True(pm.LastUnservedPowerW > 0.0);
    }

    [Fact]
    public void PowerManager_SurplusChargesBattery_AndCurtailsRemainder()
    {
        var battery = DefaultBattery() with { MaximumChargePowerW = 30.0 };
        var initial = BatteryState.Create(0.4 * battery.NominalCapacityJ, battery.NominalCapacityJ, 298.15);
        var pm = new PowerManagementComponent(
            "pm",
            battery,
            [
                new ElectricalLoadDemand
                {
                    LoadId = "fan",
                    RequestedPowerW = 10.0,
                    Priority = 0,
                    IsEssential = true
                }
            ],
            initial,
            mpptEfficiencyFraction: 1.0);

        var result = EvaluatePower(pm, generationW: 100.0, dtSeconds: 10);
        pm.Commit(result);
        Assert.Equal(10.0, pm.LastServedLoadPowerW, precision: 8);
        Assert.Equal(30.0, pm.LastBatteryChargePowerW, precision: 8);
        Assert.True(pm.LastCurtailedPowerW > 0.0);
        Assert.Contains(result.Diagnostics, d => d.Code == "POWER.SOLAR_CURTAILED");
        Assert.True(pm.BatteryState.StateOfChargeFraction > 0.4);
    }

    [Fact]
    public void SimulationEngine_ReportsProgress()
    {
        var inlet = SampleAir(20, 0.4, 0.01);
        var source = new AmbientAirSourceComponent("air", inlet);
        var sink = new ExhaustAirSinkComponent("sink");
        var reports = new List<SimulationProgress>();
        var progress = new SynchronousProgress<SimulationProgress>(reports.Add);

        var result = new SimulationEngine().Run(
            new SimulationRequest
            {
                Graph = new SimulationGraph(
                    [source, sink],
                    [
                        new PhysicalConnection
                        {
                            Id = "a_s",
                            SourceComponentId = "air",
                            SourcePortId = "outlet",
                            TargetComponentId = "sink",
                            TargetPortId = "inlet"
                        }
                    ]),
                StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                Duration = TimeSpan.FromSeconds(3),
                TimeStep = TimeSpan.FromSeconds(1)
            },
            progress: progress);

        Assert.True(result.Succeeded);
        Assert.Contains(reports, r => r.CurrentPhase == "Validate");
        Assert.Contains(reports, r => r.CurrentPhase == "Execute");
        Assert.Contains(reports, r => r.CurrentPhase == "Complete");
        Assert.Equal(3, reports.Last(r => r.CurrentPhase == "Complete").CompletedSteps);
        Assert.Equal(1.0, reports.Last(r => r.CurrentPhase == "Complete").FractionComplete, precision: 12);
    }

    private sealed class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;

        public SynchronousProgress(Action<T> handler) => _handler = handler;

        public void Report(T value) => _handler(value);
    }

    private static SilicaGelParameters BaseSilica()
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
            AirBedHeatTransferCoefficientWPerK = 40.0
        };

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

    private static ComponentStepResult EvaluateSilica(SilicaGelBedComponent bed, MoistAirState inlet)
    {
        var context = new ComponentStepContext
        {
            Simulation = new SimulationContext
            {
                SimulationStart = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                TimeStep = TimeSpan.FromSeconds(5),
                ElapsedTime = TimeSpan.Zero,
                StepIndex = 0
            },
            InputStates = new Dictionary<string, object?>(StringComparer.Ordinal) { ["inlet"] = inlet }
        };
        bed.Initialize(context.Simulation);
        return bed.Evaluate(context);
    }

    private static ComponentStepResult EvaluateCondenser(CondenserComponent condenser, MoistAirState inlet)
    {
        var context = new ComponentStepContext
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
        condenser.Initialize(context.Simulation);
        return condenser.Evaluate(context);
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

    private static ComponentStepResult EvaluatePower(PowerManagementComponent pm, double generationW, double dtSeconds)
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
                ["generation"] = new ElectricalPowerState { PowerW = generationW }
            }
        };
        pm.Initialize(context.Simulation);
        return pm.Evaluate(context);
    }
}
