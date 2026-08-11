using System.Globalization;
using System.Text.Json;
using ThermoCore.App2.SolarAirHeater;
using ThermoCore.AWG.Calibration;
using ThermoCore.AWG.Configuration;
using ThermoCore.AWG.Measurement;
using ThermoCore.AWG.Optimization;
using ThermoCore.AWG.Regression;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Components;
using ThermoCore.Persistence;
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

        if (args.Length >= 1 && args[0] is "app2" or "--app2" or "solar-air-heater")
        {
            return RunSolarAirHeaterCommands(args);
        }

        if (args.Length >= 1 && args[0] is "write-campaign" or "--write-campaign")
        {
            return WriteSyntheticCampaign(args);
        }

        if (args is ["config", _] || args is ["--config", _])
        {
            return LoadAndBuildConfiguration(args[1]);
        }

        if (args.Length >= 2 && args[0] is "run" or "--run")
        {
            return RunAwgSimulation(args);
        }

        if (args.Length >= 1 && args[0] is "capture-baseline" or "--capture-baseline")
        {
            return RunCaptureBaseline(args);
        }

        if (args.Length >= 1 && args[0] is "regress" or "--regress")
        {
            return RunRegressionScenarios(args);
        }

        if (args.Length >= 1 && args[0] is "full-flow" or "--full-flow")
        {
            return RunFullAwgFlow(args);
        }

        if (args.Length >= 1 && args[0] is "full-flow-ambient-matrix" or "--full-flow-ambient-matrix")
        {
            return RunFullAwgAmbientMatrix(args);
        }

        if (args.Length >= 1 && args[0] is "full-flow-silica-matrix" or "--full-flow-silica-matrix")
        {
            return RunFullAwgSilicaMatrix(args);
        }

        if (args.Length >= 1 && args[0] is "full-flow-peltier-matrix" or "--full-flow-peltier-matrix")
        {
            return RunFullAwgPeltierMatrix(args);
        }

        if (args.Length >= 1 && args[0] is "summer-diurnal" or "--summer-diurnal")
        {
            return RunSummerDiurnalSizing(args);
        }

        if (args.Length >= 2 && args[0] is "validate" or "--validate")
        {
            return RunMeasurementValidation(args);
        }

        if (args.Length >= 2 && args[0] is "calibrate" or "--calibrate")
        {
            return RunParameterCalibration(args);
        }

        if (args.Length >= 2 && args[0] is "holdout" or "--holdout")
        {
            return RunHoldoutValidation(args);
        }

        if (args.Length >= 1 && args[0] is "sweep" or "--sweep")
        {
            return RunParameterSweep(args);
        }

        if (args.Length >= 1 && args[0] is "sensitivity" or "--sensitivity")
        {
            return RunSensitivityAnalysis(args);
        }

        if (args.Length >= 1 && args[0] is "random-search" or "--random-search")
        {
            return RunRandomSearch(args);
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
              dotnet run --project src/ThermoCore.Console -- run <path.json> [--duration 60] [--dt 1] [--export <dir>] [--db path.db]
              dotnet run --project src/ThermoCore.Console -- capture-baseline [--suite default|dry-sunny-matrix|all] [--out samples/baselines/r0-001]
              dotnet run --project src/ThermoCore.Console -- regress [--dir samples/scenarios]
              dotnet run --project src/ThermoCore.Console -- regress --dir samples/scenarios/dry-sunny-matrix
              dotnet run --project src/ThermoCore.Console -- regress --dir samples/scenarios/full-awg-flow
              dotnet run --project src/ThermoCore.Console -- full-flow [--out samples/scenarios/full-awg-flow]
              dotnet run --project src/ThermoCore.Console -- full-flow-ambient-matrix [--out samples/scenarios/full-awg-ambient-matrix]
              dotnet run --project src/ThermoCore.Console -- full-flow-silica-matrix [--out samples/scenarios/full-awg-silica-matrix]
              dotnet run --project src/ThermoCore.Console -- full-flow-peltier-matrix [--out samples/scenarios/full-awg-peltier-matrix]
              dotnet run --project src/ThermoCore.Console -- summer-diurnal [--out samples/scenarios/awg-summer-diurnal]
              dotnet run --project src/ThermoCore.Console -- validate <measurements.csv> [--config path.json] [--duration 3] [--dt 1]
              dotnet run --project src/ThermoCore.Console -- calibrate <measurements.csv> [--params id1,id2] [--db path.db]
              dotnet run --project src/ThermoCore.Console -- holdout <measurements.csv> [--train-fraction 0.7] [--params id1,id2]
              dotnet run --project src/ThermoCore.Console -- sweep --params id=v1,v2 [--params id2=...]
              dotnet run --project src/ThermoCore.Console -- sensitivity [--params id1,id2] [--perturbation 0.1]
              dotnet run --project src/ThermoCore.Console -- random-search [--samples 20] [--seed 42]
              dotnet run --project src/ThermoCore.Console -- app2 [--size]
              dotnet run --project src/ThermoCore.Console -- write-campaign [path.csv]
              dotnet run --project src/ThermoCore.Console -- write-default-config <path.json>
              dotnet run --project src/ThermoCore.Console -- --help

            Commands:
              demo                      Run a Core moist-air heater chain smoke simulation
              app2                      Run solar air heater MVP (APP2); add --size for sizing grid
              write-campaign [path]     Write synthetic multi-regime measurement CSV (M5 stand-in)
              config <path>             Load AWG JSON configuration and build the V3 graph
              run <path>                Run an AWG simulation and print a summary (APP-003/004)
              capture-baseline          Capture R0-001 machine-readable AWG regression baseline
              regress                   Run DOC-022 / APP-006 regression scenarios
              full-flow                 Full AWG V3 path (HR+electrical) + station T/RH/W report
              full-flow-ambient-matrix  Full AWG T×RH matrix (20–35 °C × 30–60% RH) + summary table
              full-flow-silica-matrix   Full AWG silica mass sweep @ 35 °C / 50% RH
              full-flow-peltier-matrix  Full AWG Peltier power sweep @ 35 °C / 50% RH
              summer-diurnal            24h summer day/night AWG + PV/battery sizing (0.5–3 L/day)
              validate <csv>            Compare simulation channels to measurement CSV (CAL)
              calibrate <csv>           Fit bounded AWG parameters to measurements (CAL-006)
              holdout <csv>             Fit on train window, score holdout (M5 / CAL holdout)
              sweep                     Grid-search calibratable parameters (OPT-002)
              sensitivity               One-at-a-time local sensitivity ranking (OPT-003)
              random-search             Uniform random search over calibratable bounds
              write-default-config <p>  Write the MVP default AWG configuration JSON
              --help                    Show this help

            Run options:
              --duration / -d <sec>     Simulation duration in seconds (default 60)
              --dt / --timestep <sec>   Timestep in seconds (default 1)
              --export <dir>            Write DOC-029 full result export bundle (AWG-017)
              --db <path|postgres:...>  Persist summary + series (sqlite path or postgres:conn)

            Regress options:
              --dir <path>              Load scenarios from JSON directory (default: built-in catalog)
                                        e.g. samples/scenarios/dry-sunny-matrix (T×silica pack)

            Validate options:
              --config <path>           AWG configuration JSON (default: MVP without electrical)
              --duration / -d <sec>     Simulation duration (default 30)
              --dt / --timestep <sec>   Timestep in seconds (default 1)
              --max-rmse <value>        Fail if overall RMSE exceeds threshold

            Calibrate options:
              --config <path>           Baseline AWG configuration JSON
              --duration / -d <sec>     Simulation duration (default 10)
              --dt / --timestep <sec>   Timestep in seconds (default 1)
              --params <id,id,...>      Calibratable parameter ids (default catalog)
              --db <path|postgres:...>  Store specifier (sqlite path or postgres:conn)
              --write-fitted <path>     Write fitted configuration JSON

            Holdout options:
              --config <path>           Baseline AWG configuration JSON
              --duration / -d <sec>     Simulation duration (default 10)
              --dt / --timestep <sec>   Timestep in seconds (default 1)
              --train-fraction <0-1>    Earlier timestamp fraction for fitting (default 0.7)
              --params <id,id,...>      Calibratable parameter ids (default catalog)

            Sweep options:
              --params <id=v1,v2,...>   Sweep axis (repeatable, max 3)
              --config <path>           Baseline AWG configuration JSON
              --duration / -d <sec>     Simulation duration (default 10)
              --dt / --timestep <sec>   Timestep in seconds (default 1)

            Sensitivity options:
              --params <id,id,...>      Parameter ids (default calibratable catalog)
              --perturbation <frac>     Relative ± half-step (default 0.10)
              --config <path>           Baseline AWG configuration JSON
              --duration / -d <sec>     Simulation duration (default 10)
              --dt / --timestep <sec>   Timestep in seconds (default 1)

            Random-search options:
              --samples <n>             Number of random points (default 20)
              --seed <int>              RNG seed for reproducibility
              --params <id,id,...>      Parameter ids (default calibratable catalog)
              --config <path>           Baseline AWG configuration JSON
              --duration / -d <sec>     Simulation duration (default 10)
              --dt / --timestep <sec>   Timestep in seconds (default 1)
            """);
    }

    private static int RunRandomSearch(string[] args)
    {
        try
        {
            string? configurationPath = null;
            var durationSeconds = 10.0;
            var timeStepSeconds = 1.0;
            var samples = 20;
            int? seed = null;
            string[]? parameterIds = null;

            for (var i = 1; i < args.Length; i++)
            {
                if (args[i] is "--config")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --config path.");
                        return ExitUsageError;
                    }

                    configurationPath = args[i];
                }
                else if (args[i] is "--duration" or "-d")
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
                else if (args[i] is "--samples")
                {
                    if (i + 1 >= args.Length || !int.TryParse(args[++i], out samples) || samples <= 0)
                    {
                        System.Console.Error.WriteLine("Invalid --samples value.");
                        return ExitUsageError;
                    }
                }
                else if (args[i] is "--seed")
                {
                    if (i + 1 >= args.Length || !int.TryParse(args[++i], out var seedValue))
                    {
                        System.Console.Error.WriteLine("Invalid --seed value.");
                        return ExitUsageError;
                    }

                    seed = seedValue;
                }
                else if (args[i] is "--params")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --params value.");
                        return ExitUsageError;
                    }

                    parameterIds = args[i]
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }
                else
                {
                    System.Console.Error.WriteLine($"Unknown random-search option: {args[i]}");
                    return ExitUsageError;
                }
            }

            var document = string.IsNullOrWhiteSpace(configurationPath)
                ? AwgConfigurationLoader.CreateDefaultDocument(enableElectricalSubsystem: false)
                : AwgConfigurationLoader.LoadFromFile(configurationPath);
            var options = AwgSimulationOptions.CreateDefault(
                TimeSpan.FromSeconds(durationSeconds),
                TimeSpan.FromSeconds(timeStepSeconds));

            var result = new AwgRandomSearchRunner().Run(
                document.System,
                document.InitialState,
                options,
                samples,
                seed,
                parameterIds);

            System.Console.WriteLine($"=== Random search ({result.Points.Count} samples) ===");
            if (result.BestLitersPerDay is { } best)
            {
                System.Console.WriteLine(
                    $"Best L/day={best.LitersPerDay.ToString("G6", CultureInfo.InvariantCulture)} " +
                    $"at {FormatValues(best.ParameterValues)}");
            }

            return result.Points.Any(p => p.Succeeded) ? ExitSuccess : ExitSimulationFailed;
        }
        catch (Exception ex) when (ex is ArgumentException or AwgConfigurationException or FileNotFoundException)
        {
            System.Console.Error.WriteLine($"Random-search error: {ex.Message}");
            return ExitConfigurationFailed;
        }
    }

    private static int RunSensitivityAnalysis(string[] args)
    {
        try
        {
            string? configurationPath = null;
            var durationSeconds = 10.0;
            var timeStepSeconds = 1.0;
            var perturbation = 0.10;
            string[]? parameterIds = null;

            for (var i = 1; i < args.Length; i++)
            {
                if (args[i] is "--config")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --config path.");
                        return ExitUsageError;
                    }

                    configurationPath = args[i];
                }
                else if (args[i] is "--duration" or "-d")
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
                else if (args[i] is "--perturbation")
                {
                    if (i + 1 >= args.Length
                        || !double.TryParse(args[++i], NumberStyles.Float, CultureInfo.InvariantCulture, out perturbation)
                        || perturbation is <= 0.0 or >= 1.0)
                    {
                        System.Console.Error.WriteLine("Invalid --perturbation value (expected in (0, 1)).");
                        return ExitUsageError;
                    }
                }
                else if (args[i] is "--params")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --params value.");
                        return ExitUsageError;
                    }

                    parameterIds = args[i]
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }
                else
                {
                    System.Console.Error.WriteLine($"Unknown sensitivity option: {args[i]}");
                    return ExitUsageError;
                }
            }

            var document = string.IsNullOrWhiteSpace(configurationPath)
                ? AwgConfigurationLoader.CreateDefaultDocument(enableElectricalSubsystem: false)
                : AwgConfigurationLoader.LoadFromFile(configurationPath);
            var options = AwgSimulationOptions.CreateDefault(
                TimeSpan.FromSeconds(durationSeconds),
                TimeSpan.FromSeconds(timeStepSeconds));

            var result = new AwgSensitivityAnalysisRunner().Run(
                document.System,
                document.InitialState,
                options,
                new AwgSensitivityAnalysisOptions { RelativePerturbationFraction = perturbation },
                parameterIds);

            System.Console.WriteLine("=== Sensitivity analysis (OAT) ===");
            System.Console.WriteLine(
                $"Baseline L/day={result.BaselineLitersPerDay.ToString("G6", CultureInfo.InvariantCulture)} " +
                $"waterKg={result.BaselineCollectedWaterKg.ToString("G4", CultureInfo.InvariantCulture)} " +
                $"[{(result.BaselineSucceeded ? "ok" : "fail")}]");
            if (!string.IsNullOrWhiteSpace(result.BaselineFailureMessage))
            {
                System.Console.WriteLine($"  {result.BaselineFailureMessage}");
            }

            System.Console.WriteLine("Ranked by |liters/day elasticity|:");
            foreach (var parameter in result.RankedByElasticityMagnitude)
            {
                System.Console.WriteLine(
                    $"  {parameter.ParameterId} | elasticity={parameter.LitersPerDayElasticity!.Value.ToString("G4", CultureInfo.InvariantCulture)} " +
                    $"dy/dx={parameter.LitersPerDayDerivative?.ToString("G4", CultureInfo.InvariantCulture) ?? "n/a"} " +
                    $"x0={parameter.BaselineValue.ToString("G4", CultureInfo.InvariantCulture)} " +
                    $"[{parameter.LowValue.ToString("G4", CultureInfo.InvariantCulture)} .. {parameter.HighValue.ToString("G4", CultureInfo.InvariantCulture)}]");
            }

            foreach (var parameter in result.Parameters.Where(p => !p.Succeeded))
            {
                System.Console.WriteLine($"  [fail] {parameter.ParameterId}: {parameter.FailureMessage}");
            }

            return result.BaselineSucceeded && result.Parameters.Any(p => p.Succeeded)
                ? ExitSuccess
                : ExitSimulationFailed;
        }
        catch (Exception ex) when (ex is ArgumentException or AwgConfigurationException or FileNotFoundException)
        {
            System.Console.Error.WriteLine($"Sensitivity error: {ex.Message}");
            return ExitConfigurationFailed;
        }
    }

    private static int RunParameterSweep(string[] args)
    {
        try
        {
            string? configurationPath = null;
            var durationSeconds = 10.0;
            var timeStepSeconds = 1.0;
            var axes = new List<AwgParameterSweepAxis>();

            for (var i = 1; i < args.Length; i++)
            {
                if (args[i] is "--config")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --config path.");
                        return ExitUsageError;
                    }

                    configurationPath = args[i];
                }
                else if (args[i] is "--duration" or "-d")
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
                else if (args[i] is "--params")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --params value.");
                        return ExitUsageError;
                    }

                    axes.Add(ParseSweepAxis(args[i]));
                }
                else
                {
                    System.Console.Error.WriteLine($"Unknown sweep option: {args[i]}");
                    return ExitUsageError;
                }
            }

            if (axes.Count == 0)
            {
                System.Console.Error.WriteLine("Sweep requires at least one --params id=v1,v2 axis.");
                return ExitUsageError;
            }

            var document = string.IsNullOrWhiteSpace(configurationPath)
                ? AwgConfigurationLoader.CreateDefaultDocument(enableElectricalSubsystem: false)
                : AwgConfigurationLoader.LoadFromFile(configurationPath);
            var options = AwgSimulationOptions.CreateDefault(
                TimeSpan.FromSeconds(durationSeconds),
                TimeSpan.FromSeconds(timeStepSeconds));

            var result = new AwgParameterSweepRunner().Run(
                document.System,
                document.InitialState,
                options,
                axes);

            System.Console.WriteLine("=== Parameter sweep ===");
            System.Console.WriteLine($"Points: {result.Points.Count}");
            foreach (var point in result.Points)
            {
                var values = string.Join(
                    ", ",
                    point.ParameterValues.OrderBy(p => p.Key, StringComparer.Ordinal)
                        .Select(p => $"{p.Key}={p.Value.ToString("G4", CultureInfo.InvariantCulture)}"));
                var wh = point.WattHoursPerLiter?.ToString("G4", CultureInfo.InvariantCulture) ?? "n/a";
                var solar = point.SolarUtilizationFraction?.ToString("G4", CultureInfo.InvariantCulture) ?? "n/a";
                var batt = point.BatteryThroughputFraction?.ToString("G4", CultureInfo.InvariantCulture) ?? "n/a";
                System.Console.WriteLine(
                    $"  [{(point.Succeeded ? "ok" : "fail")}] {values} | " +
                    $"L/day={point.LitersPerDay.ToString("G4", CultureInfo.InvariantCulture)} Wh/L={wh} " +
                    $"solarUtil={solar} battThru={batt} " +
                    $"waterKg={point.CollectedWaterKg.ToString("G4", CultureInfo.InvariantCulture)}");
                if (!string.IsNullOrWhiteSpace(point.FailureMessage))
                {
                    System.Console.WriteLine($"    {point.FailureMessage}");
                }
            }

            if (result.BestLitersPerDay is { } bestWater)
            {
                System.Console.WriteLine(
                    $"Best liters/day: {bestWater.LitersPerDay.ToString("G6", CultureInfo.InvariantCulture)} " +
                    $"at {FormatValues(bestWater.ParameterValues)}");
            }

            if (result.BestWattHoursPerLiter is { } bestEnergy)
            {
                System.Console.WriteLine(
                    $"Best Wh/liter: {bestEnergy.WattHoursPerLiter!.Value.ToString("G6", CultureInfo.InvariantCulture)} " +
                    $"at {FormatValues(bestEnergy.ParameterValues)}");
            }

            if (result.BestSolarUtilization is { } bestSolar)
            {
                System.Console.WriteLine(
                    $"Best solar utilization: {bestSolar.SolarUtilizationFraction!.Value.ToString("G6", CultureInfo.InvariantCulture)} " +
                    $"at {FormatValues(bestSolar.ParameterValues)}");
            }

            if (result.BestBatteryThroughput is { } bestBattery)
            {
                System.Console.WriteLine(
                    $"Best battery throughput: {bestBattery.BatteryThroughputFraction!.Value.ToString("G6", CultureInfo.InvariantCulture)} " +
                    $"at {FormatValues(bestBattery.ParameterValues)}");
            }

            var pareto = result.ParetoFrontLitersPerDayVsWattHoursPerLiter;
            if (pareto.Count > 0)
            {
                System.Console.WriteLine($"Pareto front (max L/day, min Wh/L): {pareto.Count} point(s)");
                foreach (var point in pareto)
                {
                    System.Console.WriteLine(
                        $"  L/day={point.LitersPerDay.ToString("G6", CultureInfo.InvariantCulture)} " +
                        $"Wh/L={point.WattHoursPerLiter!.Value.ToString("G6", CultureInfo.InvariantCulture)} " +
                        $"at {FormatValues(point.ParameterValues)}");
                }
            }

            return result.Points.Any(p => p.Succeeded) ? ExitSuccess : ExitSimulationFailed;
        }
        catch (Exception ex) when (ex is ArgumentException or AwgConfigurationException or FileNotFoundException)
        {
            System.Console.Error.WriteLine($"Sweep error: {ex.Message}");
            return ExitConfigurationFailed;
        }
    }

    private static AwgParameterSweepAxis ParseSweepAxis(string raw)
    {
        var eq = raw.IndexOf('=');
        if (eq <= 0 || eq >= raw.Length - 1)
        {
            throw new ArgumentException(
                $"Sweep axis '{raw}' must look like id=v1,v2,v3.",
                nameof(raw));
        }

        var id = raw[..eq].Trim();
        var values = raw[(eq + 1)..]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v =>
            {
                if (!double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                {
                    throw new ArgumentException($"Invalid sweep value '{v}' for '{id}'.");
                }

                return number;
            })
            .ToArray();

        return new AwgParameterSweepAxis
        {
            ParameterId = id,
            Values = values
        }.Validate();
    }

    private static string FormatValues(IReadOnlyDictionary<string, double> values)
        => string.Join(
            ", ",
            values.OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => $"{p.Key}={p.Value.ToString("G6", CultureInfo.InvariantCulture)}"));

    private static int RunParameterCalibration(string[] args)
    {
        try
        {
            var measurementPath = args[1];
            string? configurationPath = null;
            var durationSeconds = 10.0;
            var timeStepSeconds = 1.0;
            string? parameterList = null;
            string? databasePath = null;
            string? fittedPath = null;

            for (var i = 2; i < args.Length; i++)
            {
                if (args[i] is "--config")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --config path.");
                        return ExitUsageError;
                    }

                    configurationPath = args[i];
                }
                else if (args[i] is "--duration" or "-d")
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
                else if (args[i] is "--params")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --params value.");
                        return ExitUsageError;
                    }

                    parameterList = args[i];
                }
                else if (args[i] is "--db")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --db path.");
                        return ExitUsageError;
                    }

                    databasePath = args[i];
                }
                else if (args[i] is "--write-fitted")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --write-fitted path.");
                        return ExitUsageError;
                    }

                    fittedPath = args[i];
                }
                else
                {
                    System.Console.Error.WriteLine($"Unknown calibrate option: {args[i]}");
                    return ExitUsageError;
                }
            }

            IEnumerable<string>? parameterIds = parameterList?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var result = new AwgParameterCalibrationRunner().CalibrateFromFiles(
                measurementPath,
                configurationPath,
                durationSeconds,
                timeStepSeconds,
                parameterIds);

            System.Console.WriteLine("=== Parameter calibration ===");
            System.Console.WriteLine(
                $"Objective: {result.Fitting.InitialObjective.ToString("G6", CultureInfo.InvariantCulture)} -> " +
                $"{result.Fitting.FinalObjective.ToString("G6", CultureInfo.InvariantCulture)} " +
                $"(evals={result.Fitting.EvaluationCount}, passes={result.Fitting.PassCount})");
            foreach (var pair in result.Fitting.FittedValues.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                var initial = result.Fitting.InitialValues[pair.Key];
                System.Console.WriteLine(
                    $"  {pair.Key}: {initial.ToString("G6", CultureInfo.InvariantCulture)} -> " +
                    $"{pair.Value.ToString("G6", CultureInfo.InvariantCulture)}");
            }

            System.Console.WriteLine(
                $"RMSE baseline={result.BaselineReport.OverallRmse.ToString("G6", CultureInfo.InvariantCulture)} " +
                $"fitted={result.FittedReport.OverallRmse.ToString("G6", CultureInfo.InvariantCulture)}");

            if (fittedPath is not null)
            {
                var document = new AwgConfigurationDocument
                {
                    System = result.FittedConfiguration,
                    InitialState = AwgSystemDefaults.CreateMvpInitialState(result.FittedConfiguration)
                };
                AwgConfigurationLoader.SaveToFile(document, fittedPath);
                System.Console.WriteLine($"Wrote fitted configuration: {fittedPath}");
            }

            if (databasePath is not null)
            {
                var store = ThermoCoreStoreFactory.CreateFromSpecifier(databasePath);
                try
                {
                    store.EnsureCreated();
                    var baselineDoc = new AwgConfigurationDocument
                    {
                        System = result.BaselineConfiguration,
                        InitialState = AwgSystemDefaults.CreateMvpInitialState(result.BaselineConfiguration)
                    };
                    var fittedDoc = new AwgConfigurationDocument
                    {
                        System = result.FittedConfiguration,
                        InitialState = AwgSystemDefaults.CreateMvpInitialState(result.FittedConfiguration)
                    };
                    var baselineVersion = store.SaveConfiguration(baselineDoc, "calibration-baseline");
                    var fittedVersion = store.SaveConfiguration(fittedDoc, "calibration-fitted");
                    var stored = store.SaveCalibrationRun(
                        result,
                        measurementPath,
                        baselineVersion.Id,
                        fittedVersion.Id);
                    System.Console.WriteLine($"Saved calibration provenance: {stored.Id:N} in {databasePath}");
                }
                finally
                {
                    (store as IDisposable)?.Dispose();
                }
            }

            return result.Fitting.Improved || result.Fitting.FinalObjective <= result.Fitting.InitialObjective
                ? ExitSuccess
                : ExitSimulationFailed;
        }
        catch (Exception ex) when (
            ex is FileNotFoundException or FormatException or ArgumentException or AwgConfigurationException)
        {
            System.Console.Error.WriteLine($"Calibration error: {ex.Message}");
            return ExitConfigurationFailed;
        }
    }

    private static int RunHoldoutValidation(string[] args)
    {
        try
        {
            var measurementPath = args[1];
            string? configurationPath = null;
            string? parameterList = null;
            var durationSeconds = 10.0;
            var timeStepSeconds = 1.0;
            var trainFraction = 0.7;

            for (var i = 2; i < args.Length; i++)
            {
                if (args[i] is "--config")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --config path.");
                        return ExitUsageError;
                    }

                    configurationPath = args[i];
                }
                else if (args[i] is "--duration" or "-d")
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
                else if (args[i] is "--train-fraction")
                {
                    if (i + 1 >= args.Length
                        || !double.TryParse(args[++i], NumberStyles.Float, CultureInfo.InvariantCulture, out trainFraction)
                        || trainFraction is <= 0.0 or >= 1.0)
                    {
                        System.Console.Error.WriteLine("Invalid --train-fraction value (expected (0,1)).");
                        return ExitUsageError;
                    }
                }
                else if (args[i] is "--params")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --params value.");
                        return ExitUsageError;
                    }

                    parameterList = args[i];
                }
                else
                {
                    System.Console.Error.WriteLine($"Unknown holdout option: {args[i]}");
                    return ExitUsageError;
                }
            }

            IEnumerable<string>? parameterIds = parameterList?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var result = new AwgHoldoutValidationRunner().ValidateFromFiles(
                measurementPath,
                configurationPath,
                durationSeconds,
                timeStepSeconds,
                trainFraction,
                parameterIds);

            System.Console.WriteLine("=== Holdout validation ===");
            System.Console.WriteLine(
                $"Split after {result.Split.SplitAfterUtc:O} " +
                $"(train timestamps={result.Split.TrainTimestampCount}, " +
                $"holdout={result.Split.HoldoutTimestampCount}, fraction={result.Split.TrainFraction:F2})");
            System.Console.WriteLine(
                $"Train RMSE: baseline={result.Training.BaselineReport.OverallRmse.ToString("G6", CultureInfo.InvariantCulture)} " +
                $"fitted={result.Training.FittedReport.OverallRmse.ToString("G6", CultureInfo.InvariantCulture)}");
            System.Console.WriteLine(
                $"Holdout RMSE: baseline={result.HoldoutBaselineReport.OverallRmse.ToString("G6", CultureInfo.InvariantCulture)} " +
                $"fitted={result.HoldoutFittedReport.OverallRmse.ToString("G6", CultureInfo.InvariantCulture)} " +
                $"improved={result.HoldoutImproved}");

            foreach (var pair in result.Training.Fitting.FittedValues.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                System.Console.WriteLine(
                    $"  {pair.Key}={pair.Value.ToString("G6", CultureInfo.InvariantCulture)}");
            }

            return ExitSuccess;
        }
        catch (Exception ex) when (
            ex is FileNotFoundException or FormatException or ArgumentException or AwgConfigurationException)
        {
            System.Console.Error.WriteLine($"Holdout error: {ex.Message}");
            return ExitConfigurationFailed;
        }
    }

    private static int RunMeasurementValidation(string[] args)
    {
        try
        {
            var measurementPath = args[1];
            string? configurationPath = null;
            var durationSeconds = 30.0;
            var timeStepSeconds = 1.0;
            double? maxRmse = null;

            for (var i = 2; i < args.Length; i++)
            {
                if (args[i] is "--config")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --config path.");
                        return ExitUsageError;
                    }

                    configurationPath = args[i];
                }
                else if (args[i] is "--duration" or "-d")
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
                else if (args[i] is "--max-rmse")
                {
                    if (i + 1 >= args.Length
                        || !double.TryParse(args[++i], NumberStyles.Float, CultureInfo.InvariantCulture, out var threshold)
                        || threshold < 0.0)
                    {
                        System.Console.Error.WriteLine("Invalid --max-rmse value.");
                        return ExitUsageError;
                    }

                    maxRmse = threshold;
                }
                else
                {
                    System.Console.Error.WriteLine($"Unknown validate option: {args[i]}");
                    return ExitUsageError;
                }
            }

            var result = new AwgMeasurementValidationRunner().ValidateFromFiles(
                measurementPath,
                configurationPath,
                durationSeconds,
                timeStepSeconds);

            System.Console.WriteLine(AwgRunSummaryFormatter.Format(result.Run.Summary, result.Run.BalanceReport));
            System.Console.WriteLine();
            System.Console.WriteLine($"Measurement source: {result.Report.MeasurementSourcePath}");
            System.Console.WriteLine(
                $"Compared channels: {result.Report.Channels.Count}  missing: {result.Report.MissingChannels.Count}");
            System.Console.WriteLine(
                $"Overall RMSE: {result.Report.OverallRmse.ToString("G6", CultureInfo.InvariantCulture)}");

            foreach (var channel in result.Report.Channels)
            {
                System.Console.WriteLine(
                    $"  {channel.ChannelId}: RMSE={channel.Metrics.Rmse.ToString("G6", CultureInfo.InvariantCulture)} " +
                    $"MAE={channel.Metrics.Mae.ToString("G6", CultureInfo.InvariantCulture)} " +
                    $"bias={channel.Metrics.Bias.ToString("G6", CultureInfo.InvariantCulture)} " +
                    $"n={channel.MatchedSampleCount}");
            }

            foreach (var missing in result.Report.MissingChannels)
            {
                System.Console.WriteLine($"  missing channel: {missing}");
            }

            foreach (var warning in result.Report.Warnings)
            {
                System.Console.WriteLine($"  warning: {warning}");
            }

            if (!result.Run.EngineResult.Succeeded)
            {
                return ExitSimulationFailed;
            }

            if (result.Report.Channels.Count == 0)
            {
                System.Console.Error.WriteLine("No channels were compared.");
                return ExitSimulationFailed;
            }

            if (maxRmse is { } limit && result.Report.OverallRmse > limit)
            {
                System.Console.Error.WriteLine(
                    $"Overall RMSE {result.Report.OverallRmse.ToString("G6", CultureInfo.InvariantCulture)} exceeds limit {limit.ToString("G6", CultureInfo.InvariantCulture)}.");
                return ExitSimulationFailed;
            }

            return ExitSuccess;
        }
        catch (Exception ex) when (
            ex is FileNotFoundException or FormatException or ArgumentException or AwgConfigurationException)
        {
            System.Console.Error.WriteLine($"Validation error: {ex.Message}");
            return ExitConfigurationFailed;
        }
    }

    private static int RunFullAwgFlow(string[] args)
    {
        try
        {
            var outDir = Path.Combine("samples", "scenarios", "full-awg-flow");
            for (var i = 1; i < args.Length; i++)
            {
                if (args[i] is "--out")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --out path.");
                        return ExitUsageError;
                    }

                    outDir = args[i];
                }
                else
                {
                    System.Console.Error.WriteLine($"Unknown full-flow option: {args[i]}");
                    return ExitUsageError;
                }
            }

            var demo = AwgRegressionScenarioCatalog.CreateFullAwgFlowDemoScenario();
            var matrix = AwgRegressionScenarioCatalog.CreateFullAwgFlowDrySunnyScenarios();
            AwgRegressionScenarioCatalog.WriteScenarios(outDir, [demo, .. matrix]);

            var runner = new AwgRegressionScenarioRunner();
            var demoResult = runner.Run(demo);
            if (!demoResult.Passed)
            {
                System.Console.Error.WriteLine(
                    $"Full-flow demo failed: {string.Join("; ", demoResult.Failures)}");
                return ExitSimulationFailed;
            }

            var report = AwgFullFlowStationReportBuilder.Build(demoResult.Run);
            AwgFullFlowStationReportWriter.Write(report, outDir);

            System.Console.WriteLine("=== Full AWG flow (HR + electrical) ===");
            System.Console.WriteLine(
                $"Airflow: {demoResult.Run.BuiltSystem.Configuration.Ambient.DryAirMassFlowKgPerSecond.ToString("G4", CultureInfo.InvariantCulture)} kg/s");
            System.Console.WriteLine($"Wrote scenario pack + station report: {outDir}");
            foreach (var station in report.Stations)
            {
                System.Console.WriteLine(
                    $"  {station.StationId} {station.HungarianName}: " +
                    $"T={station.TemperatureC.ToString("0.00", CultureInfo.InvariantCulture)} °C  " +
                    $"RH={(station.RelativeHumidityFraction * 100.0).ToString("0.0", CultureInfo.InvariantCulture)}%  " +
                    $"W={station.HumidityRatioKgPerKgDryAir.ToString("0.000000", CultureInfo.InvariantCulture)} kg/kg");
            }

            return ExitSuccess;
        }
        catch (Exception ex) when (ex is ArgumentException or AwgConfigurationException or IOException)
        {
            System.Console.Error.WriteLine($"Full-flow error: {ex.Message}");
            return ExitConfigurationFailed;
        }
    }

    private static int RunFullAwgAmbientMatrix(string[] args)
    {
        try
        {
            if (!TryParseOutDirectory(
                    args,
                    Path.Combine("samples", "scenarios", "full-awg-ambient-matrix"),
                    "full-flow-ambient-matrix",
                    out var outDir,
                    out var exitCode))
            {
                return exitCode;
            }

            var scenarios = AwgRegressionScenarioCatalog.CreateFullAwgFlowAmbientMatrixScenarios();
            AwgRegressionScenarioCatalog.WriteScenarios(outDir, scenarios);
            var report = new AwgAmbientMatrixRunner().Run(scenarios);
            AwgAmbientMatrixReportWriter.Write(report, outDir);

            System.Console.WriteLine("=== Full AWG ambient matrix (T × RH) ===");
            System.Console.WriteLine($"Points: {report.Points.Count}  output: {outDir}");
            if (report.BestLitersPerDay is { } best)
            {
                System.Console.WriteLine(
                    $"Best L/day: {best.LitersPerDay.ToString("G6", CultureInfo.InvariantCulture)} " +
                    $"@ {best.AmbientTemperatureC.ToString("0", CultureInfo.InvariantCulture)} °C, " +
                    $"RH {best.RelativeHumidityPercent.ToString("0", CultureInfo.InvariantCulture)}%");
            }

            var failed = report.Points.Count(p => !p.Passed);
            return failed == 0 ? ExitSuccess : ExitSimulationFailed;
        }
        catch (Exception ex) when (ex is ArgumentException or AwgConfigurationException or IOException)
        {
            System.Console.Error.WriteLine($"Ambient-matrix error: {ex.Message}");
            return ExitConfigurationFailed;
        }
    }

    private static int RunSummerDiurnalSizing(string[] args)
    {
        try
        {
            if (!TryParseOutDirectory(
                    args,
                    Path.Combine("samples", "scenarios", "awg-summer-diurnal"),
                    "summer-diurnal",
                    out var outDir,
                    out var exitCode))
            {
                return exitCode;
            }

            var report = new ThermoCore.AWG.Sizing.AwgDiurnalSizingRunner().Run();
            ThermoCore.AWG.Sizing.AwgDiurnalSizingReportWriter.Write(report, outDir);
            File.WriteAllText(
                Path.Combine(outDir, "README.md"),
                """
                # AWG summer diurnal (24 h) + sizing

                Average summer day: day ~32 °C / 30% RH, night ~20 °C / 60% RH.

                ```bash
                dotnet run --project src/ThermoCore.Console -- summer-diurnal
                ```

                See `SUMMARY.md`, `sizing-table.svg`, `hourly-water-bars.svg`, and
                `docs/07_Applications/31_AwgSummerDiurnalOperation.md`.
                """);

            System.Console.WriteLine("=== Summer diurnal AWG + sizing ===");
            System.Console.WriteLine($"Output: {outDir}");
            if (!report.SimulationSucceeded)
            {
                System.Console.Error.WriteLine(report.FailureMessage ?? "Simulation failed.");
                return ExitSimulationFailed;
            }

            System.Console.WriteLine(
                $"Baseline: {report.BaselineWaterLiters.ToString("0.####", CultureInfo.InvariantCulture)} L/day, " +
                $"{report.BaselineDailyElectricalWh.ToString("0.#", CultureInfo.InvariantCulture)} Wh, " +
                $"{report.SpecificEnergyWhPerLiter.ToString("0.#", CultureInfo.InvariantCulture)} Wh/L");
            foreach (var t in report.Targets)
            {
                System.Console.WriteLine(
                    $"  {t.TargetLitersPerDay.ToString("0.#", CultureInfo.InvariantCulture)} L/day → " +
                    $"PV {t.RecommendedPvRatedPowerW.ToString("0", CultureInfo.InvariantCulture)} W, " +
                    $"batt {t.RecommendedBatteryCapacityWh.ToString("0", CultureInfo.InvariantCulture)} Wh, " +
                    $"{t.DailyElectricalEnergyWh.ToString("0", CultureInfo.InvariantCulture)} Wh/day");
            }

            return ExitSuccess;
        }
        catch (Exception ex) when (ex is ArgumentException or AwgConfigurationException or IOException)
        {
            System.Console.Error.WriteLine($"summer-diurnal error: {ex.Message}");
            return ExitConfigurationFailed;
        }
    }

    private static int RunFullAwgSilicaMatrix(string[] args)
        => RunFullAwgParameterSweep(
            args,
            commandName: "full-flow-silica-matrix",
            defaultOutDir: Path.Combine("samples", "scenarios", "full-awg-silica-matrix"),
            title: "Full AWG silica mass matrix @ 35 °C / 50% RH",
            parameterName: "Silica mass",
            parameterUnit: "kg",
            boundarySummary:
                "Fixed **35 °C**, **RH 50%**, G=950 W/m², Peltier 120 W, controlled adsorb/regen, 2 h, silica ∈ {1…5} kg.",
            consoleCommand: "dotnet run --project src/ThermoCore.Console -- full-flow-silica-matrix",
            scenarios: AwgRegressionScenarioCatalog.CreateFullAwgFlowSilicaMassMatrixScenarios(),
            parameterSelector: s => s.SilicaGelDryAdsorbentMassKg ?? 0.0);

    private static int RunFullAwgPeltierMatrix(string[] args)
        => RunFullAwgParameterSweep(
            args,
            commandName: "full-flow-peltier-matrix",
            defaultOutDir: Path.Combine("samples", "scenarios", "full-awg-peltier-matrix"),
            title: "Full AWG Peltier power matrix @ 35 °C / 50% RH",
            parameterName: "Peltier power",
            parameterUnit: "W",
            boundarySummary:
                "Fixed **35 °C**, **RH 50%**, G=950 W/m², silica 2 kg, controlled adsorb/regen, 2 h, Peltier ∈ {40…200} W.",
            consoleCommand: "dotnet run --project src/ThermoCore.Console -- full-flow-peltier-matrix",
            scenarios: AwgRegressionScenarioCatalog.CreateFullAwgFlowPeltierPowerMatrixScenarios(),
            parameterSelector: s => s.NominalPeltierPowerRequestW ?? 0.0);

    private static int RunFullAwgParameterSweep(
        string[] args,
        string commandName,
        string defaultOutDir,
        string title,
        string parameterName,
        string parameterUnit,
        string boundarySummary,
        string consoleCommand,
        IReadOnlyList<AwgRegressionScenario> scenarios,
        Func<AwgRegressionScenario, double> parameterSelector)
    {
        try
        {
            if (!TryParseOutDirectory(args, defaultOutDir, commandName, out var outDir, out var exitCode))
            {
                return exitCode;
            }

            AwgRegressionScenarioCatalog.WriteScenarios(outDir, scenarios);
            var report = new AwgSweepRunner().Run(
                title,
                parameterName,
                parameterUnit,
                boundarySummary,
                consoleCommand,
                scenarios,
                parameterSelector);
            AwgSweepReportWriter.Write(report, outDir);

            System.Console.WriteLine($"=== {title} ===");
            System.Console.WriteLine($"Points: {report.Points.Count}  output: {outDir}");
            if (report.BestLitersPerDay is { } best)
            {
                System.Console.WriteLine(
                    $"Best L/day: {best.LitersPerDay.ToString("G6", CultureInfo.InvariantCulture)} " +
                    $"@ {best.ParameterValue.ToString("0.##", CultureInfo.InvariantCulture)} {report.ParameterUnit}");
            }

            var failed = report.Points.Count(p => !p.Passed);
            return failed == 0 ? ExitSuccess : ExitSimulationFailed;
        }
        catch (Exception ex) when (ex is ArgumentException or AwgConfigurationException or IOException)
        {
            System.Console.Error.WriteLine($"{commandName} error: {ex.Message}");
            return ExitConfigurationFailed;
        }
    }

    private static bool TryParseOutDirectory(
        string[] args,
        string defaultOutDir,
        string commandName,
        out string outDir,
        out int exitCode)
    {
        outDir = defaultOutDir;
        exitCode = ExitSuccess;
        for (var i = 1; i < args.Length; i++)
        {
            if (args[i] is "--out")
            {
                if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                {
                    System.Console.Error.WriteLine("Invalid --out path.");
                    exitCode = ExitUsageError;
                    return false;
                }

                outDir = args[i];
            }
            else
            {
                System.Console.Error.WriteLine($"Unknown {commandName} option: {args[i]}");
                exitCode = ExitUsageError;
                return false;
            }
        }

        return true;
    }

    private static int RunCaptureBaseline(string[] args)
    {
        try
        {
            var suite = "all";
            var outDir = AwgRegressionBaselineCapture.DefaultBaselineDirectory;
            for (var i = 1; i < args.Length; i++)
            {
                if (args[i] is "--suite")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --suite value.");
                        return ExitUsageError;
                    }

                    suite = args[i];
                }
                else if (args[i] is "--out")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --out path.");
                        return ExitUsageError;
                    }

                    outDir = args[i];
                }
                else
                {
                    System.Console.Error.WriteLine($"Unknown capture-baseline option: {args[i]}");
                    return ExitUsageError;
                }
            }

            var gitSha = TryReadGitCommitSha();
            Directory.CreateDirectory(outDir);
            var documents = new List<AwgRegressionBaselineDocument>();
            switch (suite.Trim().ToLowerInvariant())
            {
                case "default":
                case "doc-022":
                    documents.Add(AwgRegressionBaselineCapture.CaptureDefaultSuite(gitSha));
                    break;
                case "dry-sunny-matrix":
                case "dry-sunny":
                    documents.Add(AwgRegressionBaselineCapture.CaptureDrySunnyMatrixSuite(gitSha));
                    break;
                case "all":
                    documents.Add(AwgRegressionBaselineCapture.CaptureDefaultSuite(gitSha));
                    documents.Add(AwgRegressionBaselineCapture.CaptureDrySunnyMatrixSuite(gitSha));
                    break;
                default:
                    System.Console.Error.WriteLine(
                        "Unknown --suite. Use: default | dry-sunny-matrix | all");
                    return ExitUsageError;
            }

            var failed = 0;
            System.Console.WriteLine("=== R0-001 AWG regression baseline capture ===");
            foreach (var document in documents)
            {
                var fileName = document.SuiteId + "-baseline.json";
                AwgRegressionBaselineCapture.Write(document, outDir, fileName);
                System.Console.WriteLine(
                    $"{document.SuiteId}: {document.PassedCount}/{document.ScenarioCount} passed → {Path.Combine(outDir, fileName)}");
                if (document.FailedCount > 0)
                {
                    failed += document.FailedCount;
                    foreach (var entry in document.Scenarios.Where(s => !s.Passed))
                    {
                        System.Console.Error.WriteLine(
                            $"  FAIL {entry.ScenarioId}: {string.Join("; ", entry.Failures)}");
                    }
                }
            }

            WriteBaselineReadme(outDir, documents, gitSha);
            return failed == 0 ? ExitSuccess : ExitSimulationFailed;
        }
        catch (Exception ex) when (ex is ArgumentException or AwgConfigurationException or IOException or JsonException)
        {
            System.Console.Error.WriteLine($"capture-baseline error: {ex.Message}");
            return ExitConfigurationFailed;
        }
    }

    private static void WriteBaselineReadme(
        string outDir,
        IReadOnlyList<AwgRegressionBaselineDocument> documents,
        string? gitSha)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# R0-001 — AWG regression baseline");
        sb.AppendLine();
        sb.AppendLine("Pre-research machine-readable baseline for ThermoCore AWG regressions.");
        sb.AppendLine();
        sb.AppendLine("## Identifiers");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|---|---|");
        sb.AppendLine("| Task | `R0-001` |");
        sb.AppendLine($"| Captured (UTC) | `{documents[0].CapturedUtc:O}` |");
        sb.AppendLine($"| Git commit | `{(gitSha ?? "unknown")}` |");
        sb.AppendLine();
        sb.AppendLine("## Suites");
        sb.AppendLine();
        foreach (var document in documents)
        {
            sb.AppendLine($"- **{document.SuiteId}** → `{document.SuiteId}-baseline.json` ");
            sb.AppendLine($"  ({document.PassedCount}/{document.ScenarioCount} passed) — {document.SuiteDescription}");
        }

        sb.AppendLine();
        sb.AppendLine("## Reproduce");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("dotnet build ThermoCore.slnx -nologo");
        sb.AppendLine("dotnet run --project src/ThermoCore.Console -- capture-baseline --suite all");
        sb.AppendLine("dotnet run --project src/ThermoCore.Console -- regress");
        sb.AppendLine("dotnet run --project src/ThermoCore.Console -- regress --dir samples/scenarios/dry-sunny-matrix");
        sb.AppendLine("dotnet test tests/ThermoCore.AWG.Tests --filter FullyQualifiedName~AwgRegressionAndPvRearAirTests");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("Later research PRs should compare scenario fingerprints, residuals, and water/tank metrics against these JSON files.");
        File.WriteAllText(Path.Combine(outDir, "README.md"), sb.ToString());
    }

    private static string? TryReadGitCommitSha()
    {
        try
        {
            var start = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse HEAD",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = System.Diagnostics.Process.Start(start);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5_000);
            return process.ExitCode == 0 && output.Length >= 7 ? output : null;
        }
        catch
        {
            return null;
        }
    }

    private static int RunRegressionScenarios(string[] args)
    {
        try
        {
            string? directory = null;
            for (var i = 1; i < args.Length; i++)
            {
                if (args[i] is "--dir")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --dir value.");
                        return ExitUsageError;
                    }

                    directory = args[i];
                }
                else
                {
                    System.Console.Error.WriteLine($"Unknown regress option: {args[i]}");
                    return ExitUsageError;
                }
            }

            IReadOnlyList<AwgRegressionScenario> scenarios = directory is null
                ? AwgRegressionScenarioCatalog.CreateDefaultScenarios()
                : AwgRegressionScenarioCatalog.LoadFromDirectory(directory);

            var results = new AwgRegressionScenarioRunner().RunAll(scenarios);
            var failed = 0;
            foreach (var result in results)
            {
                var mark = result.Passed ? "PASS" : "FAIL";
                System.Console.WriteLine(
                    $"[{mark}] {result.Scenario.Id} — steps={result.Run.EngineResult.Steps.Count}, " +
                    $"balance={(result.Run.BalanceReport.AllPassed ? "ok" : "fail")}");
                if (!result.Passed)
                {
                    failed++;
                    foreach (var failure in result.Failures)
                    {
                        System.Console.Error.WriteLine($"  {failure}");
                    }
                }
            }

            System.Console.WriteLine($"Regression complete: {results.Count - failed}/{results.Count} passed.");
            return failed == 0 ? ExitSuccess : ExitSimulationFailed;
        }
        catch (Exception ex) when (ex is DirectoryNotFoundException or JsonException or ArgumentException or AwgConfigurationException)
        {
            System.Console.Error.WriteLine($"Regression error: {ex.Message}");
            return ExitConfigurationFailed;
        }
    }

    private static int RunAwgSimulation(string[] args)
    {
        try
        {
            var path = args[1];
            var durationSeconds = 60.0;
            var timeStepSeconds = 1.0;
            string? exportDirectory = null;
            string? databasePath = null;

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
                else if (args[i] is "--db")
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[++i]))
                    {
                        System.Console.Error.WriteLine("Invalid --db path.");
                        return ExitUsageError;
                    }

                    databasePath = args[i];
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

            System.Console.WriteLine(AwgRunSummaryFormatter.Format(run.Summary, run.BalanceReport));

            if (exportDirectory is not null)
            {
                var (_, manifest) = AwgResultExporter.ExportBundle(run, exportDirectory);
                System.Console.WriteLine(
                    $"Exported result package ({manifest.Files.Count} files) to: {exportDirectory}");
            }

            if (databasePath is not null)
            {
                var store = ThermoCoreStoreFactory.CreateFromSpecifier(databasePath);
                try
                {
                    store.EnsureCreated();
                    var version = store.SaveConfiguration(document, Path.GetFileNameWithoutExtension(path));
                    var summary = store.SaveSimulationSummary(run, version.Id);
                    var series = store.SaveResultSeries(summary.Id, run);
                    System.Console.WriteLine(
                        $"Persisted summary {summary.Id:N} with {series.Channels.Count} series channel(s) to {databasePath}");
                }
                finally
                {
                    (store as IDisposable)?.Dispose();
                }
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

    private static int RunSolarAirHeaterCommands(string[] args)
    {
        var size = args.Any(a => a is "--size" or "size");
        if (size)
        {
            return RunSolarAirHeaterSizing();
        }

        var configuration = new SolarAirHeaterConfiguration();
        var result = new SolarAirHeaterSimulationRunner().Run(configuration);
        System.Console.WriteLine("=== Solar air heater (APP2) ===");
        System.Console.WriteLine($"Fingerprint: {result.BuiltSystem.GraphFingerprint}");
        System.Console.WriteLine($"Succeeded: {result.EngineResult.Succeeded}");
        System.Console.WriteLine(
            $"ΔT={result.TemperatureRiseK.ToString("G6", CultureInfo.InvariantCulture)} K  " +
            $"UsefulHeat={result.UsefulHeatW.ToString("G6", CultureInfo.InvariantCulture)} W  " +
            $"SolarUtil={result.SolarUtilizationFraction.ToString("G6", CultureInfo.InvariantCulture)}");
        return result.EngineResult.Succeeded ? ExitSuccess : ExitSimulationFailed;
    }

    private static int RunSolarAirHeaterSizing()
    {
        var result = new SolarAirHeaterSizingRunner().Run(
            new SolarAirHeaterConfiguration(),
            apertureAreasM2: [1.0, 2.0, 4.0],
            dryAirMassFlowsKgPerSecond: [0.03, 0.05, 0.08],
            irradiancesWPerM2: [400.0, 800.0]);

        System.Console.WriteLine("=== Solar air heater sizing (APP2-006) ===");
        System.Console.WriteLine($"Points: {result.Points.Count}");
        foreach (var point in result.Points.Where(p => p.Succeeded)
                     .OrderByDescending(p => p.UsefulHeatW)
                     .Take(8))
        {
            System.Console.WriteLine(
                $"  A={point.ApertureAreaM2.ToString("G4", CultureInfo.InvariantCulture)} m² " +
                $"mdot={point.DryAirMassFlowKgPerSecond.ToString("G4", CultureInfo.InvariantCulture)} kg/s " +
                $"G={point.SolarIrradianceWPerM2.ToString("G4", CultureInfo.InvariantCulture)} W/m² | " +
                $"ΔT={point.TemperatureRiseK.ToString("G4", CultureInfo.InvariantCulture)} K " +
                $"Qu={point.UsefulHeatW.ToString("G4", CultureInfo.InvariantCulture)} W");
        }

        if (result.BestUsefulHeat is { } best)
        {
            System.Console.WriteLine(
                $"Best useful heat: {best.UsefulHeatW.ToString("G6", CultureInfo.InvariantCulture)} W " +
                $"at A={best.ApertureAreaM2}, mdot={best.DryAirMassFlowKgPerSecond}, G={best.SolarIrradianceWPerM2}");
        }

        return result.Points.Any(p => p.Succeeded) ? ExitSuccess : ExitSimulationFailed;
    }

    private static int WriteSyntheticCampaign(string[] args)
    {
        var path = args.Length >= 2 && !args[1].StartsWith('-')
            ? args[1]
            : Path.Combine("samples", "calibration", "awg-mvp-campaign-synthetic.csv");

        try
        {
            var csv = AwgSyntheticCampaignGenerator.GenerateCsv(
                AwgSyntheticCampaignGenerator.CreateDefaultThreeRegimeSegments());
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, csv);
            System.Console.WriteLine($"Wrote synthetic campaign CSV: {path}");
            System.Console.WriteLine("Note: synthetic stand-in only — not physical prototype data.");
            return ExitSuccess;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException)
        {
            System.Console.Error.WriteLine($"write-campaign error: {ex.Message}");
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
