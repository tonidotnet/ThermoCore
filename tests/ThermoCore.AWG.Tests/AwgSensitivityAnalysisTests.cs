using ThermoCore.AWG.Calibration;
using ThermoCore.AWG.Optimization;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;

namespace ThermoCore.AWG.Tests;

public class AwgSensitivityAnalysisTests
{
    [Fact]
    public void Sensitivity_RanksParametersByElasticityMagnitude()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false);
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
        var options = AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));

        var result = new AwgSensitivityAnalysisRunner().Run(
            configuration,
            initial,
            options,
            new AwgSensitivityAnalysisOptions { RelativePerturbationFraction = 0.10 },
            [
                AwgCalibratableParameterIds.CondenserBypassFactor,
                AwgCalibratableParameterIds.CondenserDrainageEfficiency
            ]);

        Assert.True(result.BaselineSucceeded, result.BaselineFailureMessage);
        Assert.Equal(2, result.Parameters.Count);
        Assert.All(result.Parameters, p => Assert.True(p.Succeeded, p.FailureMessage));
        Assert.All(result.Parameters, p => Assert.NotNull(p.LitersPerDayDerivative));
        Assert.All(result.Parameters, p => Assert.NotNull(p.LitersPerDayElasticity));
        Assert.Equal(2, result.RankedByElasticityMagnitude.Count);
        Assert.Equal(
            result.RankedByElasticityMagnitude[0].RankingMagnitude,
            Math.Max(
                result.Parameters[0].RankingMagnitude,
                result.Parameters[1].RankingMagnitude));
    }
}
