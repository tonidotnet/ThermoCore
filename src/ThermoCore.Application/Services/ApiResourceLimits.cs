namespace ThermoCore.Api.Services;

/// <summary>Configurable API resource limits (API-009 / DOC-019 §23).</summary>
public sealed class ApiResourceLimits
{
    public double MaximumDurationSeconds { get; set; } = 86_400.0;

    public double MinimumTimeStepSeconds { get; set; } = 0.1;

    public int MaximumStepCount { get; set; } = 50_000;

    public int MaximumConcurrentJobs { get; set; } = 4;

    public int MaximumResultChannels { get; set; } = 256;

    public int MaximumDiagnosticsReturned { get; set; } = 5_000;

    public static ApiResourceLimits Default { get; } = new();
}
