namespace ThermoCore.AWG.Regression;

/// <summary>Machine-readable pre-research AWG regression baseline document (R0-001).</summary>
public sealed record AwgRegressionBaselineDocument
{
    public required string TaskId { get; init; }

    public required string SuiteId { get; init; }

    public required string SuiteDescription { get; init; }

    public required DateTimeOffset CapturedUtc { get; init; }

    public string? GitCommitSha { get; init; }

    public required int ScenarioCount { get; init; }

    public required int PassedCount { get; init; }

    public required int FailedCount { get; init; }

    public required IReadOnlyList<AwgRegressionBaselineScenarioEntry> Scenarios { get; init; }

    public required IReadOnlyList<string> CaptureCommands { get; init; }
}
