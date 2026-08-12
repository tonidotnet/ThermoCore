using System.Text.Json;
using ThermoCore.Core.Components.Thermoelectric;
using ThermoCore.Core.Components.VaporCompression;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

public class VaporCompressionPerformanceMapTests
{
    [Fact]
    public void CatalogMap_ExactManufacturerPoints_AreReproduced()
    {
        var map = VaporCompressionPerformanceMapCatalog.CreateGenericSmallDcModuleReference();
        var evaluator = new VaporCompressionMapEvaluator(map);

        foreach (var point in map.MapPoints)
        {
            var result = evaluator.Evaluate(
                point.EvaporatingTemperatureK,
                point.CondensingTemperatureK,
                point.SpeedFraction);

            Assert.False(result.OutsideValidity);
            Assert.False(result.Rejected);
            Assert.Equal(point.CoolingCapacityW, result.CoolingCapacityW, precision: 10);
            Assert.Equal(point.ElectricalPowerW, result.ElectricalPowerW, precision: 10);
            Assert.Equal(point.EffectiveCop, result.Cop!.Value, precision: 10);
        }
    }

    [Fact]
    public void Interpolation_IsDeterministic_BetweenGridNeighbors()
    {
        var map = VaporCompressionPerformanceMapCatalog.CreateGenericSmallDcModuleReference();
        var evaluator = new VaporCompressionMapEvaluator(map);

        var te = UnitConversions.CelsiusToKelvin(7.5);
        var tc = UnitConversions.CelsiusToKelvin(40.0);
        const double speed = 0.75;

        var a = evaluator.Evaluate(te, tc, speed);
        var b = evaluator.Evaluate(te, tc, speed);

        Assert.Equal(a.CoolingCapacityW, b.CoolingCapacityW, precision: 12);
        Assert.Equal(a.ElectricalPowerW, b.ElectricalPowerW, precision: 12);
        Assert.Equal(a.Cop, b.Cop);
        Assert.True(a.CoolingCapacityW > 0.0);
        Assert.True(a.ElectricalPowerW > 0.0);
        Assert.InRange(a.Cop!.Value, 1.0, 4.0);
    }

    [Fact]
    public void ClampPolicy_EmitsOutsideValidity_WithoutExtrapolation()
    {
        var map = VaporCompressionPerformanceMapCatalog.CreateGenericSmallDcModuleReference();
        Assert.Equal(VaporCompressionExtrapolationPolicy.ClampWithDiagnostic, map.ExtrapolationPolicy);
        var evaluator = new VaporCompressionMapEvaluator(map);

        var result = evaluator.Evaluate(
            UnitConversions.CelsiusToKelvin(-10.0),
            UnitConversions.CelsiusToKelvin(60.0),
            speedFraction: 1.2);

        Assert.True(result.OutsideValidity);
        Assert.False(result.Rejected);
        Assert.Contains(result.Diagnostics, d => d.Code == "VC.OUTSIDE_VALIDITY");
        Assert.InRange(
            result.EffectiveQuery.EvaporatingTemperatureK,
            map.Validity.MinimumEvaporatingTemperatureK,
            map.Validity.MaximumEvaporatingTemperatureK);
        Assert.InRange(
            result.EffectiveQuery.CondensingTemperatureK,
            map.Validity.MinimumCondensingTemperatureK,
            map.Validity.MaximumCondensingTemperatureK);
        Assert.InRange(
            result.EffectiveQuery.SpeedFraction,
            map.Validity.MinimumSpeedFraction,
            map.Validity.MaximumSpeedFraction);
    }

    [Fact]
    public void RejectPolicy_ZerosCapacity_WithErrorDiagnostic()
    {
        var map = VaporCompressionPerformanceMapCatalog.CreateGenericSmallDcModuleReference() with
        {
            ExtrapolationPolicy = VaporCompressionExtrapolationPolicy.Reject
        };
        var evaluator = new VaporCompressionMapEvaluator(map);

        var result = evaluator.Evaluate(
            UnitConversions.CelsiusToKelvin(-10.0),
            UnitConversions.CelsiusToKelvin(60.0),
            1.0);

        Assert.True(result.Rejected);
        Assert.Equal(0.0, result.CoolingCapacityW);
        Assert.Equal(0.0, result.ElectricalPowerW);
        Assert.Null(result.Cop);
        Assert.Contains(result.Diagnostics, d => d.Code == "VC.EXTRAPOLATION_REJECTED");
    }

    [Fact]
    public void SafetyDiagnostics_FrostAndHighCondensing_AreEmitted()
    {
        var map = VaporCompressionPerformanceMapCatalog.CreateGenericSmallDcModuleReference();
        var evaluator = new VaporCompressionMapEvaluator(map);

        var frost = evaluator.Evaluate(
            UnitConversions.CelsiusToKelvin(-1.0),
            UnitConversions.CelsiusToKelvin(40.0),
            1.0);
        Assert.Contains(frost.Diagnostics, d => d.Code == "VC.FROST_RISK");

        var high = evaluator.Evaluate(
            UnitConversions.CelsiusToKelvin(10.0),
            UnitConversions.CelsiusToKelvin(60.0),
            1.0,
            dischargeTemperatureK: UnitConversions.CelsiusToKelvin(100.0));
        Assert.Contains(high.Diagnostics, d => d.Code == "VC.CONDENSING_TEMPERATURE_HIGH");
        Assert.Contains(high.Diagnostics, d => d.Code == "VC.DISCHARGE_TEMPERATURE_HIGH");
    }

    [Fact]
    public void CyclingLimits_AreExposedOnEvaluationResult()
    {
        var map = VaporCompressionPerformanceMapCatalog.CreateGenericSmallDcModuleReference();
        var result = new VaporCompressionMapEvaluator(map).Evaluate(
            UnitConversions.CelsiusToKelvin(10.0),
            UnitConversions.CelsiusToKelvin(35.0),
            1.0);

        Assert.Equal(TimeSpan.FromMinutes(3), result.MinimumRuntime);
        Assert.Equal(TimeSpan.FromMinutes(3), result.MinimumOffTime);
    }

    [Fact]
    public void Serialization_RoundTrips_AndIgnoresUnknownFields()
    {
        var map = VaporCompressionPerformanceMapCatalog.CreateGenericSmallDcModuleReference();
        var json = VaporCompressionPerformanceMapSerializer.SaveToJson(map);
        var loaded = VaporCompressionPerformanceMapSerializer.LoadFromJson(json);

        Assert.Equal(map.ProfileId, loaded.ProfileId);
        Assert.Equal(map.MapPoints.Count, loaded.MapPoints.Count);
        Assert.Equal(map.Cycling.MinimumRuntime, loaded.Cycling.MinimumRuntime);
        Assert.Equal(map.Safety.FrostThresholdEvaporatingTemperatureK, loaded.Safety.FrostThresholdEvaporatingTemperatureK);

        var mutable = JsonSerializer.SerializeToNode(map, VaporCompressionPerformanceMapSerializer.CreateSerializerOptions())!;
        mutable["futureExtensionField"] = "ignored";
        var reloaded = VaporCompressionPerformanceMapSerializer.LoadFromJson(mutable.ToJsonString());
        Assert.Equal(map.ProfileId, reloaded.ProfileId);
    }

    [Fact]
    public void SampleFile_LoadsAndMatchesCatalogShape()
    {
        var path = FindRepoFile(Path.Combine("samples", "vapor-compression", "generic-small-dc-module.r5-001.json"));
        var loaded = VaporCompressionPerformanceMapSerializer.LoadFromFile(path);
        Assert.Equal(VaporCompressionPerformanceMapCatalog.GenericSmallDcModuleProfileId, loaded.ProfileId);
        Assert.Equal(8, loaded.MapPoints.Count);
        Assert.Equal(TecEvidenceLevel.ProvisionalEngineering, loaded.EvidenceLevel);
    }

    [Fact]
    public void InvalidPoint_TevapAboveTcond_IsRejected()
    {
        Assert.ThrowsAny<ArgumentException>(() => new VaporCompressionMapPoint
        {
            EvaporatingTemperatureK = 300.0,
            CondensingTemperatureK = 290.0,
            SpeedFraction = 1.0,
            CoolingCapacityW = 100.0,
            ElectricalPowerW = 50.0
        }.Validate());
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
