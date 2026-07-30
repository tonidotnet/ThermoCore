namespace ThermoCore.Api.Contracts;

/// <summary>Side-by-side summary comparison with B − A deltas (DATA-008).</summary>
public sealed record SimulationCompareResponse
{
    public required SimulationSummaryResponse A { get; init; }

    public required SimulationSummaryResponse B { get; init; }

    public required int CompletedStepsDelta { get; init; }

    public required double AggregatedWaterResidualKgDelta { get; init; }

    public required double AggregatedEnergyResidualJDelta { get; init; }

    public double? FinalWaterTankContentKgDelta { get; init; }
}
