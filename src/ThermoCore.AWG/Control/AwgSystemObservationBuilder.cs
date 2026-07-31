using ThermoCore.AWG.Topology;
using ThermoCore.Core.Components;
using ThermoCore.Core.Components.Adsorption;
using ThermoCore.Core.Components.Power;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.AWG.Control;

/// <summary>Builds controller observations from committed plant state.</summary>
public static class AwgSystemObservationBuilder
{
    public static AwgSystemObservation CreateSeed(
        AwgBuiltSystem built,
        DateTimeOffset simulationTimeUtc)
    {
        ArgumentNullException.ThrowIfNull(built);
        var ambient = built.Configuration.Ambient;
        var condenser = built.Configuration.Condenser;
        var calc = new PsychrometricCalculator();
        var vaporPressure = calc.CalculateVaporPressureFromRelativeHumidityPa(
            ambient.TemperatureK,
            ambient.RelativeHumidityFraction);
        var dewPointK = calc.CalculateDewPointTemperatureK(vaporPressure)
            ?? Math.Max(230.0, ambient.TemperatureK - 20.0);

        return new AwgSystemObservation
        {
            SimulationTimeUtc = simulationTimeUtc,
            AmbientTemperatureK = ambient.TemperatureK,
            AmbientRelativeHumidityFraction = ambient.RelativeHumidityFraction,
            AmbientVaporPressurePa = vaporPressure,
            SolarIrradianceWPerSquareMeter = ambient.SolarIrradianceWPerSquareMeter,
            BatteryStateOfChargeFraction = ResolveBatterySoc(built),
            AvailableElectricalPowerW = ResolveAvailableElectricalPowerW(built),
            SilicaGelLoadingKgPerKg = built.InitialState.SilicaGelLoadingKgPerKg,
            SilicaGelTemperatureK = built.InitialState.SilicaGelTemperatureK,
            SilicaGelEquilibriumLoadingKgPerKg =
                built.Configuration.SilicaGel.MaximumWaterLoadingKgPerKgDryAdsorbent
                * ambient.RelativeHumidityFraction,
            CondenserSurfaceTemperatureK = condenser.FallbackSurfaceTemperatureK,
            InletDewPointTemperatureK = dewPointK,
            CondenserInletDewPointTemperatureK = dewPointK,
            PeltierHotSideTemperatureK = ambient.TemperatureK + 5.0,
            PeltierColdSideTemperatureK = condenser.FallbackSurfaceTemperatureK,
            CollectorAbsorberTemperatureK = built.InitialState.SolarCollectorAbsorberTemperatureK,
            ProcessDryAirMassFlowKgPerSecond = built.Configuration.Fan.DryAirMassFlowKgPerSecond,
            WaterTankLevelFraction = built.InitialState.WaterTankContentKg
                / Math.Max(built.Configuration.WaterTank.CapacityKg, 1e-12),
            FanOperatingPointValid = true,
            ComponentDiagnostics = Array.Empty<SimulationDiagnostic>()
        }.Validate();
    }

    public static AwgSystemObservation CreateFromCommittedState(
        AwgBuiltSystem built,
        SimulationContext context,
        IReadOnlyDictionary<string, object?> committedPortStates)
    {
        ArgumentNullException.ThrowIfNull(built);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(committedPortStates);

        var seed = CreateSeed(built, context.SimulationStart + context.ElapsedTime);
        if (context.StepIndex == 0 && committedPortStates.Count == 0)
        {
            return seed;
        }

        var ambientKey = $"{AwgV3TopologyIds.AmbientSource}.outlet";
        var solarKey = $"{AwgV3TopologyIds.SolarRadiation}.outlet";
        var bedOutletKey = $"{AwgV3TopologyIds.SilicaGelBed}.outlet";
        var fanOutletKey = $"{AwgV3TopologyIds.ProcessFan}.outlet";

        var ambient = TryMoistAir(committedPortStates, ambientKey);
        var bedOutlet = TryMoistAir(committedPortStates, bedOutletKey);
        var fanOutlet = TryMoistAir(committedPortStates, fanOutletKey);
        var solar = committedPortStates.TryGetValue(solarKey, out var solarRaw)
            && solarRaw is SolarIrradianceState solarState
            ? solarState.IrradianceWPerM2
            : seed.SolarIrradianceWPerSquareMeter;

        var bed = built.Graph.Components.OfType<SilicaGelBedComponent>()
            .FirstOrDefault(c => c.Id == AwgV3TopologyIds.SilicaGelBed);
        var collector = built.Graph.Components.OfType<DynamicLumpedSolarCollectorComponent>()
            .FirstOrDefault(c => c.Id == AwgV3TopologyIds.SolarCollector);
        var cooling = built.Graph.Components.OfType<ControllableHeatSourceComponent>()
            .FirstOrDefault(c => c.Id == AwgV3TopologyIds.CondenserCooling);
        var tank = built.Graph.Components.OfType<WaterTankComponent>()
            .FirstOrDefault(c => c.Id == AwgV3TopologyIds.WaterTank);

        var surfaceK = cooling?.TemperatureK ?? seed.CondenserSurfaceTemperatureK;
        var diagnostics = built.Graph.Components
            .SelectMany(c => c.GetDiagnostics())
            .Where(d => d.Severity is not DiagnosticSeverity.Critical
                || string.Equals(d.Code, "POWER.SOLAR_CURTAILED", StringComparison.Ordinal))
            .ToArray();

        return new AwgSystemObservation
        {
            SimulationTimeUtc = context.SimulationStart + context.ElapsedTime,
            AmbientTemperatureK = ambient?.TemperatureK ?? seed.AmbientTemperatureK,
            AmbientRelativeHumidityFraction = ambient?.RelativeHumidityFraction
                ?? seed.AmbientRelativeHumidityFraction,
            AmbientVaporPressurePa = ambient?.VaporPressurePa ?? seed.AmbientVaporPressurePa,
            SolarIrradianceWPerSquareMeter = solar,
            BatteryStateOfChargeFraction = ResolveBatterySoc(built),
            AvailableElectricalPowerW = ResolveAvailableElectricalPowerW(built),
            SilicaGelLoadingKgPerKg = bed?.State.WaterLoadingKgPerKgDryAdsorbent
                ?? seed.SilicaGelLoadingKgPerKg,
            SilicaGelTemperatureK = bed?.State.BedTemperatureK ?? seed.SilicaGelTemperatureK,
            SilicaGelEquilibriumLoadingKgPerKg = bed?.LastEquilibriumLoadingKgPerKg
                ?? seed.SilicaGelEquilibriumLoadingKgPerKg,
            CondenserSurfaceTemperatureK = surfaceK,
            InletDewPointTemperatureK = ambient?.DewPointTemperatureK ?? seed.InletDewPointTemperatureK,
            CondenserInletDewPointTemperatureK = bedOutlet?.DewPointTemperatureK
                ?? seed.CondenserInletDewPointTemperatureK,
            PeltierHotSideTemperatureK = ambient?.TemperatureK + 5.0
                ?? seed.PeltierHotSideTemperatureK,
            PeltierColdSideTemperatureK = surfaceK,
            CollectorAbsorberTemperatureK = collector?.AbsorberTemperatureK
                ?? seed.CollectorAbsorberTemperatureK,
            ProcessDryAirMassFlowKgPerSecond = fanOutlet?.DryAirMassFlowKgPerSecond
                ?? seed.ProcessDryAirMassFlowKgPerSecond,
            WaterTankLevelFraction = tank?.LevelFraction ?? seed.WaterTankLevelFraction,
            FanOperatingPointValid = true,
            ComponentDiagnostics = diagnostics
        }.Validate();
    }

    private static MoistAirState? TryMoistAir(
        IReadOnlyDictionary<string, object?> ports,
        string key)
        => ports.TryGetValue(key, out var raw) && raw is MoistAirState state ? state : null;

    private static double ResolveBatterySoc(AwgBuiltSystem built)
    {
        if (built.Graph.Components.FirstOrDefault(c => c.Id == AwgV3TopologyIds.PowerManager)
            is PowerManagementComponent power)
        {
            return power.BatteryState.StateOfChargeFraction;
        }

        var capacity = built.Configuration.Battery.NominalCapacityJ;
        return capacity <= 0.0
            ? 0.0
            : Math.Clamp(built.InitialState.BatteryStoredEnergyJ / capacity, 0.0, 1.0);
    }

    private static double ResolveAvailableElectricalPowerW(AwgBuiltSystem built)
    {
        var battery = built.Configuration.Battery;
        var soc = ResolveBatterySoc(built);
        if (soc <= battery.MinimumSocFraction)
        {
            return 0.0;
        }

        // Controllable electrical budget for the Peltier proxy until a real Peltier load is wired.
        return battery.MaximumDischargePowerW;
    }
}
