using System.Text.Json;
using ThermoCore.Core.Components.Thermoelectric;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

public class TecManufacturerProfileTests
{
    [Fact]
    public void GenericTec112706Reference_MapsToAnalyticalParameters_MatchingProvisionalDefaults()
    {
        var profile = TecManufacturerProfileCatalog.CreateGenericTec112706Reference();
        var expected = AnalyticalPeltierParameters.CreateProvisionalEngineeringDefaults();
        var mapped = profile.ToAnalyticalPeltierParameters();

        Assert.Equal(TecManufacturerProfileCatalog.GenericTec112706ProfileId, profile.ProfileId);
        Assert.Equal("Generic", profile.Manufacturer);
        Assert.Equal("TEC1-12706", profile.Model);
        Assert.Equal(TecEvidenceLevel.ProvisionalEngineering, profile.EvidenceLevel);
        Assert.Equal(TecParameterModelType.AnalyticalSteadyState, profile.ParameterModelType);

        Assert.Equal(expected.SeebeckCoefficientVPerK, mapped.SeebeckCoefficientVPerK);
        Assert.Equal(expected.ElectricalResistanceOhm, mapped.ElectricalResistanceOhm);
        Assert.Equal(expected.ThermalConductanceWPerK, mapped.ThermalConductanceWPerK);
        Assert.Equal(expected.MaximumCurrentA, mapped.MaximumCurrentA);
        Assert.Equal(expected.MaximumVoltageV, mapped.MaximumVoltageV);
        Assert.Equal(expected.MaximumTemperatureDifferenceK, mapped.MaximumTemperatureDifferenceK);
        Assert.Equal(0.04 * 0.04, mapped.ActiveColdSideAreaM2, precision: 12);
    }

    [Fact]
    public void GenericProfile_DrivesAnalyticalPeltierWithoutChangingPhysicsIdentity()
    {
        var profile = TecManufacturerProfileCatalog.CreateGenericTec112706Reference();
        var parameters = profile.ToAnalyticalPeltierParameters();
        var peltier = new AnalyticalPeltierComponent(
            id: "tec",
            parameters: parameters,
            coldSideTemperatureK: UnitConversions.CelsiusToKelvin(10.0),
            hotSideTemperatureK: UnitConversions.CelsiusToKelvin(40.0),
            requestedElectricalPowerW: 30.0);

        var context = new ThermoCore.Core.Graph.ComponentStepContext
        {
            Simulation = new ThermoCore.Core.Graph.SimulationContext
            {
                SimulationStart = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                TimeStep = TimeSpan.FromSeconds(1),
                ElapsedTime = TimeSpan.Zero,
                StepIndex = 0
            }
        };
        peltier.Initialize(context.Simulation);
        var step = peltier.Evaluate(context);
        peltier.Commit(step);

        Assert.DoesNotContain(step.Diagnostics, d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error);
        Assert.True(peltier.LastElectricalPowerW > 0.0);
        Assert.True(peltier.LastColdSideHeatW > 0.0);
    }

    [Fact]
    public void Serialization_RoundTrips_AndIgnoresUnknownFields()
    {
        var profile = TecManufacturerProfileCatalog.CreateGenericTec112706Reference();
        var json = TecManufacturerProfileSerializer.SaveToJson(profile);
        var loaded = TecManufacturerProfileSerializer.LoadFromJson(json);

        Assert.Equal(profile.ProfileId, loaded.ProfileId);
        Assert.Equal(profile.MaximumCurrentA, loaded.MaximumCurrentA);
        Assert.Equal(profile.AnalyticalCoefficients!.SeebeckCoefficientVPerK,
            loaded.AnalyticalCoefficients!.SeebeckCoefficientVPerK);

        // Forward-compatible: unknown members are skipped.
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement.Clone();
        var mutable = JsonSerializer.SerializeToNode(profile, TecManufacturerProfileSerializer.CreateSerializerOptions())!;
        mutable["futureExtensionField"] = "ignored";
        var withExtra = mutable.ToJsonString();
        var reloaded = TecManufacturerProfileSerializer.LoadFromJson(withExtra);
        Assert.Equal(profile.ProfileId, reloaded.ProfileId);
    }

    [Fact]
    public void ExistingProvisionalParameters_StillValidateIndependentlyOfProfiles()
    {
        var parameters = AnalyticalPeltierParameters.CreateProvisionalEngineeringDefaults().Validate();
        Assert.Equal(6.0, parameters.MaximumCurrentA);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void Invalid_Imax_IsRejected(double imax)
    {
        var profile = ValidAnalyticalShell() with { MaximumCurrentA = imax };
        Assert.ThrowsAny<ArgumentException>(() => profile.Validate());
    }

    [Fact]
    public void Invalid_EmptyManufacturer_IsRejected()
    {
        var profile = ValidAnalyticalShell() with { Manufacturer = "  " };
        Assert.ThrowsAny<ArgumentException>(() => profile.Validate());
    }

    [Fact]
    public void Invalid_UnsupportedSchemaVersion_IsRejected()
    {
        var profile = ValidAnalyticalShell() with { SchemaVersion = "9.9" };
        Assert.ThrowsAny<ArgumentException>(() => profile.Validate());
    }

    [Fact]
    public void Invalid_HotSideReferenceNotAboveDeltaTmax_IsRejected()
    {
        var profile = ValidAnalyticalShell() with
        {
            HotSideReferenceTemperatureK = 50.0,
            MaximumTemperatureDifferenceK = 70.0
        };
        Assert.ThrowsAny<ArgumentException>(() => profile.Validate());
    }

    [Fact]
    public void DatasheetEstimation_ProducesPositiveCoefficients()
    {
        var profile = ValidAnalyticalShell() with
        {
            AnalyticalCoefficients = null,
            MaximumCurrentA = 6.0,
            MaximumVoltageV = 15.0,
            MaximumCoolingPowerW = 50.0,
            MaximumTemperatureDifferenceK = 70.0,
            HotSideReferenceTemperatureK = 300.0,
            FittingMethod = "Lineykin-style datasheet estimation"
        };

        var coeffs = profile.EstimateAnalyticalCoefficientsFromDatasheet();
        Assert.True(coeffs.SeebeckCoefficientVPerK > 0.0);
        Assert.True(coeffs.ElectricalResistanceOhm > 0.0);
        Assert.True(coeffs.ThermalConductanceWPerK > 0.0);

        var mapped = profile.ToAnalyticalPeltierParameters();
        Assert.Equal(coeffs.SeebeckCoefficientVPerK, mapped.SeebeckCoefficientVPerK);
    }

    [Fact]
    public void ConstantCopProfile_RequiresPositiveCop()
    {
        var profile = ValidAnalyticalShell() with
        {
            ParameterModelType = TecParameterModelType.ConstantCop,
            ConstantCoolingCop = null
        };
        Assert.ThrowsAny<ArgumentException>(() => profile.Validate());
    }

    [Fact]
    public void SampleJsonFile_LoadsAndValidates()
    {
        var path = FindRepoFile(Path.Combine("samples", "tec-profiles", "generic-tec1-12706.json"));
        var loaded = TecManufacturerProfileSerializer.LoadFromFile(path);
        Assert.Equal(TecManufacturerProfileCatalog.GenericTec112706ProfileId, loaded.ProfileId);
        Assert.Equal(TecManufacturerProfile.CurrentSchemaVersion, loaded.SchemaVersion);
        _ = loaded.ToAnalyticalPeltierParameters();
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

        throw new FileNotFoundException($"Could not locate '{relativePath}' from test base directory.");
    }

    private static TecManufacturerProfile ValidAnalyticalShell()
        => new()
        {
            ProfileId = "test-profile",
            Manufacturer = "Generic",
            Model = "TEST",
            ParameterModelType = TecParameterModelType.AnalyticalSteadyState,
            SourceIdentifier = "thermocore://test",
            SourceRevision = "1",
            EvidenceLevel = TecEvidenceLevel.ProvisionalEngineering,
            MaximumCurrentA = 6.0,
            MaximumVoltageV = 15.0,
            MaximumCoolingPowerW = 50.0,
            MaximumTemperatureDifferenceK = 70.0,
            HotSideReferenceTemperatureK = 300.0,
            AnalyticalCoefficients = new TecAnalyticalCoefficientSet
            {
                SeebeckCoefficientVPerK = 0.05,
                ElectricalResistanceOhm = 2.0,
                ThermalConductanceWPerK = 0.5
            }
        };
}
