using ThermoCore.AWG.Cooling;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Components.VaporCompression;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.Tests;

public class VaporCompressionCoolingPlantAdapterTests
{
    private readonly PsychrometricCalculator _calculator = new();

    [Fact]
    public void Factory_CreatesVaporCompressionAdapter_FromInlineMap()
    {
        var map = VaporCompressionPerformanceMapCatalog.CreateGenericSmallDcModuleReference();
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false) with
        {
            Cooling = new AwgCoolingPlantConfiguration
            {
                Technology = CoolingTechnology.VaporCompression,
                VaporCompressionMap = map
            }
        };

        var plant = CoolingPlantFactory.Create(configuration, _calculator);
        Assert.IsType<VaporCompressionCoolingPlantAdapter>(plant);
        Assert.Equal(CoolingTechnology.VaporCompression, plant.Technology);

        var point = map.MapPoints.First(p => Math.Abs(p.SpeedFraction - 1.0) < 1e-9);
        var result = plant.Evaluate(new CoolingPlantRequest
        {
            Inlet = _calculator.CreateFromRelativeHumidity(
                UnitConversions.CelsiusToKelvin(32.0),
                PhysicalConstants.StandardAtmosphericPressurePa,
                0.70,
                0.02),
            Simulation = new SimulationContext
            {
                SimulationStart = DateTimeOffset.Parse("2026-07-15T10:00:00Z"),
                TimeStep = TimeSpan.FromSeconds(1),
                ElapsedTime = TimeSpan.Zero,
                StepIndex = 0
            },
            EvaporatingTemperatureK = point.EvaporatingTemperatureK,
            CondensingTemperatureK = point.CondensingTemperatureK,
            CompressorSpeedFraction = 1.0,
            CompressorRequested = true,
            FanElectricalPowerW = 5.0
        });

        Assert.Equal(CoolingTechnology.VaporCompression, result.Technology);
        Assert.True(result.CoolingDeliveredW > 0.0);
        Assert.True(result.CollectedWaterKgPerSecond >= 0.0);
        Assert.Equal(0.0, result.Balance.EnergyResidualJ, precision: 4);
        Assert.Equal(1.0, result.TechnologySpecificValues["compressorOn"]);
    }

    [Fact]
    public void Factory_LoadsMapFromSampleFile()
    {
        var path = FindRepoFile(Path.Combine("samples", "vapor-compression", "generic-small-dc-module.r5-001.json"));
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(enableElectricalSubsystem: false) with
        {
            Cooling = new AwgCoolingPlantConfiguration
            {
                Technology = CoolingTechnology.VaporCompression,
                VaporCompressionMapPath = path
            }
        };

        var plant = CoolingPlantFactory.Create(configuration, _calculator);
        Assert.IsType<VaporCompressionCoolingPlantAdapter>(plant);
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
