using ThermoCore.AWG.Configuration;
using ThermoCore.AWG.Simulation;

namespace ThermoCore.Api.Services;

/// <summary>In-memory AWG simulation job state.</summary>
public sealed class SimulationJob
{
    public required string SimulationId { get; init; }

    public required AwgConfigurationDocument Configuration { get; init; }

    public required AwgSimulationOptions Options { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public SimulationJobStatus Status { get; set; } = SimulationJobStatus.Queued;

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public int CompletedSteps { get; set; }

    public int TotalSteps { get; set; }

    public DateTimeOffset? SimulationTimeUtc { get; set; }

    public string? ErrorMessage { get; set; }

    public AwgSimulationRunResult? RunResult { get; set; }

    public CancellationTokenSource Cancellation { get; } = new();

    public double ProgressFraction
        => TotalSteps <= 0 ? 0.0 : Math.Clamp(CompletedSteps / (double)TotalSteps, 0.0, 1.0);
}
