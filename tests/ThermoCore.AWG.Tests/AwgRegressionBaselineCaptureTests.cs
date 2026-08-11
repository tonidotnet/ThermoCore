using ThermoCore.AWG.Regression;

namespace ThermoCore.AWG.Tests;

public class AwgRegressionBaselineCaptureTests
{
    [Fact]
    public void CaptureDefaultSuite_ContainsCanonicalScenarioIds_AndAllPass()
    {
        var document = AwgRegressionBaselineCapture.CaptureDefaultSuite(gitCommitSha: "test");
        Assert.Equal("R0-001", document.TaskId);
        Assert.Equal("doc-022-default", document.SuiteId);
        Assert.Equal(document.ScenarioCount, document.PassedCount);
        Assert.Equal(0, document.FailedCount);
        Assert.Contains(document.Scenarios, s => s.ScenarioId == "no-recirculation");
        Assert.Contains(document.Scenarios, s => s.ScenarioId == "warm-humid-day");
        Assert.Contains(document.Scenarios, s => s.ScenarioId == "pv-rear-air");
        Assert.All(document.Scenarios, s =>
        {
            Assert.True(s.Passed, $"{s.ScenarioId}: {string.Join("; ", s.Failures)}");
            Assert.False(string.IsNullOrWhiteSpace(s.GraphFingerprint));
            Assert.True(s.CompletedSteps > 0);
        });
    }

    [Fact]
    public void BaselineDocument_RoundTripsThroughJson()
    {
        var directory = Path.Combine(Path.GetTempPath(), "awg-baseline-" + Guid.NewGuid().ToString("N"));
        try
        {
            var original = AwgRegressionBaselineCapture.CaptureDefaultSuite("roundtrip");
            AwgRegressionBaselineCapture.Write(original, directory);
            var loaded = AwgRegressionBaselineCapture.Load(
                Path.Combine(directory, AwgRegressionBaselineCapture.DefaultBaselineFileName));
            Assert.Equal(original.SuiteId, loaded.SuiteId);
            Assert.Equal(original.ScenarioCount, loaded.ScenarioCount);
            Assert.Equal(
                original.Scenarios.Select(s => s.ScenarioId),
                loaded.Scenarios.Select(s => s.ScenarioId));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
