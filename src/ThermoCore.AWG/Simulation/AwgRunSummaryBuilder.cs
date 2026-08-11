using ThermoCore.AWG.Measurement;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Components;
using ThermoCore.Core.Components.Power;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.Simulation;

/// <summary>Builds <see cref="AwgRunSummary"/> from an engine result.</summary>
public static class AwgRunSummaryBuilder
{
    public static AwgRunSummary Build(
        AwgBuiltSystem built,
        AwgSimulationOptions options,
        SimulationRunResult engineResult)
    {
        ArgumentNullException.ThrowIfNull(built);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(engineResult);

        var warningCount = engineResult.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
        var errorCount = engineResult.Diagnostics.Count(d =>
            d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Critical);

        var temperatures = new Dictionary<string, double?>(StringComparer.Ordinal);
        var humidity = new Dictionary<string, double?>(StringComparer.Ordinal);
        double? busPower = null;
        double? curtailedPower = null;

        if (engineResult.Steps.Count > 0)
        {
            var last = engineResult.Steps[^1];
            foreach (var sample in AwgMeasurementSampler.SampleMoistAir(built, last))
            {
                temperatures[sample.PointId] = UnitConversions.KelvinToCelsius(sample.TemperatureK);
                humidity[sample.PointId] = sample.HumidityRatioKgPerKgDryAir;
            }

            // PortStates store component outputs; bus/curtail live on PowerManager ports.
            if (last.PortStates.TryGetValue($"{AwgV3TopologyIds.PowerManager}.bus", out var bus)
                && bus is ElectricalPowerState busState)
            {
                busPower = busState.PowerW;
            }

            if (last.PortStates.TryGetValue($"{AwgV3TopologyIds.PowerManager}.curtailed", out var curtail)
                && curtail is ElectricalPowerState curtailState)
            {
                curtailedPower = curtailState.PowerW;
            }
        }

        double? tankContent = null;
        double? tankLevel = null;
        if (built.Graph.Components.FirstOrDefault(c => c.Id == AwgV3TopologyIds.WaterTank)
            is WaterTankComponent tank)
        {
            tankContent = tank.StoredMassKg;
            tankLevel = tank.LevelFraction;
        }

        var solar = ComputeSolarMetrics(built, options, engineResult);
        var battery = ComputeBatteryMetrics(built);
        var kpis = AwgPerformanceKpiCalculator.Compute(built, options, engineResult, tankContent);
        var cooling = AwgCoolingMetricsCalculator.Compute(built, options, engineResult);

        return new AwgRunSummary
        {
            Succeeded = engineResult.Succeeded,
            TopologyId = built.Metadata.TopologyId,
            TopologyVersion = built.Metadata.TopologyVersion,
            GraphFingerprint = built.Metadata.GraphFingerprint,
            ComponentCount = built.Graph.Components.Count,
            ConnectionCount = built.Graph.Connections.Count,
            CompletedSteps = engineResult.Steps.Count,
            Duration = options.Duration,
            TimeStep = options.TimeStep,
            AggregatedEnergyResidualJ = engineResult.AggregatedBalance.EnergyResidualJ,
            AggregatedWaterResidualKg = engineResult.AggregatedBalance.WaterMassResidualKg,
            AggregatedDryAirResidualKg = engineResult.AggregatedBalance.DryAirMassResidualKg,
            WarningCount = warningCount,
            ErrorCount = errorCount,
            FinalMoistAirTemperaturesC = temperatures,
            FinalHumidityRatiosKgPerKg = humidity,
            FinalBusPowerW = busPower,
            FinalCurtailedPowerW = curtailedPower,
            FinalWaterTankContentKg = tankContent,
            FinalWaterTankLevelFraction = tankLevel,
            MeanIncidentSolarIrradianceWPerM2 = solar.MeanIrradianceWPerM2,
            IncidentSolarEnergyJ = solar.IncidentEnergyJ,
            UsefulCollectorEnergyJ = solar.UsefulEnergyJ,
            SolarUtilizationFraction = solar.UtilizationFraction,
            FinalBatteryStateOfChargeFraction = battery.FinalSocFraction,
            BatteryStateOfChargeSwingFraction = battery.SocSwingFraction,
            BatteryThroughputFraction = battery.ThroughputFraction,
            LitersPerDay = kpis.LitersPerDay,
            ElectricEnergyConsumedJ = kpis.ElectricEnergyConsumedJ,
            BusElectricalEnergyJ = kpis.BusElectricalEnergyJ,
            PeltierElectricalProxyEnergyJ = kpis.PeltierElectricalProxyEnergyJ,
            AmbientMoistureIntakeKg = kpis.AmbientMoistureIntakeKg,
            DesorbedWaterMassKg = kpis.DesorbedWaterMassKg,
            SolarCollectorApertureAreaM2 = kpis.SolarCollectorApertureAreaM2,
            LitersPerKwhElectric = kpis.LitersPerKwhElectric,
            LitersPerKwhSolarPrimary = kpis.LitersPerKwhSolarPrimary,
            LitersPerDayPerSquareMeterAperture = kpis.LitersPerDayPerSquareMeterAperture,
            WaterRecoveryFraction = kpis.WaterRecoveryFraction,
            DesorptionCaptureFraction = kpis.DesorptionCaptureFraction,
            WattHoursElectricPerLiter = kpis.WattHoursElectricPerLiter,
            CoolingPlantThermalInputJ = cooling.CoolingPlantThermalInputJ,
            CoolingPlantElectricalEnergyJ = cooling.CoolingPlantElectricalEnergyJ,
            BareCoolingDeviceCOP = cooling.BareCoolingDeviceCOP,
            CoolingPlantCOP = cooling.CoolingPlantCOP,
            AverageTemperatureLiftK = cooling.AverageTemperatureLiftK,
            AverageDewPointMarginK = cooling.AverageDewPointMarginK
        };
    }

    private static (
        double? MeanIrradianceWPerM2,
        double? IncidentEnergyJ,
        double? UsefulEnergyJ,
        double? UtilizationFraction) ComputeSolarMetrics(
        AwgBuiltSystem built,
        AwgSimulationOptions options,
        SimulationRunResult engineResult)
    {
        if (engineResult.Steps.Count == 0)
        {
            return (null, null, null, null);
        }

        var apertureM2 = built.Configuration.SolarCollector.ApertureAreaM2;
        var dt = options.TimeStep.TotalSeconds;
        if (dt <= 0.0 || apertureM2 <= 0.0)
        {
            return (null, null, null, null);
        }

        var solarKey = $"{AwgV3TopologyIds.SolarRadiation}.outlet";
        var inletKey = $"{AwgV3TopologyIds.SolarCollector}.inlet";
        var outletKey = $"{AwgV3TopologyIds.SolarCollector}.outlet";

        var irradianceSum = 0.0;
        var irradianceCount = 0;
        var incidentJ = 0.0;
        var usefulJ = 0.0;

        foreach (var step in engineResult.Steps)
        {
            if (step.PortStates.TryGetValue(solarKey, out var solarRaw)
                && solarRaw is SolarIrradianceState solar)
            {
                irradianceSum += solar.IrradianceWPerM2;
                irradianceCount++;
                incidentJ += solar.IrradianceWPerM2 * apertureM2 * dt;
            }

            if (step.PortStates.TryGetValue(inletKey, out var inletRaw)
                && inletRaw is MoistAirState inlet
                && step.PortStates.TryGetValue(outletKey, out var outletRaw)
                && outletRaw is MoistAirState outlet
                && inlet.DryAirMassFlowKgPerSecond > 0.0)
            {
                var heatW = inlet.DryAirMassFlowKgPerSecond
                    * (outlet.SpecificEnthalpyJPerKgDryAir - inlet.SpecificEnthalpyJPerKgDryAir);
                usefulJ += Math.Max(0.0, heatW) * dt;
            }
        }

        if (irradianceCount == 0)
        {
            return (null, null, null, null);
        }

        var meanG = irradianceSum / irradianceCount;
        double? utilization = incidentJ > 1e-12 ? usefulJ / incidentJ : null;
        return (meanG, incidentJ, usefulJ, utilization);
    }

    private static (
        double? FinalSocFraction,
        double? SocSwingFraction,
        double? ThroughputFraction) ComputeBatteryMetrics(AwgBuiltSystem built)
    {
        if (built.Graph.Components.FirstOrDefault(c => c.Id == AwgV3TopologyIds.PowerManager)
            is not PowerManagementComponent power)
        {
            return (null, null, null);
        }

        var capacityJ = built.Configuration.Battery.NominalCapacityJ;
        var swing = power.MaximumStateOfChargeFractionObserved - power.MinimumStateOfChargeFractionObserved;
        double? throughput = capacityJ > 0.0
            ? (power.AccumulatedChargeEnergyJ + power.AccumulatedDischargeEnergyJ) / capacityJ
            : null;

        return (
            power.BatteryState.StateOfChargeFraction,
            Math.Max(0.0, swing),
            throughput);
    }
}
