using ThermoCore.Core.Components;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Results;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

public class SimulationResultBundleExporterTests
{
    [Fact]
    public void BundleExporter_WritesCorePackageFilesAndManifestHashes()
    {
        var calculator = new PsychrometricCalculator();
        var inlet = calculator.CreateFromRelativeHumidity(
            UnitConversions.CelsiusToKelvin(25.0),
            PhysicalConstants.StandardAtmosphericPressurePa,
            0.5,
            0.02);
        var request = new SimulationRequest
        {
            Graph = new SimulationGraph(
                [
                    new AmbientAirSourceComponent("air", inlet),
                    new SensibleHeaterComponent("heater", 100.0, calculator),
                    new ExhaustAirSinkComponent("sink")
                ],
                [
                    new PhysicalConnection
                    {
                        Id = "a_h",
                        SourceComponentId = "air",
                        SourcePortId = "outlet",
                        TargetComponentId = "heater",
                        TargetPortId = "inlet"
                    },
                    new PhysicalConnection
                    {
                        Id = "h_s",
                        SourceComponentId = "heater",
                        SourcePortId = "outlet",
                        TargetComponentId = "sink",
                        TargetPortId = "inlet"
                    }
                ]),
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(2),
            TimeStep = TimeSpan.FromSeconds(1)
        };

        var run = new SimulationEngine().Run(request);
        Assert.True(run.Succeeded);
        var result = SimulationResultCollector.Collect(run, request);
        var directory = Path.Combine(Path.GetTempPath(), "core-bundle-" + Guid.NewGuid().ToString("N"));
        try
        {
            var manifest = SimulationResultBundleExporter.ExportDirectory(result, directory, run, simulationId: "core-1");
            Assert.Equal("core-1", manifest.SimulationId);
            Assert.True(File.Exists(Path.Combine(directory, "manifest.json")));
            Assert.True(File.Exists(Path.Combine(directory, "channels.json")));
            Assert.Contains(manifest.Files, f => f.Path == "summary.json" && f.Sha256.Length == 64);
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
