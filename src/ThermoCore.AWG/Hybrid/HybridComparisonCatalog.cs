namespace ThermoCore.AWG.Hybrid;

/// <summary>Default hybrid comparison catalog (DOC-038 variants A–E).</summary>
public static class HybridComparisonCatalog
{
    public static IReadOnlyList<HybridComparisonScenario> CreateDefaultSuite()
    {
        var climates = new (double T, double Rh, string Tag)[]
        {
            (30.0, 0.50, "t30-rh50"),
            (35.0, 0.60, "t35-rh60")
        };

        var variants = new (HybridComparisonVariant Variant, string Code)[]
        {
            (HybridComparisonVariant.DirectTec, "A-direct-tec"),
            (HybridComparisonVariant.HeatingOnlyControl, "B-heating-only"),
            (HybridComparisonVariant.SorbentPlusTec, "C-sorbent-tec"),
            (HybridComparisonVariant.DirectCompressor, "D-direct-vc"),
            (HybridComparisonVariant.SorbentPlusCompressor, "E-sorbent-vc")
        };

        var scenarios = new List<HybridComparisonScenario>();
        foreach (var (t, rh, tag) in climates)
        {
            foreach (var (variant, code) in variants)
            {
                scenarios.Add(new HybridComparisonScenario
                {
                    ScenarioId = $"{code}/{tag}",
                    Variant = variant,
                    AmbientTemperatureC = t,
                    RelativeHumidityFraction = rh,
                    CondensingTemperatureC = Math.Max(40.0, t + 5.0),
                    CoolingSurfaceTemperatureC = 8.0,
                    Notes = $"Hybrid R6-001 {variant} at {t}°C / {rh * 100:0}% RH."
                }.Validate());
            }
        }

        return scenarios;
    }

    public static IReadOnlyList<HybridComparisonScenario> CreatePairwiseTecComparison(
        double ambientTemperatureC = 30.0,
        double relativeHumidityFraction = 0.50)
        =>
        [
            new HybridComparisonScenario
            {
                ScenarioId = "HYB-001/direct-tec",
                Variant = HybridComparisonVariant.DirectTec,
                AmbientTemperatureC = ambientTemperatureC,
                RelativeHumidityFraction = relativeHumidityFraction
            }.Validate(),
            new HybridComparisonScenario
            {
                ScenarioId = "HYB-001/sorbent-tec",
                Variant = HybridComparisonVariant.SorbentPlusTec,
                AmbientTemperatureC = ambientTemperatureC,
                RelativeHumidityFraction = relativeHumidityFraction
            }.Validate()
        ];

    public static IReadOnlyList<HybridComparisonScenario> CreatePairwiseCompressorComparison(
        double ambientTemperatureC = 30.0,
        double relativeHumidityFraction = 0.50)
        =>
        [
            new HybridComparisonScenario
            {
                ScenarioId = "HYB-002/direct-vc",
                Variant = HybridComparisonVariant.DirectCompressor,
                AmbientTemperatureC = ambientTemperatureC,
                RelativeHumidityFraction = relativeHumidityFraction,
                CondensingTemperatureC = Math.Max(40.0, ambientTemperatureC + 5.0)
            }.Validate(),
            new HybridComparisonScenario
            {
                ScenarioId = "HYB-002/sorbent-vc",
                Variant = HybridComparisonVariant.SorbentPlusCompressor,
                AmbientTemperatureC = ambientTemperatureC,
                RelativeHumidityFraction = relativeHumidityFraction,
                CondensingTemperatureC = Math.Max(40.0, ambientTemperatureC + 5.0)
            }.Validate()
        ];
}
