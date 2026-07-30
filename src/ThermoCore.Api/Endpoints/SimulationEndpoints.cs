using ThermoCore.Api.Contracts;
using ThermoCore.Api.Services;
using ThermoCore.AWG.Topology;

namespace ThermoCore.Api.Endpoints;

public static class SimulationEndpoints
{
    public static IEndpointRouteBuilder MapSimulationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/simulations", (CreateSimulationRequest request, ISimulationJobStore store) =>
            {
                try
                {
                    var created = store.Enqueue(request);
                    return Results.Accepted($"/api/v1/simulations/{created.SimulationId}", created);
                }
                catch (AwgConfigurationException ex)
                {
                    return Results.BadRequest(new
                    {
                        title = "Configuration error",
                        status = 400,
                        detail = ex.Message,
                        errors = ex.Diagnostics.Select(d => new ValidationIssueDto
                        {
                            Path = d.ComponentId ?? "configuration",
                            Code = d.Code,
                            Message = d.Message
                        }).ToArray()
                    });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new
                    {
                        title = "Validation error",
                        status = 400,
                        detail = ex.Message
                    });
                }
            })
            .WithName("CreateSimulation")
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

                var run = job.RunResult;
                return Results.Ok(new SimulationSummaryResponse
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
            })
            .WithName("GetSimulationSummary")
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
}
