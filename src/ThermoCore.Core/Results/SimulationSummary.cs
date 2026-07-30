using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.Core.Results;

public sealed record SimulationSummary
{
    public required bool Succeeded { get; init; }

    public required SimulationRunStatus Status { get; init; }

    public required double MaxAbsEnergyResidualJ { get; init; }

    public required double MaxAbsWaterResidualKg { get; init; }

    public required double MaxAbsDryAirResidualKg { get; init; }

    public required double AggregatedEnergyResidualJ { get; init; }

    public required double AggregatedWaterResidualKg { get; init; }

    public required int WarningCount { get; init; }

    public required int ErrorCount { get; init; }

    public IReadOnlyDictionary<string, double> ScalarMetrics { get; init; }
        = new Dictionary<string, double>(StringComparer.Ordinal);
}
