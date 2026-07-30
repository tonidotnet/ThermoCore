using System.Globalization;
using System.Text;
using ThermoCore.AWG.Calibration;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Calibration;

namespace ThermoCore.AWG.Tests;

public class AwgParameterCalibrationTests
{
    [Fact]
    public void Calibrate_CondenserBypass_ImprovesObjectiveTowardTruth()
    {
        var truthBypass = 0.25;
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false);
        configuration = configuration with
        {
            Condenser = configuration.Condenser with { BypassFactor = truthBypass }
        };
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
        var options = AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));

        var truthRun = new AwgSimulationRunner().Run(configuration, initial, options);
        Assert.True(truthRun.EngineResult.Succeeded);
        var collected = AwgResultExporter.Collect(truthRun);
        var channel = collected.Channels.First(c =>
            c.Definition.Id.Contains("condenser.outlet.temperature", StringComparison.Ordinal));

        var csv = BuildMeasurementCsv(options.StartTimeUtc, options.TimeStep, channel.Definition.Id, channel.Definition.Unit, channel.Values);
        var measurements = MeasurementCsvImporter.Import(csv, "synthetic-condenser");

        var wrongStart = configuration with
        {
            Condenser = configuration.Condenser with { BypassFactor = 0.45 }
        };
        var parameters = AwgCalibratableParameterCatalog.Select(
            wrongStart,
            [AwgCalibratableParameterIds.CondenserBypassFactor]);

        var result = new AwgParameterCalibrationRunner().Calibrate(
            measurements,
            wrongStart,
            initial,
            options,
            parameters,
            maximumPasses: 2,
            maximumEvaluationsPerParameter: 14);

        Assert.True(result.Fitting.Improved || result.Fitting.FinalObjective <= result.Fitting.InitialObjective);
        Assert.True(
            result.FittedReport.OverallRmse <= result.BaselineReport.OverallRmse + 1e-9,
            $"fitted={result.FittedReport.OverallRmse} baseline={result.BaselineReport.OverallRmse}");
        Assert.InRange(
            result.Fitting.FittedValues[AwgCalibratableParameterIds.CondenserBypassFactor],
            0.15,
            0.40);
    }

    private static string BuildMeasurementCsv(
        DateTimeOffset start,
        TimeSpan dt,
        string channelId,
        string unit,
        IReadOnlyList<double> values)
    {
        var sb = new StringBuilder();
        sb.AppendLine(MeasurementCsvSchema.HeaderLine);
        for (var i = 0; i < values.Count; i++)
        {
            var t = start + TimeSpan.FromSeconds(dt.TotalSeconds * i);
            sb.Append(t.ToString("O", CultureInfo.InvariantCulture)).Append(',')
                .Append(channelId).Append(',')
                .Append(values[i].ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                .Append(unit)
                .AppendLine();
        }

        return sb.ToString();
    }
}
