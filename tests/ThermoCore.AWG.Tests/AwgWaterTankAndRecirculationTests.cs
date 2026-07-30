using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Components;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.Tests;

public class AwgWaterTankAndRecirculationTests
{
    [Fact]
    public void WaterTank_AccumulatesCondensateDuringRun()
    {
        var baseConfig = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false);
        var configuration = baseConfig with
        {
            WaterTank = new AwgWaterTankParameters
            {
                CapacityKg = 5.0,
                InitialTemperatureK = baseConfig.Ambient.TemperatureK
            },
            Condenser = baseConfig.Condenser with
            {
                FallbackAvailableCoolingPowerW = 500.0,
                BypassFactor = 0.05,
                FallbackSurfaceTemperatureK = UnitConversions.CelsiusToKelvin(2.0)
            }
        };

        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration) with
        {
            WaterTankContentKg = 0.1,
            SilicaGelLoadingKgPerKg = 0.25,
            SilicaGelTemperatureK = UnitConversions.CelsiusToKelvin(70.0)
        };

        var run = new AwgSimulationRunner().Run(
            configuration,
            initial,
            AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1)));

        Assert.True(run.EngineResult.Succeeded, string.Join("; ", run.EngineResult.Diagnostics.Select(d => d.Message)));
        Assert.True(run.Summary.FinalWaterTankContentKg > 0.1);
        var tank = Assert.IsType<WaterTankComponent>(
            run.BuiltSystem.Graph.Components.Single(c => c.Id == AwgV3TopologyIds.WaterTank));
        Assert.True(tank.StoredMassKg >= initial.WaterTankContentKg);
    }

    [Fact]
    public void RecirculationTopology_ConvergesWithCyclicSolver()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(
            enableElectricalSubsystem: false,
            enableRecirculation: true);
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);

        var run = new AwgSimulationRunner().Run(
            configuration,
            initial,
            AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1)));

        Assert.True(run.BuiltSystem.RequiresCyclicSolver);
        Assert.True(
            run.EngineResult.Succeeded,
            string.Join("; ", run.EngineResult.Diagnostics.Select(d => $"{d.Code}:{d.Message}")));
        Assert.Contains(run.Summary.FinalMoistAirTemperaturesC.Keys, k => k is "MP-02" or "MP-11");
        Assert.NotNull(run.Summary.FinalWaterTankContentKg);
    }
}
