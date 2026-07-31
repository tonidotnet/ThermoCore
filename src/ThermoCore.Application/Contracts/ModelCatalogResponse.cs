namespace ThermoCore.Api.Contracts;

public sealed record ModelCatalogResponse
{
    public required string TopologyId { get; init; }

    public required string TopologyVersion { get; init; }

    public required IReadOnlyList<string> ComponentModelIds { get; init; }

    public required string ResultFormatVersion { get; init; }

    public required string ApiVersion { get; init; }

    public string FidelityNotes { get; init; } =
        "AWG V3 MVP models; heat recovery uses prescribed effectiveness in the topology builder; airflow network graph types remain deferred.";
}
