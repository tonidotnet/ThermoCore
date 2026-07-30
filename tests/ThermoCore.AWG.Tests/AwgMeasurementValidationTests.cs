using ThermoCore.AWG.Calibration;

namespace ThermoCore.AWG.Tests;

public class AwgMeasurementValidationTests
{
    [Fact]
    public void Validate_AmbientSmokeCsv_NearZeroRmse()
    {
        var path = FindRepoFile(Path.Combine("samples", "calibration", "awg-mvp-ambient-smoke.csv"));
        Assert.True(File.Exists(path), $"Missing sample dataset at {path}");

        var result = new AwgMeasurementValidationRunner().ValidateFromFiles(
            path,
            configurationPath: null,
            durationSeconds: 3,
            timeStepSeconds: 1);

        Assert.True(
            result.Run.EngineResult.Succeeded,
            string.Join("; ", result.Run.EngineResult.Diagnostics.Select(d => $"{d.Code}:{d.Message}")));
        Assert.True(result.Report.Channels.Count >= 3);
        Assert.Empty(result.Report.MissingChannels);
        Assert.True(result.Report.OverallRmse < 1e-6, $"Overall RMSE was {result.Report.OverallRmse}");
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            if (File.Exists(Path.Combine(dir.FullName, "ThermoCore.sln")))
            {
                return Path.Combine(dir.FullName, relativePath);
            }

            dir = dir.Parent;
        }

        return Path.GetFullPath(relativePath);
    }
}
