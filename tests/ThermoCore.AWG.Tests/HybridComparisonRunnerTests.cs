using ThermoCore.AWG.Hybrid;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.Tests;

public class HybridComparisonRunnerTests
{
    private readonly PsychrometricCalculator _calculator = new();

    [Fact]
    public void DefaultSuite_RunsAllFiveVariants_AndPassesBalances()
    {
        var report = new HybridComparisonRunner().Run();
        Assert.Equal(10, report.Points.Count); // 2 climates × 5 variants
        Assert.Contains(report.Points, p => p.Variant == HybridComparisonVariant.DirectTec);
        Assert.Contains(report.Points, p => p.Variant == HybridComparisonVariant.HeatingOnlyControl);
        Assert.Contains(report.Points, p => p.Variant == HybridComparisonVariant.SorbentPlusTec);
        Assert.Contains(report.Points, p => p.Variant == HybridComparisonVariant.DirectCompressor);
        Assert.Contains(report.Points, p => p.Variant == HybridComparisonVariant.SorbentPlusCompressor);
        Assert.All(report.Points, p => Assert.True(p.Passed, $"{p.ScenarioId}: {p.FailureMessage}"));
    }

    [Fact]
    public void Hyb001_SorbentPlusTec_HasHigherInletDewPointThanDirectTec()
    {
        var report = new HybridComparisonRunner().Run(
            HybridComparisonCatalog.CreatePairwiseTecComparison());

        var direct = report.Points.Single(p => p.Variant == HybridComparisonVariant.DirectTec);
        var hybrid = report.Points.Single(p => p.Variant == HybridComparisonVariant.SorbentPlusTec);

        Assert.True(hybrid.InletDewPointC > direct.InletDewPointC);
        Assert.True(hybrid.DesorbedVaporKgPerSecond is > 0.0);
        Assert.Null(direct.DesorbedVaporKgPerSecond);
        Assert.True(hybrid.ExhaustedVaporKgPerSecond >= 0.0);
        Assert.True(direct.ExhaustedVaporKgPerSecond >= 0.0);
    }

    [Fact]
    public void Hyb002_SorbentPlusCompressor_HasHigherInletHumidityThanDirect()
    {
        var report = new HybridComparisonRunner().Run(
            HybridComparisonCatalog.CreatePairwiseCompressorComparison());

        var direct = report.Points.Single(p => p.Variant == HybridComparisonVariant.DirectCompressor);
        var hybrid = report.Points.Single(p => p.Variant == HybridComparisonVariant.SorbentPlusCompressor);

        Assert.True(hybrid.InletHumidityRatioKgPerKgDryAir > direct.InletHumidityRatioKgPerKgDryAir);
        Assert.True(hybrid.DesorbedVaporKgPerSecond is > 0.0);
        Assert.All(report.Points, p => Assert.True(p.Passed, p.FailureMessage));
    }

    [Fact]
    public void HeatingOnlyControl_DoesNotRaiseDewPoint()
    {
        var ambient = HybridStreamFactory.CreateAmbient(30.0, 0.50, 0.02, _calculator);
        var heated = HybridStreamFactory.CreateHeatedControlStream(ambient, 25.0, _calculator);

        Assert.True(heated.TemperatureK > ambient.TemperatureK);
        Assert.Equal(ambient.HumidityRatioKgPerKgDryAir, heated.HumidityRatioKgPerKgDryAir, precision: 10);
        Assert.Equal(ambient.DewPointTemperatureK, heated.DewPointTemperatureK, precision: 6);
    }

    [Fact]
    public void ReportWriter_EmitsCsvAndMarkdown()
    {
        var report = new HybridComparisonRunner().Run(
            HybridComparisonCatalog.CreatePairwiseTecComparison());
        var directory = Path.Combine(Path.GetTempPath(), "hybrid-" + Guid.NewGuid().ToString("N"));
        try
        {
            HybridComparisonReportWriter.Write(report, directory);
            Assert.True(File.Exists(Path.Combine(directory, "hybrid-comparison.csv")));
            Assert.True(File.Exists(Path.Combine(directory, "hybrid-comparison.md")));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
