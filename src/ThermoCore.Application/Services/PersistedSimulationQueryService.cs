using ThermoCore.Api.Contracts;
using ThermoCore.Persistence;

namespace ThermoCore.Api.Services;

/// <summary>Reads persisted simulation summaries for list/compare (DATA-008).</summary>
public sealed class PersistedSimulationQueryService
{
    private readonly IThermoCoreStore _store;

    public PersistedSimulationQueryService(IThermoCoreStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public PersistedSimulationListResponse List(int take = 50)
    {
        var items = _store.ListSimulationSummaries(take)
            .Select(s => new PersistedSimulationListItem
            {
                SummaryId = s.Id.ToString("N"),
                ConfigurationVersionId = s.ConfigurationVersionId.ToString("N"),
                Status = s.Status,
                Succeeded = s.Succeeded,
                TopologyId = s.TopologyId,
                CompletedSteps = s.CompletedSteps,
                FinalWaterTankContentKg = s.FinalWaterTankContentKg,
                CreatedAtUtc = s.CreatedAtUtc
            })
            .ToArray();

        return new PersistedSimulationListResponse { Simulations = items };
    }

    public SimulationSummaryResponse? GetSummary(Guid summaryId)
    {
        var stored = _store.GetSimulationSummary(summaryId);
        return stored is null ? null : ToSummaryResponse(stored);
    }

    public static SimulationSummaryResponse ToSummaryResponse(StoredSimulationSummary stored)
        => new()
        {
            SimulationId = stored.Id.ToString("N"),
            Status = stored.Status,
            Succeeded = stored.Succeeded,
            TopologyId = stored.TopologyId,
            CompletedSteps = stored.CompletedSteps,
            AggregatedEnergyResidualJ = stored.AggregatedEnergyResidualJ,
            AggregatedWaterResidualKg = stored.AggregatedWaterResidualKg,
            AggregatedDryAirResidualKg = 0,
            WaterBalancePassed = stored.WaterBalancePassed,
            EnergyBalancePassed = stored.EnergyBalancePassed,
            WarningCount = 0,
            ErrorCount = 0,
            FinalWaterTankContentKg = stored.FinalWaterTankContentKg,
            FinalBusPowerW = null,
            CollectedWaterKg = stored.FinalWaterTankContentKg,
            LitersPerDay = null,
            WattHoursPerLiter = null
        };
}
