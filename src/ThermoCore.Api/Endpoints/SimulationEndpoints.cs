using ThermoCore.Api.Contracts;
using ThermoCore.Api.Services;
using ThermoCore.AWG.Topology;

namespace ThermoCore.Api.Endpoints;

public static class SimulationEndpoints
{
    public static IEndpointRouteBuilder MapSimulationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/simulations", (
                CreateSimulationRequest request,
                ISimulationJobStore store,
                HttpRequest http) =>
            {
                try
                {
                    string? idempotencyKey = null;
                    if (http.Headers.TryGetValue("Idempotency-Key", out var keys)
                        && !string.IsNullOrWhiteSpace(keys.FirstOrDefault()))
                    {
                        idempotencyKey = keys.ToString().Trim();
                    }

                    var created = store.Enqueue(request, idempotencyKey);
                    return Results.Accepted($"/api/v1/simulations/{created.SimulationId}", created);
                }
                catch (AwgConfigurationException ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Configuration error",
                        type: "https://thermocore.local/errors/configuration",
                        extensions: new Dictionary<string, object?>
                        {
                            ["errors"] = ex.Diagnostics.Select(d => new ValidationIssueDto
                            {
                                Path = d.ComponentId ?? "configuration",
                                Code = d.Code,
                                Message = d.Message
                            }).ToArray()
                        });
                }
                catch (ArgumentException ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Validation error",
                        type: "https://thermocore.local/errors/validation");
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Resource limit",
                        type: "https://thermocore.local/errors/conflict");
                }
            })
            .WithName("CreateSimulation")
            .WithTags("Simulations");

        app.MapGet("/api/v1/simulations", (ISimulationJobStore store) =>
            {
                var jobs = store.List()
                    .Select(ToStatus)
                    .ToArray();
                return Results.Ok(new SimulationListResponse { Simulations = jobs });
            })
            .WithName("ListSimulations")
            .WithTags("Simulations");

        app.MapGet("/api/v1/simulations/{simulationId}", (string simulationId, ISimulationJobStore store) =>
            {
                var job = store.Get(simulationId);
                return job is null
                    ? Results.NotFound()
                    : Results.Ok(ToStatus(job));
            })
            .WithName("GetSimulation")
            .WithTags("Simulations");

        app.MapPost("/api/v1/simulations/{simulationId}/cancel", (string simulationId, ISimulationJobStore store) =>
            {
                var job = store.Get(simulationId);
                if (job is null)
                {
                    return Results.NotFound();
                }

                _ = store.TryCancel(simulationId, out var conflict);
                if (conflict is not null
                    && job.Status is SimulationJobStatus.Completed or SimulationJobStatus.Failed or SimulationJobStatus.Cancelled)
                {
                    return Results.Conflict(new { detail = conflict });
                }

                return Results.Accepted($"/api/v1/simulations/{simulationId}", ToStatus(store.Get(simulationId)!));
            })
            .WithName("CancelSimulation")
            .WithTags("Simulations");

        app.MapGet("/api/v1/simulations/{simulationId}/summary", (string simulationId, ISimulationJobStore store) =>
            {
                var job = store.Get(simulationId);
                if (job is null)
                {
                    return Results.NotFound();
                }

                if (job.Status is not (SimulationJobStatus.Completed or SimulationJobStatus.Failed)
                    || job.RunResult is null)
                {
                    return Results.Conflict(new
                    {
                        detail = $"Summary is available only after completion. Current status: {job.Status}."
                    });
                }

                return Results.Ok(SimulationSummaryMapper.FromJob(job));
            })
            .WithName("GetSimulationSummary")
            .WithTags("Simulations");

        app.MapGet("/api/v1/simulations/{simulationId}/series", (
                string simulationId,
                SimulationResultQueryService results,
                string? channels = null,
                int page = 1,
                int pageSize = 50,
                DateTimeOffset? from = null,
                DateTimeOffset? to = null,
                double? intervalSeconds = null) =>
            {
                var lookup = results.TryGetCompletedJob(simulationId);
                return ToHttpResult(lookup)
                    ?? Results.Ok(results.BuildSeries(
                        lookup.Job!,
                        channels,
                        page,
                        pageSize,
                        from,
                        to,
                        intervalSeconds));
            })
            .WithName("GetSimulationSeries")
            .WithTags("Simulations");

        app.MapGet("/api/v1/simulations/{simulationId}/diagnostics", (
                string simulationId,
                SimulationResultQueryService results,
                string? severity = null,
                string? componentId = null,
                string? code = null,
                int? fromStep = null,
                int? toStep = null) =>
            {
                var lookup = results.TryGetCompletedJob(simulationId);
                return ToHttpResult(lookup)
                    ?? Results.Ok(results.BuildDiagnostics(lookup.Job!, severity, componentId, code, fromStep, toStep));
            })
            .WithName("GetSimulationDiagnostics")
            .WithTags("Simulations");

        app.MapGet("/api/v1/simulations/{simulationId}/export", (
                string simulationId,
                SimulationResultQueryService results,
                string format = "json") =>
            {
                var lookup = results.TryGetCompletedJob(simulationId);
                var error = ToHttpResult(lookup);
                if (error is not null)
                {
                    return error;
                }

                try
                {
                    var (content, contentType, fileName) = results.BuildExport(lookup.Job!, format);
                    return Results.File(content, contentType, fileName);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { detail = ex.Message });
                }
            })
            .WithName("ExportSimulation")
            .WithTags("Simulations");

        return app;
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

    private static IResult? ToHttpResult(CompletedJobLookupResult lookup)
        => lookup.StatusCode switch
        {
            404 => Results.NotFound(),
            409 => Results.Conflict(new { detail = lookup.Detail }),
            _ => null
        };
}
