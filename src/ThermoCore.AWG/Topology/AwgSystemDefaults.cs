using ThermoCore.Core.Components.Adsorption;
using ThermoCore.Core.Components.Power;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.Topology;

/// <summary>Factory for provisional MVP configuration used by demos and tests.</summary>
public static class AwgSystemDefaults
{
    public static AwgSystemConfiguration CreateMvpConfiguration(
        bool enableElectricalSubsystem = true,
        bool enableRecirculation = false,
        bool enableHeatRecovery = false)
    {
        var ambientTemperatureK = UnitConversions.CelsiusToKelvin(25.0);
        var modelSelections = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [AwgV3TopologyIds.ProcessFan] = AwgV3TopologyIds.ModelIds.PrescribedFlowFan,
            [AwgV3TopologyIds.PeltierHotSideHx] = AwgV3TopologyIds.ModelIds.MoistAirPassThrough,
            [AwgV3TopologyIds.SolarCollector] = AwgV3TopologyIds.ModelIds.DynamicLumpedCollector,
            [AwgV3TopologyIds.SilicaGelBed] = AwgV3TopologyIds.ModelIds.SilicaGelLdfLinear,
            [AwgV3TopologyIds.Condenser] = AwgV3TopologyIds.ModelIds.CondenserBypassFactor,
            [AwgV3TopologyIds.WaterTank] = AwgV3TopologyIds.ModelIds.WaterTankInventory,
            [AwgV3TopologyIds.PvPanel] = AwgV3TopologyIds.ModelIds.ConstantEfficiencyPv,
            [AwgV3TopologyIds.PowerManager] = AwgV3TopologyIds.ModelIds.PowerManagerWithBattery
        };

        if (enableRecirculation)
        {
            modelSelections[AwgV3TopologyIds.FreshAirMixer] = AwgV3TopologyIds.ModelIds.MoistAirMixer;
            modelSelections[AwgV3TopologyIds.RecirculationSplitter] = AwgV3TopologyIds.ModelIds.MoistAirSplitter;
        }

        return new AwgSystemConfiguration
        {
            TopologyId = AwgV3TopologyIds.TopologyId,
            TopologyVersion = AwgV3TopologyIds.TopologyVersion,
            Topology = new AwgV3TopologyConfiguration
            {
                EnableRecirculation = enableRecirculation,
                EnableHeatRecovery = enableHeatRecovery,
                EnablePvRearAirChannel = false,
                EnableElectricalSubsystem = enableElectricalSubsystem,
                InitialRecirculationFraction = enableRecirculation ? 0.2 : 0.0,
                HeatRecoveryColdSideSource = "mixed-inlet",
                ComponentModelSelections = modelSelections
            },
            Ambient = new AwgAmbientBoundaryConfiguration
            {
                TemperatureK = ambientTemperatureK,
                PressurePa = PhysicalConstants.StandardAtmosphericPressurePa,
                RelativeHumidityFraction = 0.50,
                DryAirMassFlowKgPerSecond = 0.02,
                SolarIrradianceWPerSquareMeter = 800.0
            },
            Fan = new AwgFanParameters
            {
                DryAirMassFlowKgPerSecond = 0.02,
                PressureRisePa = 100.0
            },
            SolarCollector = new AwgSolarCollectorParameters
            {
                OpticalEfficiencyFraction = 0.75,
                ApertureAreaM2 = 2.0,
                EffectiveThermalCapacityJPerK = 8_000.0,
                AbsorberToAirUaWPerK = 40.0,
                OverallLossCoefficientWPerM2K = 4.0
            },
            SilicaGel = new SilicaGelParameters
            {
                DryAdsorbentMassKg = 2.0,
                MaximumWaterLoadingKgPerKgDryAdsorbent = 0.35,
                MinimumRegeneratedLoadingKgPerKgDryAdsorbent = 0.02,
                EffectiveSpecificHeatJPerKgK = 920.0,
                BedHousingThermalCapacityJPerK = 500.0,
                EffectiveHeatOfAdsorptionJPerKgWater = 2_600_000.0,
                BedHeatLossCoefficientWPerK = 0.5,
                ReferenceMassTransferCoefficientPerSecond = 0.02,
                AmbientTemperatureK = ambientTemperatureK,
                AirBedHeatTransferCoefficientWPerK = 80.0
            },
            Condenser = new AwgCondenserParameters
            {
                BypassFactor = 0.15,
                DrainageEfficiency = 0.90,
                FallbackSurfaceTemperatureK = UnitConversions.CelsiusToKelvin(5.0),
                FallbackAvailableCoolingPowerW = 200.0,
                MaximumRetainedFilmKg = 0.01
            },
            WaterTank = new AwgWaterTankParameters
            {
                CapacityKg = 20.0,
                InitialTemperatureK = ambientTemperatureK
            },
            Pv = new AwgPvParameters
            {
                EfficiencyFraction = 0.18,
                AreaM2 = 1.5
            },
            Battery = new BatteryParameters
            {
                NominalCapacityJ = 3_600_000.0,
                MinimumSocFraction = 0.1,
                MaximumSocFraction = 0.9,
                ChargeEfficiencyFraction = 0.95,
                DischargeEfficiencyFraction = 0.95,
                MaximumChargePowerW = 200.0,
                MaximumDischargePowerW = 200.0
            },
            ElectricalLoads =
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
                    LoadId = "fan",
                    RequestedPowerW = 40.0,
                    Priority = 1,
                    IsEssential = true
                }
            ],
            MpptEfficiencyFraction = 0.95
        }.Validate();
    }

    public static AwgInitialState CreateMvpInitialState(AwgSystemConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();

        return new AwgInitialState
        {
            SilicaGelLoadingKgPerKg = 0.08,
            SilicaGelTemperatureK = configuration.Ambient.TemperatureK,
            SolarCollectorAbsorberTemperatureK = configuration.Ambient.TemperatureK,
            BatteryStoredEnergyJ = 0.5 * configuration.Battery.NominalCapacityJ,
            WaterTankContentKg = 0.0,
            RecirculationFraction = configuration.Topology.InitialRecirculationFraction,
            ControllerMode = "Adsorption"
        }.Validate(configuration);
    }
}
