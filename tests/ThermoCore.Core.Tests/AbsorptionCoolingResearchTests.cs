using System.Text.Json;
using ThermoCore.Core.Components.Absorption;
using ThermoCore.Core.Components.Thermoelectric;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

public class AbsorptionCoolingResearchTests
{
    [Fact]
    public void CatalogPoints_AreReproducedExactly()
    {
        var map = AbsorptionPerformanceMapCatalog.CreateGenericSolarThermalScreen();
        var model = new AbsorptionCoolingResearchModel(map);

        foreach (var point in map.MapPoints)
        {
            var result = model.Evaluate(
                point.GeneratorTemperatureK,
                point.HeatSinkTemperatureK,
                point.EvaporatorTemperatureK);

            Assert.True(result.ResearchOnly);
            Assert.True(result.Feasible);
            Assert.False(result.OutsideValidity);
            Assert.Equal(point.CoolingOutputW, result.CoolingOutputW, precision: 8);
            Assert.Equal(point.ThermalInputW, result.ThermalInputW, precision: 8);
            Assert.Equal(point.EffectiveThermalCop, result.ThermalCop!.Value, precision: 8);
            Assert.Contains(result.Diagnostics, d => d.Code == "ABSORPTION.RESEARCH_ONLY");
        }
    }

    [Fact]
    public void ClampPolicy_EmitsOutsideValidity()
    {
        var map = AbsorptionPerformanceMapCatalog.CreateGenericSolarThermalScreen();
        var result = new AbsorptionCoolingResearchModel(map).Evaluate(
            UnitConversions.CelsiusToKelvin(120.0),
            UnitConversions.CelsiusToKelvin(20.0),
            UnitConversions.CelsiusToKelvin(0.0));

        Assert.True(result.OutsideValidity);
        Assert.Contains(result.Diagnostics, d => d.Code == "ABSORPTION.OUTSIDE_VALIDITY");
    }

    [Fact]
    public void RejectPolicy_ZerosOutputs()
    {
        var map = AbsorptionPerformanceMapCatalog.CreateGenericSolarThermalScreen() with
        {
            ExtrapolationPolicy = AbsorptionExtrapolationPolicy.Reject
        };
        var result = new AbsorptionCoolingResearchModel(map).Evaluate(
            UnitConversions.CelsiusToKelvin(120.0),
            UnitConversions.CelsiusToKelvin(20.0),
            UnitConversions.CelsiusToKelvin(0.0));

        Assert.True(result.Rejected);
        Assert.False(result.Feasible);
        Assert.Equal(0.0, result.CoolingOutputW);
        Assert.Null(result.ThermalCop);
        Assert.Contains(result.Diagnostics, d => d.Code == "ABSORPTION.EXTRAPOLATION_REJECTED");
    }

    [Fact]
    public void ResearchOnlyFlag_CannotBeCleared()
    {
        var map = AbsorptionPerformanceMapCatalog.CreateGenericSolarThermalScreen() with
        {
            ResearchOnly = false
        };
        Assert.ThrowsAny<ArgumentException>(() => map.Validate());
    }

    [Fact]
    public void Serialization_RoundTrips_AndSampleFileLoads()
    {
        var map = AbsorptionPerformanceMapCatalog.CreateGenericSolarThermalScreen();
        var json = AbsorptionPerformanceMapSerializer.SaveToJson(map);
        var loaded = AbsorptionPerformanceMapSerializer.LoadFromJson(json);
        Assert.Equal(map.ProfileId, loaded.ProfileId);
        Assert.Equal(8, loaded.MapPoints.Count);

        var mutable = JsonSerializer.SerializeToNode(map, AbsorptionPerformanceMapSerializer.CreateSerializerOptions())!;
        mutable["futureExtensionField"] = "ignored";
        Assert.Equal(map.ProfileId, AbsorptionPerformanceMapSerializer.LoadFromJson(mutable.ToJsonString()).ProfileId);

        var path = FindRepoFile(Path.Combine("samples", "absorption", "generic-solar-thermal-screen.r7-001.json"));
        var fromFile = AbsorptionPerformanceMapSerializer.LoadFromFile(path);
        Assert.Equal(AbsorptionPerformanceMapCatalog.GenericSolarThermalScreenProfileId, fromFile.ProfileId);
        Assert.Equal(TecEvidenceLevel.ProvisionalEngineering, fromFile.EvidenceLevel);
        Assert.True(fromFile.ResearchOnly);
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate '{relativePath}'.");
    }
}
