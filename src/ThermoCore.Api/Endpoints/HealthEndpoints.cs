using ThermoCore.Api.Contracts;
using ThermoCore.Persistence;

namespace ThermoCore.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/health", () => Results.Ok(new HealthResponse
            {
                Status = "Healthy",
                ApplicationVersion = ApiApplicationVersions.ApplicationVersion,
                CoreVersion = ApiApplicationVersions.CoreVersion,
                TimestampUtc = DateTimeOffset.UtcNow
            }))
            .WithName("GetHealth")
            .WithTags("Health");

        app.MapGet("/health/live", () => Results.Ok(new { status = "Live" }))
            .WithName("GetLiveness")
            .WithTags("Health");

        app.MapGet("/health/ready", (IThermoCoreStore store) =>
            {
                try
                {
                    store.EnsureCreated();
                    _ = store.ListSimulationSummaries(1);
                    return Results.Ok(new
                    {
                        status = "Ready",
                        persistence = "ok",
                        timestampUtc = DateTimeOffset.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    return Results.Json(
                        new
                        {
                            status = "NotReady",
                            persistence = "error",
                            detail = ex.Message,
                            timestampUtc = DateTimeOffset.UtcNow
                        },
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }
            })
            .WithName("GetReadiness")
            .WithTags("Health");

        return app;
    }
}
