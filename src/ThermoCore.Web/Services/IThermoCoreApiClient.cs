using ThermoCore.Api.Contracts;
using ThermoCore.AWG.Configuration;

namespace ThermoCore.Web.Services;

/// <summary>Application-facing ThermoCore API used by Blazor pages (no physics in UI).</summary>
public interface IThermoCoreApiClient
{
    Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default);

    Task<ModelCatalogResponse> GetModelsAsync(CancellationToken cancellationToken = default);

    Task<PsychrometricCalculateResponse> CalculatePsychrometricsAsync(
        PsychrometricCalculateRequest request,
        CancellationToken cancellationToken = default);

    Task<ConfigurationValidateResponse> ValidateConfigurationAsync(
        AwgConfigurationDocument configuration,
        CancellationToken cancellationToken = default);

    Task<CreateSimulationResponse> CreateSimulationAsync(
        CreateSimulationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SimulationStatusResponse>> ListSimulationsAsync(
        CancellationToken cancellationToken = default);

    Task<SimulationStatusResponse?> GetSimulationAsync(
        string simulationId,
        CancellationToken cancellationToken = default);

    Task<bool> CancelSimulationAsync(
        string simulationId,
        CancellationToken cancellationToken = default);

    Task<SimulationSummaryResponse?> GetSummaryAsync(
        string simulationId,
        CancellationToken cancellationToken = default);

    Task<SimulationSeriesResponse?> GetSeriesAsync(
        string simulationId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<SimulationDiagnosticsResponse?> GetDiagnosticsAsync(
        string simulationId,
        CancellationToken cancellationToken = default);

    Task<(byte[] Content, string ContentType, string FileName)?> ExportAsync(
        string simulationId,
        string format,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersistedSimulationListItem>> ListPersistedSimulationsAsync(
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<SimulationSummaryResponse?> GetPersistedSummaryAsync(
        string summaryId,
        CancellationToken cancellationToken = default);

    Task<SimulationCompareResponse?> ComparePersistedAsync(
        string summaryIdA,
        string summaryIdB,
        CancellationToken cancellationToken = default);
}
