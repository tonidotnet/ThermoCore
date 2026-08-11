using System.Text.Json;
using ThermoCore.AWG.Configuration;
using ThermoCore.Core.Results;
using ThermoCore.Core.Simulation;

namespace ThermoCore.AWG.Simulation;

/// <summary>
/// Collects Core <see cref="SimulationResult"/> and writes DOC-029 CSV/full export packages
/// (APP-005, AWG-017).
/// </summary>
public static class AwgResultExporter
{
    public static SimulationResult Collect(AwgSimulationRunResult run)
    {
        ArgumentNullException.ThrowIfNull(run);
        var request = new SimulationRequest
        {
            Graph = run.BuiltSystem.Graph,
            StartTimeUtc = run.Options.StartTimeUtc,
            Duration = run.Options.Duration,
            TimeStep = run.Options.TimeStep,
            ExternalInputs = run.BuiltSystem.ExternalInputs,
            Loops = run.BuiltSystem.Loops
        };

        var result = SimulationResultCollector.Collect(run.EngineResult, request);
        var scalars = new Dictionary<string, double>(result.Summary.ScalarMetrics, StringComparer.Ordinal)
        {
            ["balance.water.maximumAbsoluteResidualKg"] = run.BalanceReport.MaxAbsWaterResidualKg,
            ["balance.energy.maximumAbsoluteResidualJ"] = run.BalanceReport.MaxAbsEnergyResidualJ,
            ["balance.dryAir.maximumAbsoluteResidualKg"] = run.BalanceReport.MaxAbsDryAirResidualKg
        };

        if (run.Summary.FinalWaterTankContentKg is { } tankKg)
        {
            scalars["water.collected.totalKg"] = tankKg;
            scalars["water.collected.totalLitersApprox"] = tankKg; // ρ≈1000 kg/m³ → L ≈ kg for liquid water
            var days = Math.Max(run.Options.Duration.TotalDays, 1e-12);
            scalars["water.production.averageKgPerDay"] = tankKg / days;
        }

        if (run.Summary.LitersPerDay is { } litersPerDay)
        {
            scalars["water.production.litersPerDay"] = litersPerDay;
        }

        if (run.Summary.ElectricEnergyConsumedJ is { } electricJ)
        {
            scalars["energy.electrical.totalJ"] = electricJ;
        }

        if (run.Summary.PeltierElectricalProxyEnergyJ is { } peltierJ)
        {
            scalars["energy.peltier.totalJ"] = peltierJ;
        }

        if (run.Summary.IncidentSolarEnergyJ is { } solarJ)
        {
            scalars["energy.solar.totalJ"] = solarJ;
        }

        if (run.Summary.WattHoursElectricPerLiter is { } whPerL)
        {
            scalars["efficiency.whPerLiterApprox"] = whPerL;
        }

        if (run.Summary.LitersPerKwhElectric is { } lPerKwhE)
        {
            scalars["kpi.litersPerKwhElectric"] = lPerKwhE;
        }

        if (run.Summary.LitersPerKwhSolarPrimary is { } lPerKwhS)
        {
            scalars["kpi.litersPerKwhSolarPrimary"] = lPerKwhS;
        }

        if (run.Summary.LitersPerDayPerSquareMeterAperture is { } lPerM2)
        {
            scalars["kpi.litersPerDayPerSquareMeterAperture"] = lPerM2;
        }

        if (run.Summary.WaterRecoveryFraction is { } recovery)
        {
            scalars["kpi.waterRecoveryFraction"] = recovery;
        }

        if (run.Summary.DesorptionCaptureFraction is { } capture)
        {
            scalars["kpi.desorptionCaptureFraction"] = capture;
        }

        if (run.Summary.CoolingPlantThermalInputJ is { } coolTherm)
        {
            scalars["energy.coolingPlant.thermalInputJ"] = coolTherm;
        }

        if (run.Summary.CoolingPlantElectricalEnergyJ is { } coolElec)
        {
            scalars["energy.coolingPlant.electricalJ"] = coolElec;
        }

        if (run.Summary.BareCoolingDeviceCOP is { } bareCop)
        {
            scalars["kpi.bareCoolingDeviceCOP"] = bareCop;
        }

        if (run.Summary.CoolingPlantCOP is { } plantCop)
        {
            scalars["kpi.coolingPlantCOP"] = plantCop;
        }

        if (run.Summary.AverageTemperatureLiftK is { } lift)
        {
            scalars["kpi.averageTemperatureLiftK"] = lift;
        }

        if (run.Summary.AverageDewPointMarginK is { } margin)
        {
            scalars["kpi.averageDewPointMarginK"] = margin;
        }

        return result with
        {
            Summary = result.Summary with { ScalarMetrics = scalars }
        };
    }

    public static SimulationResult ExportCsv(AwgSimulationRunResult run, string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var result = Collect(run);
        SimulationResultCsvExporter.ExportDirectory(result, directory, run.EngineResult);
        return result;
    }

    public static (SimulationResult Result, SimulationExportManifest Manifest) ExportBundle(
        AwgSimulationRunResult run,
        string directory,
        string? simulationId = null)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        var result = Collect(run);
        var jsonOptions = SimulationResultBundleExporter.CreateJsonOptions();
        var document = new AwgConfigurationDocument
        {
            System = run.BuiltSystem.Configuration,
            InitialState = run.BuiltSystem.InitialState
        };

        var additional = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["configuration.json"] = AwgConfigurationLoader.SaveToJson(document),
            ["awg-summary.json"] = JsonSerializer.Serialize(run.Summary, jsonOptions),
            ["balance-verification.json"] = JsonSerializer.Serialize(run.BalanceReport, jsonOptions)
        };

        if (run.Options.WeatherProvider is not null)
        {
            additional["weather-metadata.json"] = JsonSerializer.Serialize(
                run.Options.WeatherProvider.Metadata,
                jsonOptions);
        }

        var manifest = SimulationResultBundleExporter.ExportDirectory(
            result,
            directory,
            run.EngineResult,
            additional,
            simulationId);

        return (result, manifest);
    }
}
