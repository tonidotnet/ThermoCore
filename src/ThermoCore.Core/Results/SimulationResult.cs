using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.Core.Results;

/// <summary>
/// Collected simulation result with optional downsampled time-series channels
/// (docs/04_Simulation/16_SimulationEngine.md §19–§20, docs/05_Product/29_ResultFormats.md).
/// </summary>
public sealed record SimulationResult
{
    public required SimulationRunMetadata Metadata { get; init; }

    public required SimulationRunStatus Status { get; init; }

    public required SimulationSummary Summary { get; init; }

    public required IReadOnlyList<ResultTimeSeriesChannel> Channels { get; init; }

    public required IReadOnlyList<SimulationDiagnostic> Diagnostics { get; init; }

    public required ConservationBalance AggregatedBalance { get; init; }
}
