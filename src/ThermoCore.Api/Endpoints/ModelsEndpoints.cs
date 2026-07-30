using ThermoCore.Api.Contracts;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Results;

namespace ThermoCore.Api.Endpoints;

public static class ModelsEndpoints
{
    public static IEndpointRouteBuilder MapModelsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/models", () =>
            {
                var modelIds = typeof(AwgV3TopologyIds.ModelIds)
                    .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                    .Select(f => (string)f.GetRawConstantValue()!)
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .ToArray();

                return Results.Ok(new ModelCatalogResponse
                {
                    TopologyId = AwgV3TopologyIds.TopologyId,
                    TopologyVersion = AwgV3TopologyIds.TopologyVersion,
                    ComponentModelIds = modelIds,
                    ResultFormatVersion = SimulationResultCollector.ResultFormatVersion,
                    ApiVersion = "v1"
                });
            })
            .WithName("GetModels")
            .WithTags("Models");

        return app;
    }
}
