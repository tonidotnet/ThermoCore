using ThermoCore.Api.Contracts;

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

        return app;
    }
}
