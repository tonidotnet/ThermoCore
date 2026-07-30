using System.Text.Json;
using ThermoCore.AWG.Configuration;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Components;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;

namespace ThermoCore.Console;

/// <summary>
/// Minimal console host for Core smoke demos and AWG configuration loading.
/// </summary>
internal static class DemoHost
{
    private const int ExitSuccess = 0;
    private const int ExitUsageError = 2;
    private const int ExitSimulationFailed = 1;
    private const int ExitConfigurationFailed = 3;

    public static int Run(string[] args)
    {
        if (IsHelp(args))
        {
            PrintHelp();
            return ExitSuccess;
        }

        if (args.Length == 0 || args is ["demo"] or ["--demo"])
        {
            return RunHeaterChainDemo();
        }

        if (args is ["config", _] || args is ["--config", _])
        {
            return LoadAndBuildConfiguration(args[1]);
        }

        if (args is ["write-default-config", _])
        {
            return WriteDefaultConfiguration(args[1]);
        }

        System.Console.Error.WriteLine($"Unknown command: {args[0]}");
        PrintHelp();
        return ExitUsageError;
    }

    private static bool IsHelp(string[] args) =>
        args is ["--help"] or ["-h"] or ["help"] or ["/?"];

    private static void PrintHelp()
    {
        System.Console.WriteLine(
            """
            ThermoCore Console

            Usage:
              dotnet run --project src/ThermoCore.Console -- demo
              dotnet run --project src/ThermoCore.Console -- config <path.json>
              dotnet run --project src/ThermoCore.Console -- write-default-config <path.json>
              dotnet run --project src/ThermoCore.Console -- --help

            Commands:
              demo                      Run a Core moist-air heater chain smoke simulation
              config <path>             Load AWG JSON configuration and build the V3 graph
              write-default-config <p>  Write the MVP default AWG configuration JSON
              --help                    Show this help
            """);
    }

    private static int LoadAndBuildConfiguration(string path)
    {
        try
        {
            var document = AwgConfigurationLoader.LoadFromFile(path);
            var built = new AwgV3SystemGraphBuilder().Build(document.System, document.InitialState);

            System.Console.WriteLine($"Loaded configuration: {path}");
            System.Console.WriteLine($"Topology: {built.Metadata.TopologyId} v{built.Metadata.TopologyVersion}");
            System.Console.WriteLine($"Components: {built.Graph.Components.Count}");
            System.Console.WriteLine($"Connections: {built.Graph.Connections.Count}");
            System.Console.WriteLine($"Electrical: {built.Metadata.EnableElectricalSubsystem}");
            System.Console.WriteLine($"Fingerprint: {built.Metadata.GraphFingerprint}");

            var result = new AcyclicSimulationEngine().Run(new SimulationRequest
            {
                Graph = built.Graph,
                StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                Duration = TimeSpan.FromSeconds(1),
                TimeStep = TimeSpan.FromSeconds(1)
            });

            if (!result.Succeeded)
            {
                System.Console.Error.WriteLine("Configuration graph simulation failed.");
                foreach (var diagnostic in result.Diagnostics)
                {
                    System.Console.Error.WriteLine($"  [{diagnostic.Severity}] {diagnostic.Code}: {diagnostic.Message}");
                }

                return ExitSimulationFailed;
            }

            System.Console.WriteLine("Smoke simulation succeeded.");
            return ExitSuccess;
        }
        catch (Exception ex) when (ex is AwgConfigurationException or JsonException or FileNotFoundException or ArgumentException)
        {
            System.Console.Error.WriteLine($"Configuration error: {ex.Message}");
            if (ex is AwgConfigurationException awg)
            {
                foreach (var diagnostic in awg.Diagnostics)
                {
                    System.Console.Error.WriteLine($"  [{diagnostic.Severity}] {diagnostic.Code}: {diagnostic.Message}");
                }
            }

            return ExitConfigurationFailed;
        }
    }

    private static int WriteDefaultConfiguration(string path)
    {
        try
        {
            var document = AwgConfigurationLoader.CreateDefaultDocument();
            AwgConfigurationLoader.SaveToFile(document, path);
            System.Console.WriteLine($"Wrote default AWG configuration to {path}");
            return ExitSuccess;
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"Failed to write configuration: {ex.Message}");
            return ExitConfigurationFailed;
        }
    }

    private static int RunHeaterChainDemo()
    {
        System.Console.WriteLine("ThermoCore Console — Core heater-chain demo");

        var calculator = new PsychrometricCalculator();
        var ambient = calculator.CreateFromRelativeHumidity(
            UnitConversions.CelsiusToKelvin(25.0),
            PhysicalConstants.StandardAtmosphericPressurePa,
            relativeHumidityFraction: 0.50,
            dryAirMassFlowKgPerSecond: 0.02);

        var graph = new SimulationGraph(
            [
                new AmbientAirSourceComponent("source", ambient),
                new SensibleHeaterComponent("heater", heatRateW: 200.0, calculator),
                new ExhaustAirSinkComponent("sink")
            ],
            [
                new PhysicalConnection
                {
                    Id = "source.outlet->heater.inlet",
                    SourceComponentId = "source",
                    SourcePortId = "outlet",
                    TargetComponentId = "heater",
                    TargetPortId = "inlet"
                },
                new PhysicalConnection
                {
                    Id = "heater.outlet->sink.inlet",
                    SourceComponentId = "heater",
                    SourcePortId = "outlet",
                    TargetComponentId = "sink",
                    TargetPortId = "inlet"
                }
            ]);

        var result = new AcyclicSimulationEngine().Run(new SimulationRequest
        {
            Graph = graph,
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(1),
            TimeStep = TimeSpan.FromSeconds(1)
        });

        if (!result.Succeeded)
        {
            System.Console.Error.WriteLine("Simulation failed.");
            foreach (var diagnostic in result.Diagnostics)
            {
                System.Console.Error.WriteLine($"  [{diagnostic.Severity}] {diagnostic.Code}: {diagnostic.Message}");
            }

            return ExitSimulationFailed;
        }

        var outlet = (MoistAirState)result.Steps[0].PortStates["heater.outlet"]!;
        System.Console.WriteLine($"Engine succeeded: {result.Succeeded}");
        System.Console.WriteLine(
            $"Heater outlet: T={UnitConversions.KelvinToCelsius(outlet.TemperatureK):F2} °C, " +
            $"W={outlet.HumidityRatioKgPerKgDryAir:F6} kg/kg, " +
            $"h={outlet.SpecificEnthalpyJPerKgDryAir:F0} J/kg");

        return ExitSuccess;
    }
}
