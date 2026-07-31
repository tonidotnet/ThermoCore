using System.Globalization;
using System.Text;
using ThermoCore.AWG.Calibration;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Calibration;

namespace ThermoCore.AWG.Tests;

public class AwgHoldoutValidationTests
{
    [Fact]
    public void Holdout_CondenserBypass_ScoresFittedModelOnLaterWindow()
    {
        var truthBypass = 0.25;
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false);
        configuration = configuration with
        {
            Condenser = configuration.Condenser with { BypassFactor = truthBypass }
        };
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
        var options = AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(1));

        var truthRun = new AwgSimulationRunner().Run(configuration, initial, options);
        Assert.True(truthRun.EngineResult.Succeeded);
        var collected = AwgResultExporter.Collect(truthRun);
        var channel = collected.Channels.First(c =>
            c.Definition.Id.Contains("condenser.outlet.temperature", StringComparison.Ordinal));

        var csv = BuildMeasurementCsv(
            options.StartTimeUtc,
            options.TimeStep,
            channel.Definition.Id,
            channel.Definition.Unit,
            channel.Values);
        var measurements = MeasurementCsvImporter.Import(csv, "synthetic-holdout");

        var wrongStart = configuration with
        {
            Condenser = configuration.Condenser with { BypassFactor = 0.45 }
        };
        var parameters = AwgCalibratableParameterCatalog.Select(
            wrongStart,
            [AwgCalibratableParameterIds.CondenserBypassFactor]);

        var result = new AwgHoldoutValidationRunner().Validate(
            measurements,
            wrongStart,
            initial,
            options,
            trainFraction: 0.625,
            parameters,
            maximumPasses: 2,
            maximumEvaluationsPerParameter: 12);

        Assert.True(result.Split.HoldoutTimestampCount >= 1);
        Assert.True(result.Split.TrainTimestampCount >= 1);
        Assert.False(double.IsNaN(result.HoldoutFittedReport.OverallRmse));
        Assert.True(
            result.Training.FittedReport.OverallRmse <= result.Training.BaselineReport.OverallRmse + 1e-9,
            $"train fitted={result.Training.FittedReport.OverallRmse} baseline={result.Training.BaselineReport.OverallRmse}");
        // Synthetic noiseless series: both holdout RMSEs sit near machine/noise floor; require absolute quality.
        Assert.True(
            result.HoldoutFittedReport.OverallRmse < 1e-3,
            $"holdout fitted RMSE too large: {result.HoldoutFittedReport.OverallRmse}");
        Assert.InRange(
            result.Training.Fitting.FittedValues[AwgCalibratableParameterIds.CondenserBypassFactor],
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
