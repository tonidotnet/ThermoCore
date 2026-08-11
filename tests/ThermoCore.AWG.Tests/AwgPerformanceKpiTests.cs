using ThermoCore.AWG.Optimization;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;

namespace ThermoCore.AWG.Tests;

public class AwgPerformanceKpiTests
{
    [Theory]
    [InlineData(1.0, 2.0, 0.5)]
    [InlineData(0.0, 2.0, 0.0)]
    public void RatioOrNull_DividesWhenDenominatorPositive(double num, double den, double expected)
    {
        Assert.Equal(expected, AwgPerformanceKpiCalculator.RatioOrNull(num, den));
    }

    [Theory]
    [InlineData(1.0, 0.0)]
    [InlineData(1.0, -1.0)]
    public void RatioOrNull_ReturnsNullWhenDenominatorNonPositive(double num, double den)
    {
        Assert.Null(AwgPerformanceKpiCalculator.RatioOrNull(num, den));
    }

    [Fact]
    public void RatioOrNull_ReturnsNullWhenEitherOperandMissing()
    {
        Assert.Null(AwgPerformanceKpiCalculator.RatioOrNull(null, 1.0));
        Assert.Null(AwgPerformanceKpiCalculator.RatioOrNull(1.0, null));
        Assert.Null(AwgPerformanceKpiCalculator.RatioOrNull(null, null));
    }

    [Fact]
    public void InverseWhPerLiter_ReturnsNullWhenWaterOrEnergyMissing()
    {
        Assert.Null(AwgPerformanceKpiCalculator.InverseWhPerLiter(0.0, 3600.0));
        Assert.Null(AwgPerformanceKpiCalculator.InverseWhPerLiter(1.0, 0.0));
        Assert.Null(AwgPerformanceKpiCalculator.InverseWhPerLiter(1.0, null));
    }

    [Fact]
    public void InverseWhPerLiter_ConvertsJoulesToWattHoursPerLiter()
    {
        // 3600 J = 1 Wh over 0.5 L → 2 Wh/L
        Assert.Equal(2.0, AwgPerformanceKpiCalculator.InverseWhPerLiter(0.5, 3600.0));
    }

    [Fact]
    public void ShortRun_ExposesAdditiveKpis_AndOmitsUndefinedScalars()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: true);
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
        var options = AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1));
        var run = new AwgSimulationRunner().Run(configuration, initial, options);

        Assert.True(run.EngineResult.Succeeded);
        Assert.NotNull(run.Summary.LitersPerDay);
        Assert.NotNull(run.Summary.SolarCollectorApertureAreaM2);
        Assert.Equal(2.0, run.Summary.SolarCollectorApertureAreaM2);
        Assert.NotNull(run.Summary.AmbientMoistureIntakeKg);
        Assert.True(run.Summary.AmbientMoistureIntakeKg > 0.0);
        Assert.NotNull(run.Summary.ElectricEnergyConsumedJ);
        Assert.True(run.Summary.ElectricEnergyConsumedJ >= 0.0);
        Assert.NotNull(run.Summary.LitersPerDayPerSquareMeterAperture);
        Assert.Equal(
            run.Summary.LitersPerDay / run.Summary.SolarCollectorApertureAreaM2,
            run.Summary.LitersPerDayPerSquareMeterAperture);

        if ((run.Summary.FinalWaterTankContentKg ?? 0.0) <= 0.0)
        {
            // Zero liters → Wh/L undefined; L/kWh may be 0 when energy > 0.
            Assert.Null(run.Summary.WattHoursElectricPerLiter);
            if (run.Summary.ElectricEnergyConsumedJ is > 0.0)
            {
                Assert.Equal(0.0, run.Summary.LitersPerKwhElectric);
            }
        }
        else
        {
            Assert.NotNull(run.Summary.LitersPerKwhSolarPrimary);
            Assert.NotNull(run.Summary.WaterRecoveryFraction);
            Assert.True(run.Summary.WaterRecoveryFraction is >= 0 and <= 1.0);
        }

        Assert.Equal(run.Summary.LitersPerKwhElectric, AwgOptimizationObjectives.LitersPerKwhElectric(run.Summary));
        Assert.Equal(run.Summary.WaterRecoveryFraction, AwgOptimizationObjectives.WaterRecoveryFraction(run.Summary));

        var collected = AwgResultExporter.Collect(run);
        Assert.True(collected.Summary.ScalarMetrics.ContainsKey("energy.solar.totalJ"));
        Assert.True(collected.Summary.ScalarMetrics.ContainsKey("kpi.litersPerDayPerSquareMeterAperture"));
        if (run.Summary.LitersPerKwhElectric is null)
        {
            Assert.False(collected.Summary.ScalarMetrics.ContainsKey("kpi.litersPerKwhElectric"));
        }
        else
        {
            Assert.True(collected.Summary.ScalarMetrics.ContainsKey("kpi.litersPerKwhElectric"));
        }
    }

    [Fact]
    public void SolarPrimary_UsesIncidentApertureEnergy_NotUsefulHeat()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false);
        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration);
        var options = AwgSimulationOptions.CreateDefault(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));
        var run = new AwgSimulationRunner().Run(configuration, initial, options);

        Assert.True(run.EngineResult.Succeeded);
        Assert.NotNull(run.Summary.IncidentSolarEnergyJ);
        Assert.True(run.Summary.IncidentSolarEnergyJ > 0.0);

        // Useful collector heat is a process transfer; KPI denominator must stay on incident solar.
        if (run.Summary.UsefulCollectorEnergyJ is { } useful
            && useful > 0.0
            && (run.Summary.FinalWaterTankContentKg ?? 0.0) > 0.0)
        {
            var expected = (run.Summary.FinalWaterTankContentKg!.Value)
                / (run.Summary.IncidentSolarEnergyJ.Value / AwgPerformanceKpiCalculator.JoulesPerKilowattHour);
            Assert.Equal(expected, run.Summary.LitersPerKwhSolarPrimary!.Value, precision: 9);
            Assert.NotEqual(
                (run.Summary.FinalWaterTankContentKg.Value)
                / (useful / AwgPerformanceKpiCalculator.JoulesPerKilowattHour),
                run.Summary.LitersPerKwhSolarPrimary.Value,
                precision: 6);
        }
    }

    [Fact]
    public void ZeroAperture_YieldsNullApertureIntensity()
    {
        Assert.Null(AwgPerformanceKpiCalculator.RatioOrNull(1.5, 0.0));
        Assert.Null(AwgPerformanceKpiCalculator.RatioOrNull(1.5, null));
    }
}
