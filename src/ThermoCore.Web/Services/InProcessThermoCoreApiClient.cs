using System.Text.Json;
using ThermoCore.Api;
using ThermoCore.Api.Contracts;
using ThermoCore.Api.Services;
using ThermoCore.AWG.Configuration;
using ThermoCore.AWG.Topology;

namespace ThermoCore.Web.Services;

/// <summary>
/// In-process API client for local Blazor hosting (calls the same application services as ThermoCore.Api).
/// </summary>
public sealed class InProcessThermoCoreApiClient : IThermoCoreApiClient
{
    private readonly PsychrometricApiService _psychrometrics;
    private readonly ConfigurationValidationService _validation;
    private readonly ISimulationJobStore _jobs;
    private readonly SimulationResultQueryService _results;

    public InProcessThermoCoreApiClient(
        PsychrometricApiService psychrometrics,
        ConfigurationValidationService validation,
        ISimulationJobStore jobs,
        SimulationResultQueryService results)
    {
        _psychrometrics = psychrometrics;
        _validation = validation;
        _jobs = jobs;
        _results = results;
    }

    public Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new HealthResponse
        {
            Status = "Healthy",
            ApplicationVersion = ApiApplicationVersions.ApplicationVersion,
            CoreVersion = ApiApplicationVersions.CoreVersion,
            TimestampUtc = DateTimeOffset.UtcNow
        });

    public Task<PsychrometricCalculateResponse> CalculatePsychrometricsAsync(
        PsychrometricCalculateRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_psychrometrics.Calculate(request));

    public Task<ConfigurationValidateResponse> ValidateConfigurationAsync(
        AwgConfigurationDocument configuration,
        CancellationToken cancellationToken = default)
    {
        // Serialize without SaveToJson validation so ConfigurationValidationService owns the report.
        var json = JsonSerializer.Serialize(configuration, AwgConfigurationLoader.CreateSerializerOptions());
        using var document = JsonDocument.Parse(json);
        return Task.FromResult(_validation.Validate(document.RootElement));
    }

    public Task<CreateSimulationResponse> CreateSimulationAsync(
        CreateSimulationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(_jobs.Enqueue(request));
        }
        catch (AwgConfigurationException ex)
        {
            throw new InvalidOperationException(ex.Message, ex);
        }
    }

    public Task<SimulationStatusResponse?> GetSimulationAsync(
        string simulationId,
        CancellationToken cancellationToken = default)
    {
        var job = _jobs.Get(simulationId);
        if (job is null)
        {
            return Task.FromResult<SimulationStatusResponse?>(null);
        }

        return Task.FromResult<SimulationStatusResponse?>(ToStatus(job));
    }

    public Task<bool> CancelSimulationAsync(
        string simulationId,
        CancellationToken cancellationToken = default)
    {
        var cancelled = _jobs.TryCancel(simulationId, out _);
        return Task.FromResult(cancelled);
    }

    public Task<SimulationSummaryResponse?> GetSummaryAsync(
        string simulationId,
        CancellationToken cancellationToken = default)
    {
        var job = _jobs.Get(simulationId);
        if (job?.RunResult is null
            || job.Status is not (SimulationJobStatus.Completed or SimulationJobStatus.Failed))
        {
            return Task.FromResult<SimulationSummaryResponse?>(null);
        }

        var run = job.RunResult;
        return Task.FromResult<SimulationSummaryResponse?>(new SimulationSummaryResponse
        {
            SimulationId = job.SimulationId,
            Status = job.Status.ToString(),
            Succeeded = run.Summary.Succeeded,
            TopologyId = run.Summary.TopologyId,
            CompletedSteps = run.Summary.CompletedSteps,
            AggregatedEnergyResidualJ = run.Summary.AggregatedEnergyResidualJ,
            AggregatedWaterResidualKg = run.Summary.AggregatedWaterResidualKg,
            AggregatedDryAirResidualKg = run.Summary.AggregatedDryAirResidualKg,
            WaterBalancePassed = run.BalanceReport.WaterBalancePassed,
            EnergyBalancePassed = run.BalanceReport.EnergyBalancePassed,
            WarningCount = run.Summary.WarningCount,
            ErrorCount = run.Summary.ErrorCount,
            FinalWaterTankContentKg = run.Summary.FinalWaterTankContentKg,
            FinalBusPowerW = run.Summary.FinalBusPowerW
        });
    }

    public Task<SimulationSeriesResponse?> GetSeriesAsync(
        string simulationId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var lookup = _results.TryGetCompletedJob(simulationId);
        if (!lookup.Succeeded)
        {
            return Task.FromResult<SimulationSeriesResponse?>(null);
        }

        return Task.FromResult<SimulationSeriesResponse?>(
            _results.BuildSeries(lookup.Job!, channels: null, page, pageSize));
    }

    public Task<SimulationDiagnosticsResponse?> GetDiagnosticsAsync(
        string simulationId,
        CancellationToken cancellationToken = default)
    {
        var lookup = _results.TryGetCompletedJob(simulationId);
        if (!lookup.Succeeded)
        {
            return Task.FromResult<SimulationDiagnosticsResponse?>(null);
        }

        return Task.FromResult<SimulationDiagnosticsResponse?>(
            _results.BuildDiagnostics(lookup.Job!, null, null, null, null, null));
    }

    public Task<(byte[] Content, string ContentType, string FileName)?> ExportAsync(
        string simulationId,
        string format,
        CancellationToken cancellationToken = default)
    {
        var lookup = _results.TryGetCompletedJob(simulationId);
        if (!lookup.Succeeded)
        {
            return Task.FromResult<(byte[] Content, string ContentType, string FileName)?>(null);
        }

        return Task.FromResult<(byte[] Content, string ContentType, string FileName)?>(
            _results.BuildExport(lookup.Job!, format));
    }

    private static SimulationStatusResponse ToStatus(SimulationJob job)
        => new()
        {
            SimulationId = job.SimulationId,
            Status = job.Status.ToString(),
            ProgressFraction = job.ProgressFraction,
            CompletedSteps = job.CompletedSteps,
            TotalSteps = job.TotalSteps,
            SimulationTimeUtc = job.SimulationTimeUtc,
            StartedAtUtc = job.StartedAtUtc,
            CompletedAtUtc = job.CompletedAtUtc,
            ErrorMessage = job.ErrorMessage
        };
}
