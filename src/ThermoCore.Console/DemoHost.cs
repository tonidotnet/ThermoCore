using ThermoCore.Core.Components;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;

namespace ThermoCore.Console;

/// <summary>
/// Minimal console host for Core smoke demos. Full AWG configuration loading is APP-002+.
/// </summary>
internal static class DemoHost
{
    private const int ExitSuccess = 0;
    private const int ExitUsageError = 2;
    private const int ExitSimulationFailed = 1;

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

        System.Console.Error.WriteLine($"Unknown command: {args[0]}");
        PrintHelp();
        return ExitUsageError;
    }

    private static bool IsHelp(string[] args) =>
        args is ["--help"] or ["-h"] or ["help"] or ["/?"] ;

    private static void PrintHelp()
    {
        System.Console.WriteLine(
            """
            ThermoCore Console

            Usage:
              dotnet run --project src/ThermoCore.Console -- demo
              dotnet run --project src/ThermoCore.Console -- --help

            Commands:
              demo      Run a Core moist-air heater chain smoke simulation
              --help    Show this help

            Notes:
              JSON AWG configuration loading is not yet implemented (APP-002).
            """);
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
