using ThermoCore.AWG.Configuration;
using ThermoCore.AWG.Cooling;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Calibration;
using ThermoCore.Core.Components.Thermoelectric;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.Tests;

public class AwgCoolingPlantAbstractionTests
{
    private readonly PsychrometricCalculator _calculator = new();

    [Fact]
    public void MissingCoolingConfig_DefaultsToThermoelectric_AndOldJsonLoads()
    {
        var document = AwgConfigurationLoader.CreateDefaultDocument(enableElectricalSubsystem: false);
        Assert.Equal(CoolingTechnology.Thermoelectric, document.System.Cooling.Technology);

        var json = AwgConfigurationLoader.SaveToJson(document);
        var node = System.Text.Json.Nodes.JsonNode.Parse(json)
            ?? throw new InvalidOperationException("JSON parse failed.");
        Assert.True(((System.Text.Json.Nodes.JsonObject)node["system"]!).Remove("cooling"));

        var loaded = AwgConfigurationLoader.LoadFromJson(node.ToJsonString());
        Assert.Equal(CoolingTechnology.Thermoelectric, loaded.System.Cooling.Technology);

        var built = new AwgV3SystemGraphBuilder().Build(loaded.System, loaded.InitialState);
        Assert.Equal(CoolingTechnology.Thermoelectric, built.Metadata.CoolingTechnology);
        Assert.Contains(built.Graph.Components, c => c.Id == AwgV3TopologyIds.Condenser);
        Assert.Contains(built.Graph.Components, c => c.Id == AwgV3TopologyIds.CondenserCooling);
    }

    [Fact]
    public void ThermoelectricAdapter_ProducesComparableKpis_WithProxyCopNearOne()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false);
        var plant = CoolingPlantFactory.Create(configuration, _calculator);
        Assert.Equal(CoolingTechnology.Thermoelectric, plant.Technology);

        var result = plant.Evaluate(CreateRequest(
            temperatureC: 35.0,
            rh: 0.70,
            electricalW: 120.0,
            coolingW: 120.0,
            surfaceC: 10.0));

        Assert.True(result.CoolingDeliveredW >= 0.0);
        Assert.Equal(120.0, result.ElectricalInputW, precision: 8);
        Assert.NotNull(result.BareDeviceCop);
        Assert.InRange(result.BareDeviceCop!.Value, 0.99, 1.01);
        Assert.Equal(0.0, result.Balance.DryAirMassResidualKg, precision: 8);
    }

    [Fact]
    public void CommercialAdapter_UsesBlackBoxProfile_AndSharesKpiDefinitions()
    {
        var package = PrototypeWideCsvImporter.ImportPackageFromFiles(
            FindRepoFile(Path.Combine("samples", "calibration", "prototype-campaign.r3-001.json")));
        var profile = CommercialPeltierDehumidifierProfileFitter.FromPackage(package);
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false) with
        {
            Cooling = new AwgCoolingPlantConfiguration
            {
                Technology = CoolingTechnology.CommercialPeltierDehumidifier,
                CommercialProfile = profile
            }
        };

        var plant = CoolingPlantFactory.Create(configuration, _calculator);
        Assert.IsType<CommercialPeltierCoolingPlantAdapter>(plant);

        var point = profile.MapPoints[0];
        var inlet = _calculator.CreateFromRelativeHumidity(
            point.InletTemperatureK,
            PhysicalConstants.StandardAtmosphericPressurePa,
            point.InletRelativeHumidityFraction,
            0.015);
        var result = plant.Evaluate(new CoolingPlantRequest
        {
            Inlet = inlet,
            Simulation = SimContext(),
            ElectricalPowerW = point.ElectricalPowerW,
            FanElectricalPowerW = 5.0
        });

        Assert.Equal(CoolingTechnology.CommercialPeltierDehumidifier, result.Technology);
        Assert.True(result.CollectedWaterKgPerSecond > 0.0);
        Assert.Equal(point.ElectricalPowerW, result.ElectricalInputW, precision: 6);
        Assert.NotNull(result.BareDeviceCop);
        Assert.Equal(
            CommercialPeltierBlackBoxKpis.BareCoolingDeviceCop(
                result.CoolingDeliveredW,
                result.ElectricalInputW),
            result.BareDeviceCop);
    }

    [Fact]
    public void TechnologySwitch_DoesNotRequireTopologyRewrite_ForPlantEvaluate()
    {
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false);
        var tec = CoolingPlantFactory.Create(configuration, _calculator);

        var package = PrototypeWideCsvImporter.ImportPackageFromFiles(
            FindRepoFile(Path.Combine("samples", "calibration", "prototype-campaign.r3-001.json")));
        var commercial = CoolingPlantFactory.Create(
            configuration with
            {
                Cooling = new AwgCoolingPlantConfiguration
                {
                    Technology = CoolingTechnology.CommercialPeltierDehumidifier,
                    CommercialProfile = CommercialPeltierDehumidifierProfileFitter.FromPackage(package)
                }
            },
            _calculator);

        var request = CreateRequest(30.0, 0.60, electricalW: 41.4, coolingW: 41.4, surfaceC: 8.0);
        var tecResult = tec.Evaluate(request);
        var commercialResult = commercial.Evaluate(request);

        // Same request contract; different technology payloads — no graph/topology change required.
        Assert.Equal(CoolingTechnology.Thermoelectric, tecResult.Technology);
        Assert.Equal(CoolingTechnology.CommercialPeltierDehumidifier, commercialResult.Technology);
        Assert.NotNull(tecResult.Outlet);
        Assert.NotNull(commercialResult.Outlet);
    }

    [Fact]
    public void ReservedTechnologies_AreRejected()
    {
        var cooling = new AwgCoolingPlantConfiguration
        {
            Technology = CoolingTechnology.AbsorptionResearch
        };
        Assert.ThrowsAny<ArgumentException>(() => cooling.Validate());
    }

    [Fact]
    public void VaporCompression_RequiresMap()
    {
        var cooling = new AwgCoolingPlantConfiguration
        {
            Technology = CoolingTechnology.VaporCompression
        };
        Assert.ThrowsAny<ArgumentException>(() => cooling.Validate());
    }

    private CoolingPlantRequest CreateRequest(
        double temperatureC,
        double rh,
        double electricalW,
        double coolingW,
        double surfaceC)
        => new()
        {
            Inlet = _calculator.CreateFromRelativeHumidity(
                UnitConversions.CelsiusToKelvin(temperatureC),
                PhysicalConstants.StandardAtmosphericPressurePa,
                rh,
                0.02),
            Simulation = SimContext(),
            ElectricalPowerW = electricalW,
            AvailableCoolingPowerW = coolingW,
            ColdSurfaceTemperatureK = UnitConversions.CelsiusToKelvin(surfaceC),
            FanElectricalPowerW = 10.0
        };

    private static SimulationContext SimContext()
        => new()
        {
            SimulationStart = DateTimeOffset.Parse("2026-07-15T10:00:00Z"),
            TimeStep = TimeSpan.FromSeconds(1),
            ElapsedTime = TimeSpan.Zero,
            StepIndex = 0
        };

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
