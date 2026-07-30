using ThermoCore.Api.Contracts;

namespace ThermoCore.Api.Services;

/// <summary>Builds summary-level compare payloads (DATA-008).</summary>
public sealed class SimulationCompareService
{
    public SimulationCompareResponse Compare(SimulationSummaryResponse a, SimulationSummaryResponse b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        double? tankDelta = null;
        if (a.FinalWaterTankContentKg is { } tankA && b.FinalWaterTankContentKg is { } tankB)
        {
            tankDelta = tankB - tankA;
        }

        return new SimulationCompareResponse
        {
            A = a,
            B = b,
            CompletedStepsDelta = b.CompletedSteps - a.CompletedSteps,
            AggregatedWaterResidualKgDelta = b.AggregatedWaterResidualKg - a.AggregatedWaterResidualKg,
            AggregatedEnergyResidualJDelta = b.AggregatedEnergyResidualJ - a.AggregatedEnergyResidualJ,
            FinalWaterTankContentKgDelta = tankDelta
        };
    }
}
