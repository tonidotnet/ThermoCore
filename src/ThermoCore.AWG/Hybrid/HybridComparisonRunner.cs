using ThermoCore.AWG.Cooling;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Components.Thermoelectric;
using ThermoCore.Core.Components.VaporCompression;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.Hybrid;

/// <summary>
/// Runs hybrid architecture variants through the shared cooling-plant contract
/// with common water/energy KPIs (R6-001 / HYB-001…003).
/// </summary>
public sealed class HybridComparisonRunner
{
    private readonly IPsychrometricCalculator _calculator;
    private readonly AwgCondenserParameters _condenser;
    private readonly VaporCompressionPerformanceMap _vcMap;

    public HybridComparisonRunner(
        AwgCondenserParameters? condenserParameters = null,
        VaporCompressionPerformanceMap? vaporCompressionMap = null,
        IPsychrometricCalculator? calculator = null)
    {
        _calculator = calculator ?? new PsychrometricCalculator();
        _condenser = (condenserParameters
            ?? AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false).Condenser).Validate();
        _vcMap = (vaporCompressionMap
            ?? VaporCompressionPerformanceMapCatalog.CreateGenericSmallDcModuleReference()).Validate();
    }

    public HybridComparisonReport Run(IReadOnlyList<HybridComparisonScenario>? scenarios = null)
    {
        scenarios ??= HybridComparisonCatalog.CreateDefaultSuite();
        var points = new List<HybridComparisonPointResult>(scenarios.Count);
        foreach (var scenario in scenarios)
        {
            points.Add(Evaluate(scenario.Validate()));
        }

        return new HybridComparisonReport
        {
            Points = points
                .OrderBy(p => p.AmbientTemperatureC)
                .ThenBy(p => p.RelativeHumidityPercent)
                .ThenBy(p => p.Variant)
                .ToArray()
        };
    }

    public HybridComparisonPointResult Evaluate(HybridComparisonScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        scenario.Validate();

        try
        {
            var ambient = HybridStreamFactory.CreateAmbient(
                scenario.AmbientTemperatureC,
                scenario.RelativeHumidityFraction,
                scenario.DryAirMassFlowKgPerSecond,
                _calculator);

            var (inlet, desorbed) = ResolveInlet(scenario, ambient);
            var plant = CreatePlant(scenario.Variant);
            var request = BuildRequest(scenario, inlet);
            var result = plant.Evaluate(request);

            var surfaceK = UnitConversions.CelsiusToKelvin(scenario.CoolingSurfaceTemperatureC);
            var condensed = Math.Max(0.0, result.CollectedWaterKgPerSecond);
            var exhausted = Math.Max(0.0, result.Outlet.WaterVaporMassFlowKgPerSecond);
            var litersPerKwh = CommercialPeltierBlackBoxKpis.LitersPerKwhElectric(
                condensed,
                result.ElectricalInputW);

            var energyOk = Math.Abs(result.Balance.EnergyResidualJ) < 1e-2;
            var waterOk = Math.Abs(result.Balance.WaterMassResidualKg) < 1e-6;
            var passed = energyOk && waterOk
                && !result.Diagnostics.Any(d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error);

            return new HybridComparisonPointResult
            {
                ScenarioId = scenario.ScenarioId,
                Variant = scenario.Variant,
                AmbientTemperatureC = scenario.AmbientTemperatureC,
                RelativeHumidityPercent = scenario.RelativeHumidityFraction * 100.0,
                InletTemperatureC = UnitConversions.KelvinToCelsius(inlet.TemperatureK),
                InletDewPointC = UnitConversions.KelvinToCelsius(inlet.DewPointTemperatureK),
                InletHumidityRatioKgPerKgDryAir = inlet.HumidityRatioKgPerKgDryAir,
                CondensedWaterKgPerSecond = condensed,
                ExhaustedVaporKgPerSecond = exhausted,
                DesorbedVaporKgPerSecond = desorbed,
                CoolingDeliveredW = result.CoolingDeliveredW,
                ElectricalInputW = result.ElectricalInputW,
                BareDeviceCop = result.BareDeviceCop,
                CoolingPlantCop = result.CoolingPlantCop,
                LitersPerKwhElectric = litersPerKwh,
                DewPointMarginK = inlet.DewPointTemperatureK - surfaceK,
                EnergyResidualJ = result.Balance.EnergyResidualJ,
                WaterMassResidualKg = result.Balance.WaterMassResidualKg,
                Passed = passed,
                FailureMessage = passed
                    ? null
                    : string.Join("; ",
                        result.Diagnostics
                            .Where(d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error)
                            .Select(d => d.Code)
                            .DefaultIfEmpty($"residual energy={result.Balance.EnergyResidualJ:G4}, water={result.Balance.WaterMassResidualKg:G4}"))
            };
        }
        catch (Exception ex)
        {
            return new HybridComparisonPointResult
            {
                ScenarioId = scenario.ScenarioId,
                Variant = scenario.Variant,
                AmbientTemperatureC = scenario.AmbientTemperatureC,
                RelativeHumidityPercent = scenario.RelativeHumidityFraction * 100.0,
                InletTemperatureC = scenario.AmbientTemperatureC,
                InletDewPointC = double.NaN,
                InletHumidityRatioKgPerKgDryAir = double.NaN,
                CondensedWaterKgPerSecond = 0.0,
                ExhaustedVaporKgPerSecond = 0.0,
                CoolingDeliveredW = 0.0,
                ElectricalInputW = 0.0,
                EnergyResidualJ = double.NaN,
                WaterMassResidualKg = double.NaN,
                Passed = false,
                FailureMessage = ex.Message
            };
        }
    }

    private (MoistAirState Inlet, double? DesorbedKgPerSecond) ResolveInlet(
        HybridComparisonScenario scenario,
        MoistAirState ambient)
    {
        switch (scenario.Variant)
        {
            case HybridComparisonVariant.DirectTec:
            case HybridComparisonVariant.DirectCompressor:
                return (ambient, null);

            case HybridComparisonVariant.HeatingOnlyControl:
            {
                var heated = HybridStreamFactory.CreateHeatedControlStream(
                    ambient,
                    scenario.HeatingTemperatureRiseK,
                    _calculator);
                return (heated, null);
            }

            case HybridComparisonVariant.SorbentPlusTec:
            case HybridComparisonVariant.SorbentPlusCompressor:
            {
                var regen = HybridStreamFactory.CreateRegenerationStream(
                    ambient,
                    UnitConversions.CelsiusToKelvin(scenario.RegenerationTemperatureC),
                    scenario.RegenerationDewPointBoostK,
                    _calculator);
                var desorbed = Math.Max(
                    0.0,
                    regen.WaterVaporMassFlowKgPerSecond - ambient.WaterVaporMassFlowKgPerSecond);
                return (regen, desorbed);
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(scenario.Variant));
        }
    }

    private ICoolingPlantModel CreatePlant(HybridComparisonVariant variant)
        => variant switch
        {
            HybridComparisonVariant.DirectTec
                or HybridComparisonVariant.HeatingOnlyControl
                or HybridComparisonVariant.SorbentPlusTec
                => CoolingPlantFactory.Create(
                    new AwgCoolingPlantConfiguration { Technology = CoolingTechnology.Thermoelectric },
                    _condenser,
                    _calculator),
            HybridComparisonVariant.DirectCompressor
                or HybridComparisonVariant.SorbentPlusCompressor
                => CoolingPlantFactory.Create(
                    new AwgCoolingPlantConfiguration
                    {
                        Technology = CoolingTechnology.VaporCompression,
                        VaporCompressionMap = _vcMap
                    },
                    _condenser,
                    _calculator),
            _ => throw new ArgumentOutOfRangeException(nameof(variant))
        };

    private static CoolingPlantRequest BuildRequest(HybridComparisonScenario scenario, MoistAirState inlet)
    {
        var surfaceK = UnitConversions.CelsiusToKelvin(scenario.CoolingSurfaceTemperatureC);
        var condensingK = UnitConversions.CelsiusToKelvin(scenario.CondensingTemperatureC);
        var simulation = new SimulationContext
        {
            SimulationStart = DateTimeOffset.Parse("2026-07-15T12:00:00Z"),
            TimeStep = scenario.TimeStep,
            ElapsedTime = TimeSpan.Zero,
            StepIndex = 0
        };

        var isCompressor = scenario.Variant is HybridComparisonVariant.DirectCompressor
            or HybridComparisonVariant.SorbentPlusCompressor;

        return new CoolingPlantRequest
        {
            Inlet = inlet,
            Simulation = simulation,
            ColdSurfaceTemperatureK = surfaceK,
            EvaporatingTemperatureK = surfaceK,
            CondensingTemperatureK = condensingK,
            AvailableCoolingPowerW = isCompressor ? null : scenario.TecAvailableCoolingPowerW,
            ElectricalPowerW = isCompressor ? null : scenario.TecAvailableCoolingPowerW,
            CompressorSpeedFraction = isCompressor ? scenario.CompressorSpeedFraction : null,
            CompressorRequested = isCompressor ? true : null,
            FanElectricalPowerW = scenario.ProcessFanElectricalPowerW
        };
    }
}
