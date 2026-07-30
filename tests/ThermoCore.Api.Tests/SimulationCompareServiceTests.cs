using ThermoCore.Api.Contracts;
using ThermoCore.Api.Services;

namespace ThermoCore.Api.Tests;

public class SimulationCompareServiceTests
{
    [Fact]
    public void Compare_ComputesBMinusADeltas()
    {
        var a = Summary(steps: 10, waterResidual: 1e-6, energyResidual: 2.0, tank: 0.1);
        var b = Summary(steps: 12, waterResidual: 2e-6, energyResidual: 5.0, tank: 0.25);

        var result = new SimulationCompareService().Compare(a, b);

        Assert.Equal(2, result.CompletedStepsDelta);
        Assert.Equal(1e-6, result.AggregatedWaterResidualKgDelta, precision: 12);
        Assert.Equal(3.0, result.AggregatedEnergyResidualJDelta, precision: 12);
        Assert.NotNull(result.FinalWaterTankContentKgDelta);
        Assert.Equal(0.15, result.FinalWaterTankContentKgDelta.Value, precision: 12);
    }

    private static SimulationSummaryResponse Summary(
        int steps,
        double waterResidual,
        double energyResidual,
        double? tank)
        => new()
        {
            SimulationId = Guid.NewGuid().ToString("N"),
            Status = "Completed",
            Succeeded = true,
            TopologyId = "awg-v3",
            CompletedSteps = steps,
            AggregatedEnergyResidualJ = energyResidual,
            AggregatedWaterResidualKg = waterResidual,
            AggregatedDryAirResidualKg = 0,
            WaterBalancePassed = true,
            EnergyBalancePassed = true,
            WarningCount = 0,
            ErrorCount = 0,
            FinalWaterTankContentKg = tank,
            FinalBusPowerW = null
        };
}
