using System.Text.Json;

namespace ThermoCore.AWG.Regression;

/// <summary>Captures compact regression metrics for later research PR comparison (R0-001).</summary>
public static class AwgRegressionBaselineCapture
{
    public const string DefaultBaselineDirectory = "samples/baselines/r0-001";

    public const string DefaultBaselineFileName = "baseline.json";

    public static AwgRegressionBaselineDocument Capture(
        string suiteId,
        string suiteDescription,
        IReadOnlyList<AwgRegressionScenario> scenarios,
        IReadOnlyList<string> captureCommands,
        string? gitCommitSha = null,
        AwgRegressionScenarioRunner? runner = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteId);
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteDescription);
        ArgumentNullException.ThrowIfNull(scenarios);
        ArgumentNullException.ThrowIfNull(captureCommands);

        runner ??= new AwgRegressionScenarioRunner();
        var results = runner.RunAll(scenarios);
        var entries = results.Select(ToEntry).OrderBy(e => e.ScenarioId, StringComparer.Ordinal).ToArray();
        var passed = entries.Count(e => e.Passed);

        return new AwgRegressionBaselineDocument
        {
            TaskId = "R0-001",
            SuiteId = suiteId,
            SuiteDescription = suiteDescription,
            CapturedUtc = DateTimeOffset.UtcNow,
            GitCommitSha = gitCommitSha,
            ScenarioCount = entries.Length,
            PassedCount = passed,
            FailedCount = entries.Length - passed,
            Scenarios = entries,
            CaptureCommands = captureCommands.ToArray()
        };
    }

    public static AwgRegressionBaselineDocument CaptureDefaultSuite(string? gitCommitSha = null)
        => Capture(
            suiteId: "doc-022-default",
            suiteDescription: "DOC-022 / APP-006 default AWG regression scenarios (CreateDefaultScenarios).",
            scenarios: AwgRegressionScenarioCatalog.CreateDefaultScenarios(),
            captureCommands:
            [
                "dotnet build ThermoCore.slnx -nologo",
                "dotnet run --project src/ThermoCore.Console -- capture-baseline",
                "dotnet run --project src/ThermoCore.Console -- regress",
                "dotnet test tests/ThermoCore.AWG.Tests --filter FullyQualifiedName~AwgRegressionAndPvRearAirTests"
            ],
            gitCommitSha: gitCommitSha);

    public static AwgRegressionBaselineDocument CaptureDrySunnyMatrixSuite(string? gitCommitSha = null)
        => Capture(
            suiteId: "dry-sunny-matrix",
            suiteDescription: "Dry-sunny T×silica matrix (CreateDrySunnyMatrixScenarios).",
            scenarios: AwgRegressionScenarioCatalog.CreateDrySunnyMatrixScenarios(),
            captureCommands:
            [
                "dotnet run --project src/ThermoCore.Console -- capture-baseline --suite dry-sunny-matrix",
                "dotnet run --project src/ThermoCore.Console -- regress --dir samples/scenarios/dry-sunny-matrix",
                "dotnet test tests/ThermoCore.AWG.Tests --filter FullyQualifiedName~DrySunnyMatrixScenarios_AllPass"
            ],
            gitCommitSha: gitCommitSha);

    public static void Write(AwgRegressionBaselineDocument document, string directory, string fileName = DefaultBaselineFileName)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        var json = JsonSerializer.Serialize(document, AwgRegressionScenarioCatalog.CreateSerializerOptions());
        File.WriteAllText(path, json);
    }

    public static AwgRegressionBaselineDocument Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<AwgRegressionBaselineDocument>(
            json,
            AwgRegressionScenarioCatalog.CreateSerializerOptions());
        return document
            ?? throw new InvalidOperationException($"Failed to deserialize baseline document from '{path}'.");
    }

    private static AwgRegressionBaselineScenarioEntry ToEntry(AwgRegressionScenarioResult result)
    {
        var summary = result.Run.Summary;
        return new AwgRegressionBaselineScenarioEntry
        {
            ScenarioId = result.Scenario.Id,
            Description = result.Scenario.Description,
            Passed = result.Passed,
            CompletedSteps = summary.CompletedSteps,
            DurationSeconds = summary.Duration.TotalSeconds,
            TimeStepSeconds = summary.TimeStep.TotalSeconds,
            TopologyId = summary.TopologyId,
            TopologyVersion = summary.TopologyVersion,
            GraphFingerprint = summary.GraphFingerprint,
            SimulationSucceeded = summary.Succeeded,
            BalanceAllPassed = result.Run.BalanceReport.AllPassed,
            AggregatedEnergyResidualJ = summary.AggregatedEnergyResidualJ,
            AggregatedWaterResidualKg = summary.AggregatedWaterResidualKg,
            AggregatedDryAirResidualKg = summary.AggregatedDryAirResidualKg,
            FinalWaterTankContentKg = summary.FinalWaterTankContentKg,
            FinalBusPowerW = summary.FinalBusPowerW,
            FinalBatteryStateOfChargeFraction = summary.FinalBatteryStateOfChargeFraction,
            SolarUtilizationFraction = summary.SolarUtilizationFraction,
            Failures = result.Failures.ToArray()
        };
    }
}
