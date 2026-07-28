using ThermoCore.Core.Components;
using ThermoCore.Core.Components.Adsorption;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

public class AdsorptionAndHeatRecoveryTests
{
    private readonly PsychrometricCalculator _calculator = new();

    [Fact]
    public void GenericPolynomialIsotherm_Linear_MatchesRelativePressure()
    {
        var isotherm = GenericPolynomialIsotherm.CreateLinear(0.30);
        var bedTemperatureK = UnitConversions.CelsiusToKelvin(25.0);
        var saturation = _calculator.CalculateSaturationPressurePa(bedTemperatureK);
        var loading = isotherm.CalculateEquilibriumLoadingKgPerKg(
            bedTemperatureK,
            vaporPressurePa: 0.5 * saturation,
            saturationPressurePa: saturation);

        Assert.Equal(0.15, loading, precision: 10);
    }

    [Fact]
    public void LangmuirIsotherm_IncreasesWithVaporPressure()
    {
        var isotherm = new LangmuirIsotherm(
            monolayerCapacityKgPerKg: 0.25,
            affinityPreExponentialPerPa: 1e-3);

        var low = isotherm.CalculateEquilibriumLoadingKgPerKg(300.0, 500.0, 3500.0);
        var high = isotherm.CalculateEquilibriumLoadingKgPerKg(300.0, 2500.0, 3500.0);
        Assert.True(high > low);
        Assert.True(high < 0.25);
    }

    [Fact]
    public void SilicaGelBed_Adsorption_DriesAirAndStoresWater()
    {
        var parameters = DefaultParameters();
        var isotherm = GenericPolynomialIsotherm.CreateLinear(parameters.MaximumWaterLoadingKgPerKgDryAdsorbent);
        var initial = SilicaGelState.Create(
            dryAdsorbentMassKg: parameters.DryAdsorbentMassKg,
            waterLoadingKgPerKgDryAdsorbent: 0.05,
            bedTemperatureK: UnitConversions.CelsiusToKelvin(25.0),
            maximumWaterLoadingKgPerKgDryAdsorbent: parameters.MaximumWaterLoadingKgPerKgDryAdsorbent,
            minimumRegeneratedLoadingKgPerKgDryAdsorbent: parameters.MinimumRegeneratedLoadingKgPerKgDryAdsorbent,
            effectiveSpecificHeatJPerKgK: parameters.EffectiveSpecificHeatJPerKgK,
            bedHousingThermalCapacityJPerK: parameters.BedHousingThermalCapacityJPerK);

        var bed = new SilicaGelBedComponent("sg", parameters, isotherm, initial, _calculator);
        var inlet = SampleAir(25, 0.70, 0.02);
        var result = EvaluateStandalone(bed, inlet, TimeSpan.FromSeconds(5));

        Assert.DoesNotContain(result.Diagnostics, d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error);
        bed.Commit(result);

        var outlet = Assert.IsType<MoistAirState>(result.OutputStates["outlet"]);
        Assert.True(outlet.HumidityRatioKgPerKgDryAir < inlet.HumidityRatioKgPerKgDryAir);
        Assert.True(bed.State.WaterLoadingKgPerKgDryAdsorbent > initial.WaterLoadingKgPerKgDryAdsorbent);
        Assert.True(bed.LastWaterTransferRateKgPerSecond > 0.0);
        Assert.Equal(SilicaGelOperatingRegime.Adsorption, bed.State.OperatingRegime);
        Assert.Equal(0.0, result.Balance.WaterMassResidualKg, precision: 8);
        Assert.Equal(0.0, result.Balance.DryAirMassResidualKg, precision: 12);
    }

    [Fact]
    public void SilicaGelBed_Desorption_HumidifiesAirWhenHotAndDryLoaded()
    {
        var parameters = DefaultParameters() with
        {
            AmbientTemperatureK = UnitConversions.CelsiusToKelvin(25.0),
            ReferenceMassTransferCoefficientPerSecond = 0.05
        };
        var isotherm = GenericPolynomialIsotherm.CreateLinear(parameters.MaximumWaterLoadingKgPerKgDryAdsorbent);
        var initial = SilicaGelState.Create(
            dryAdsorbentMassKg: parameters.DryAdsorbentMassKg,
            waterLoadingKgPerKgDryAdsorbent: 0.25,
            bedTemperatureK: UnitConversions.CelsiusToKelvin(70.0),
            maximumWaterLoadingKgPerKgDryAdsorbent: parameters.MaximumWaterLoadingKgPerKgDryAdsorbent,
            minimumRegeneratedLoadingKgPerKgDryAdsorbent: parameters.MinimumRegeneratedLoadingKgPerKgDryAdsorbent,
            effectiveSpecificHeatJPerKgK: parameters.EffectiveSpecificHeatJPerKgK,
            bedHousingThermalCapacityJPerK: parameters.BedHousingThermalCapacityJPerK);

        var bed = new SilicaGelBedComponent("sg", parameters, isotherm, initial, _calculator);
        var inlet = SampleAir(70, 0.10, 0.02);
        var result = EvaluateStandalone(bed, inlet, TimeSpan.FromSeconds(5));

        Assert.DoesNotContain(result.Diagnostics, d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error);
        bed.Commit(result);

        var outlet = Assert.IsType<MoistAirState>(result.OutputStates["outlet"]);
        Assert.True(outlet.HumidityRatioKgPerKgDryAir > inlet.HumidityRatioKgPerKgDryAir);
        Assert.True(bed.State.WaterLoadingKgPerKgDryAdsorbent < initial.WaterLoadingKgPerKgDryAdsorbent);
        Assert.True(bed.LastWaterTransferRateKgPerSecond < 0.0);
        Assert.Equal(SilicaGelOperatingRegime.Desorption, bed.State.OperatingRegime);
    }

    [Fact]
    public void SilicaGelBed_ExactLdf_ApproachesEquilibrium()
    {
        var parameters = DefaultParameters() with
        {
            ReferenceMassTransferCoefficientPerSecond = 0.2,
            AirBedHeatTransferCoefficientWPerK = 40.0,
            EffectiveHeatOfAdsorptionJPerKgWater = 2_400_000.0
        };
        var isotherm = GenericPolynomialIsotherm.CreateLinear(parameters.MaximumWaterLoadingKgPerKgDryAdsorbent);
        var initial = SilicaGelState.Create(
            dryAdsorbentMassKg: parameters.DryAdsorbentMassKg,
            waterLoadingKgPerKgDryAdsorbent: 0.05,
            bedTemperatureK: UnitConversions.CelsiusToKelvin(25.0),
            maximumWaterLoadingKgPerKgDryAdsorbent: parameters.MaximumWaterLoadingKgPerKgDryAdsorbent,
            minimumRegeneratedLoadingKgPerKgDryAdsorbent: parameters.MinimumRegeneratedLoadingKgPerKgDryAdsorbent,
            effectiveSpecificHeatJPerKgK: parameters.EffectiveSpecificHeatJPerKgK,
            bedHousingThermalCapacityJPerK: parameters.BedHousingThermalCapacityJPerK);

        var bed = new SilicaGelBedComponent("sg", parameters, isotherm, initial, _calculator);
        var inlet = SampleAir(25, 0.50, 0.05);

        for (var i = 0; i < 40; i++)
        {
            var step = EvaluateStandalone(bed, inlet, TimeSpan.FromSeconds(2));
            Assert.DoesNotContain(step.Diagnostics, d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error);
            bed.Commit(step);
        }

        var endGap = Math.Abs(bed.State.WaterLoadingKgPerKgDryAdsorbent - bed.LastEquilibriumLoadingKgPerKg);
        Assert.True(endGap < 0.05);
        Assert.True(bed.State.WaterLoadingKgPerKgDryAdsorbent > initial.WaterLoadingKgPerKgDryAdsorbent);
    }

    [Fact]
    public void HeatRecovery_TransfersSensibleHeatWithoutHumidityChange()
    {
        var hx = new SensibleHeatRecoveryComponent("hr", effectivenessFraction: 0.7, calculator: _calculator);
        var hot = SampleAir(40, 0.30, 0.02);
        var cold = SampleAir(20, 0.50, 0.02);
        var result = EvaluateHeatRecovery(hx, hot, cold);

        Assert.DoesNotContain(result.Diagnostics, d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error);
        var hotOut = Assert.IsType<MoistAirState>(result.OutputStates["hot_out"]);
        var coldOut = Assert.IsType<MoistAirState>(result.OutputStates["cold_out"]);

        Assert.True(hotOut.TemperatureK < hot.TemperatureK);
        Assert.True(coldOut.TemperatureK > cold.TemperatureK);
        Assert.Equal(hot.HumidityRatioKgPerKgDryAir, hotOut.HumidityRatioKgPerKgDryAir, precision: 12);
        Assert.Equal(cold.HumidityRatioKgPerKgDryAir, coldOut.HumidityRatioKgPerKgDryAir, precision: 12);
        Assert.True(hx.LastRecoveredHeatW > 0.0);
        Assert.Equal(0.0, result.Balance.EnergyResidualJ, precision: 4);
        Assert.Equal(0.0, result.Balance.WaterMassResidualKg, precision: 12);
    }

    [Fact]
    public void HeatRecovery_EqualInletTemperatures_TransfersZero()
    {
        var hx = new SensibleHeatRecoveryComponent("hr", effectivenessFraction: 0.8, calculator: _calculator);
        var hot = SampleAir(25, 0.40, 0.02);
        var cold = SampleAir(25, 0.60, 0.03);
        var result = EvaluateHeatRecovery(hx, hot, cold);

        Assert.Equal(0.0, hx.LastRecoveredHeatW, precision: 10);
        Assert.Contains(result.Diagnostics, d => d.Code == "HEAT_RECOVERY.NO_DRIVING_TEMPERATURE");
        var hotOut = Assert.IsType<MoistAirState>(result.OutputStates["hot_out"]);
        var coldOut = Assert.IsType<MoistAirState>(result.OutputStates["cold_out"]);
        Assert.Equal(hot.TemperatureK, hotOut.TemperatureK, precision: 10);
        Assert.Equal(cold.TemperatureK, coldOut.TemperatureK, precision: 10);
    }

    [Fact]
    public void HeatRecovery_Bypass_ReducesRecoveredHeat()
    {
        var full = new SensibleHeatRecoveryComponent("hr0", effectivenessFraction: 0.8, bypassFraction: 0.0, calculator: _calculator);
        var bypassed = new SensibleHeatRecoveryComponent("hr1", effectivenessFraction: 0.8, bypassFraction: 0.5, calculator: _calculator);
        var hot = SampleAir(45, 0.20, 0.02);
        var cold = SampleAir(15, 0.40, 0.02);

        EvaluateHeatRecovery(full, hot, cold);
        EvaluateHeatRecovery(bypassed, hot, cold);

        Assert.True(bypassed.LastRecoveredHeatW < full.LastRecoveredHeatW);
        Assert.Equal(0.4, bypassed.LastEffectivenessFraction, precision: 10);
    }

    private static SilicaGelParameters DefaultParameters()
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

    private static ComponentStepResult EvaluateStandalone(
        SilicaGelBedComponent bed,
        MoistAirState inlet,
        TimeSpan timeStep)
    {
        var context = new ComponentStepContext
        {
            Simulation = new SimulationContext
            {
                SimulationStart = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                TimeStep = timeStep,
                ElapsedTime = TimeSpan.Zero,
                StepIndex = 0
            },
            InputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["inlet"] = inlet
            }
        };

        bed.Initialize(context.Simulation);
        return bed.Evaluate(context);
    }

    private static ComponentStepResult EvaluateHeatRecovery(
        SensibleHeatRecoveryComponent hx,
        MoistAirState hot,
        MoistAirState cold)
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
            InputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["hot_in"] = hot,
                ["cold_in"] = cold
            }
        };

        hx.Initialize(context.Simulation);
        var result = hx.Evaluate(context);
        hx.Commit(result);
        return result;
    }
}
