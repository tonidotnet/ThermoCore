using ThermoCore.Api.Contracts;
using ThermoCore.AWG.Optimization;
using ThermoCore.AWG.Simulation;

namespace ThermoCore.Api.Services;

/// <summary>Maps AWG run results to API summary contracts (API-011 KPIs).</summary>
public static class SimulationSummaryMapper
{
    public static SimulationSummaryResponse FromJob(SimulationJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        var run = job.RunResult ?? throw new InvalidOperationException("Job has no run result.");
        return FromRun(job.SimulationId, job.Status.ToString(), run);
    }

    public static SimulationSummaryResponse FromRun(
        string simulationId,
        string status,
        AwgSimulationRunResult run)
    {
        ArgumentNullException.ThrowIfNull(run);
        var waterKg = run.Summary.FinalWaterTankContentKg;
        double? litersPerDay = null;
        if (waterKg is { } kg && run.Options.Duration > TimeSpan.Zero)
        {
            litersPerDay = AwgOptimizationObjectives.LitersPerDay(kg, run.Options.Duration);
        }

        return new SimulationSummaryResponse
        {
            SimulationId = simulationId,
            Status = status,
            Succeeded = run.Summary.Succeeded,
            TopologyId = run.Summary.TopologyId,
            CompletedSteps = run.Summary.CompletedSteps,
            AggregatedEnergyResidualJ = run.Summary.AggregatedEnergyResidualJ,
            AggregatedWaterResidualKg = run.Summary.AggregatedWaterResidualKg,
            AggregatedDryAirResidualKg = run.Summary.AggregatedDryAirResidualKg,
            WaterBalancePassed = run.BalanceReport.WaterBalancePassed,
            EnergyBalancePassed = run.BalanceReport.EnergyBalancePassed,
            WarningCount = run.Summary.WarningCount,
            ErrorCount = run.Summary.ErrorCount,
            FinalWaterTankContentKg = waterKg,
            FinalBusPowerW = run.Summary.FinalBusPowerW,
            CollectedWaterKg = waterKg,
            LitersPerDay = litersPerDay ?? run.Summary.LitersPerDay,
            WattHoursPerLiter = AwgOptimizationObjectives.WattHoursPerLiter(run.Summary),
            LitersPerKwhElectric = run.Summary.LitersPerKwhElectric,
            LitersPerKwhSolarPrimary = run.Summary.LitersPerKwhSolarPrimary,
            LitersPerDayPerSquareMeterAperture = run.Summary.LitersPerDayPerSquareMeterAperture,
            WaterRecoveryFraction = run.Summary.WaterRecoveryFraction,
            DesorptionCaptureFraction = run.Summary.DesorptionCaptureFraction,
            BareCoolingDeviceCOP = run.Summary.BareCoolingDeviceCOP,
            CoolingPlantCOP = run.Summary.CoolingPlantCOP,
            AverageTemperatureLiftK = run.Summary.AverageTemperatureLiftK,
            AverageDewPointMarginK = run.Summary.AverageDewPointMarginK,
            CoolingPlantElectricalEnergyJ = run.Summary.CoolingPlantElectricalEnergyJ,
            CoolingPlantThermalInputJ = run.Summary.CoolingPlantThermalInputJ
        };
    }
}
