using System.Collections.Concurrent;
using ThermoCore.Api.Contracts;
using ThermoCore.AWG.Simulation;

namespace ThermoCore.Api.Services;

/// <summary>Process-local simulation job queue (API-005/API-009 MVP).</summary>
public sealed class InMemorySimulationJobStore : ISimulationJobStore
{
    private readonly ConcurrentDictionary<string, SimulationJob> _jobs = new(StringComparer.Ordinal);
    private readonly AwgSimulationRunner _runner = new();
    private readonly ApiResourceLimits _limits;
    private int _activeJobs;

    public InMemorySimulationJobStore(ApiResourceLimits limits)
    {
        _limits = limits ?? ApiResourceLimits.Default;
    }

    public CreateSimulationResponse Enqueue(CreateSimulationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Configuration);

        ValidateResourceLimits(request);

        request.Configuration.System.Validate();
        request.Configuration.InitialState.Validate(request.Configuration.System);

        if (Volatile.Read(ref _activeJobs) >= _limits.MaximumConcurrentJobs)
        {
            throw new InvalidOperationException(
                $"Maximum concurrent jobs ({_limits.MaximumConcurrentJobs}) reached. Try again later.");
        }

        var options = new AwgSimulationOptions
        {
            StartTimeUtc = request.StartTimeUtc ?? DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(request.DurationSeconds),
            TimeStep = TimeSpan.FromSeconds(request.TimeStepSeconds)
        }.Validate();

        var job = new SimulationJob
        {
            SimulationId = Guid.NewGuid().ToString("N"),
            Configuration = request.Configuration,
            Options = options,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            TotalSteps = (int)Math.Ceiling(options.Duration.TotalSeconds / options.TimeStep.TotalSeconds)
        };

        if (!_jobs.TryAdd(job.SimulationId, job))
        {
            throw new InvalidOperationException("Failed to register simulation job.");
        }

        Interlocked.Increment(ref _activeJobs);
        _ = Task.Run(() => Execute(job), CancellationToken.None);

        return new CreateSimulationResponse
        {
            SimulationId = job.SimulationId,
            Status = job.Status.ToString(),
            CreatedAtUtc = job.CreatedAtUtc
        };
    }

    public SimulationJob? Get(string simulationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(simulationId);
        return _jobs.TryGetValue(simulationId, out var job) ? job : null;
    }

    public bool TryCancel(string simulationId, out string? conflictReason)
    {
        conflictReason = null;
        var job = Get(simulationId);
        if (job is null)
        {
            return false;
        }

        lock (job)
        {
            if (job.Status is SimulationJobStatus.Completed or SimulationJobStatus.Failed or SimulationJobStatus.Cancelled)
            {
                conflictReason = $"Simulation already {job.Status}.";
                return true;
            }

            job.Cancellation.Cancel();
            if (job.Status == SimulationJobStatus.Queued)
            {
                job.Status = SimulationJobStatus.Cancelled;
                job.CompletedAtUtc = DateTimeOffset.UtcNow;
            }
        }

        return true;
    }

    private void ValidateResourceLimits(CreateSimulationRequest request)
    {
        if (request.DurationSeconds <= 0.0 || request.TimeStepSeconds <= 0.0)
        {
            throw new ArgumentException("Duration and timestep must be positive.");
        }

        if (request.DurationSeconds > _limits.MaximumDurationSeconds)
        {
            throw new ArgumentException(
                $"Duration exceeds maximum of {_limits.MaximumDurationSeconds} seconds.");
        }

        if (request.TimeStepSeconds < _limits.MinimumTimeStepSeconds)
        {
            throw new ArgumentException(
                $"Timestep must be at least {_limits.MinimumTimeStepSeconds} seconds.");
        }

        var steps = (int)Math.Ceiling(request.DurationSeconds / request.TimeStepSeconds);
        if (steps > _limits.MaximumStepCount)
        {
            throw new ArgumentException(
                $"Requested step count {steps} exceeds maximum of {_limits.MaximumStepCount}.");
        }
    }

    private void Execute(SimulationJob job)
    {
        try
        {
            lock (job)
            {
                if (job.Status == SimulationJobStatus.Cancelled)
                {
                    return;
                }

                job.Status = SimulationJobStatus.Running;
                job.StartedAtUtc = DateTimeOffset.UtcNow;
            }

            try
            {
                var result = _runner.Run(
                    job.Configuration.System,
                    job.Configuration.InitialState,
                    job.Options,
                    job.Cancellation.Token);

                lock (job)
                {
                    if (job.Cancellation.IsCancellationRequested)
                    {
                        job.Status = SimulationJobStatus.Cancelled;
                    }
                    else
                    {
                        job.RunResult = result;
                        job.CompletedSteps = result.EngineResult.Steps.Count;
                        job.Status = result.EngineResult.Succeeded
                            ? SimulationJobStatus.Completed
                            : SimulationJobStatus.Failed;
                        if (!result.EngineResult.Succeeded)
                        {
                            job.ErrorMessage = string.Join(
                                "; ",
                                result.EngineResult.Diagnostics
                                    .Where(d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error)
                                    .Select(d => $"{d.Code}:{d.Message}")
                                    .Take(5));
                        }
                    }

                    job.CompletedAtUtc = DateTimeOffset.UtcNow;
                    if (result.EngineResult.Steps.Count > 0)
                    {
                        var last = result.EngineResult.Steps[^1];
                        job.SimulationTimeUtc = job.Options.StartTimeUtc + last.ElapsedTime + job.Options.TimeStep;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                lock (job)
                {
                    job.Status = SimulationJobStatus.Cancelled;
                    job.CompletedAtUtc = DateTimeOffset.UtcNow;
                }
            }
            catch (Exception ex)
            {
                lock (job)
                {
                    job.Status = SimulationJobStatus.Failed;
                    job.ErrorMessage = ex.Message;
                    job.CompletedAtUtc = DateTimeOffset.UtcNow;
                }
            }
        }
        finally
        {
            Interlocked.Decrement(ref _activeJobs);
        }
    }
}
