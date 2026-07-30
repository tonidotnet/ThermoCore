using System.Globalization;
using System.Text.Json;
using ThermoCore.AWG.Configuration;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Components;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;

namespace ThermoCore.Console;

/// <summary>
/// Console host for Core demos and AWG configuration / simulation commands.
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

        if (args.Length >= 2 && args[0] is "run" or "--run")
        {
            return RunAwgSimulation(args);
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
              dotnet run --project src/ThermoCore.Console -- run <path.json> [--duration 60] [--dt 1] [--export <dir>]
              dotnet run --project src/ThermoCore.Console -- write-default-config <path.json>
              dotnet run --project src/ThermoCore.Console -- --help

            Commands:
              demo                      Run a Core moist-air heater chain smoke simulation
              config <path>             Load AWG JSON configuration and build the V3 graph
              run <path>                Run an AWG simulation and print a summary (APP-003/004)
              write-default-config <p>  Write the MVP default AWG configuration JSON
              --help                    Show this help

            Run options:
              --duration / -d <sec>     Simulation duration in seconds (default 60)
              --dt / --timestep <sec>   Timestep in seconds (default 1)
              --export <dir>            Write DOC-029 CSV result bundle to directory (APP-005)
            """);
    }

    private static int RunAwgSimulation(string[] args)
    {
        try
        {
            var path = args[1];
            var durationSeconds = 60.0;
            var timeStepSeconds = 1.0;
            string? exportDirectory = null;

            for (var i = 2; i < args.Length; i++)
            {
                if (args[i] is "--duration" or "-d")
                {
                    if (i + 1 >= args.Length
                        || !double.TryParse(args[++i], NumberStyles.Float, CultureInfo.InvariantCulture, out durationSeconds)
                        || durationSeconds <= 0.0)
                    {
                        System.Console.Error.WriteLine("Invalid --duration value.");
                        return ExitUsageError;
                    }
                }
                else if (args[i] is "--dt" or "--timestep")
                {
                    if (i + 1 >= args.Length
                        || !double.TryParse(args[++i], NumberStyles.Float, CultureInfo.InvariantCulture, out timeStepSeconds)
                        || timeStepSeconds <= 0.0)
                    {
                        System.Console.Error.WriteLine("Invalid --dt value.");
                        return ExitUsageError;
                    }
                }
                else if (args[i] is "--export")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --export directory.");
                        return ExitUsageError;
                    }

                    exportDirectory = args[i];
                }
                else
                {
                    System.Console.Error.WriteLine($"Unknown run option: {args[i]}");
                    return ExitUsageError;
                }
            }

            var document = AwgConfigurationLoader.LoadFromFile(path);
            var options = AwgSimulationOptions.CreateDefault(
                TimeSpan.FromSeconds(durationSeconds),
                TimeSpan.FromSeconds(timeStepSeconds));
            var run = new AwgSimulationRunner().Run(document.System, document.InitialState, options);

            System.Console.WriteLine(AwgRunSummaryFormatter.Format(run.Summary));

            if (exportDirectory is not null)
            {
                AwgResultExporter.ExportCsv(run, exportDirectory);
                System.Console.WriteLine($"Exported CSV results to: {exportDirectory}");
            }

            if (!run.EngineResult.Succeeded)
            {
                foreach (var diagnostic in run.EngineResult.Diagnostics
                             .Where(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Critical)
                             .Take(20))
                {
                    System.Console.Error.WriteLine($"  [{diagnostic.Severity}] {diagnostic.Code}: {diagnostic.Message}");
                }

                return ExitSimulationFailed;
            }

            return ExitSuccess;
        }
        catch (Exception ex) when (ex is AwgConfigurationException or JsonException or FileNotFoundException or ArgumentException)
        {
            System.Console.Error.WriteLine($"Configuration/simulation error: {ex.Message}");
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
            System.Console.WriteLine("Tip: use 'run <path.json>' for a timed simulation with summary.");
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
