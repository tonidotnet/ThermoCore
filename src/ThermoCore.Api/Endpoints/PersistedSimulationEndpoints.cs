using ThermoCore.Api.Contracts;
using ThermoCore.Api.Services;

namespace ThermoCore.Api.Endpoints;

public static class PersistedSimulationEndpoints
{
    public static IEndpointRouteBuilder MapPersistedSimulationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/persisted/simulations", (PersistedSimulationQueryService query, int take = 50) =>
            {
                take = Math.Clamp(take, 1, 500);
                return Results.Ok(query.List(take));
            })
            .WithName("ListPersistedSimulations")
            .WithTags("PersistedSimulations");

        app.MapGet("/api/v1/persisted/simulations/{summaryId}/summary", (
                string summaryId,
                PersistedSimulationQueryService query) =>
            {
                if (!Guid.TryParseExact(summaryId, "N", out var id)
                    && !Guid.TryParse(summaryId, out id))
                {
                    return Results.BadRequest(new { detail = "Invalid summary id." });
                }

                var summary = query.GetSummary(id);
                return summary is null ? Results.NotFound() : Results.Ok(summary);
            })
            .WithName("GetPersistedSimulationSummary")
            .WithTags("PersistedSimulations");

        app.MapGet("/api/v1/persisted/simulations/compare", (
                string a,
                string b,
                PersistedSimulationQueryService query,
                SimulationCompareService compare) =>
            {
                if ((!Guid.TryParseExact(a, "N", out var idA) && !Guid.TryParse(a, out idA))
                    || (!Guid.TryParseExact(b, "N", out var idB) && !Guid.TryParse(b, out idB)))
                {
                    return Results.BadRequest(new { detail = "Query parameters a and b must be summary GUIDs." });
                }

                if (idA == idB)
                {
                    return Results.BadRequest(new { detail = "Select two different simulations." });
                }

                var summaryA = query.GetSummary(idA);
                var summaryB = query.GetSummary(idB);
                if (summaryA is null || summaryB is null)
                {
                    return Results.NotFound(new { detail = "One or both persisted summaries were not found." });
                }

                return Results.Ok(compare.Compare(summaryA, summaryB));
            })
            .WithName("ComparePersistedSimulations")
            .WithTags("PersistedSimulations");

        return app;
    }
}
