using ThermoCore.AWG.Simulation;

namespace ThermoCore.AWG.Measurement;

/// <summary>Full AWG process-train station snapshot for a completed run.</summary>
public sealed record AwgFullFlowStationReport
{
    public required AwgSimulationRunResult Run { get; init; }

    public required IReadOnlyList<AwgFullFlowStationSample> Stations { get; init; }

    public required bool HeatRecoveryEnabled { get; init; }

    public double? CollectedWaterKg => Run.Summary.FinalWaterTankContentKg;
}
