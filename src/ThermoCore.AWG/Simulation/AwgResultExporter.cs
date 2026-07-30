using ThermoCore.Core.Results;
using ThermoCore.Core.Simulation;

namespace ThermoCore.AWG.Simulation;

/// <summary>Collects Core <see cref="SimulationResult"/> and writes DOC-029 CSV exports (APP-005).</summary>
public static class AwgResultExporter
{
    public static SimulationResult Collect(AwgSimulationRunResult run)
    {
        ArgumentNullException.ThrowIfNull(run);
        var request = new SimulationRequest
        {
            Graph = run.BuiltSystem.Graph,
            StartTimeUtc = run.Options.StartTimeUtc,
            Duration = run.Options.Duration,
            TimeStep = run.Options.TimeStep,
            ExternalInputs = run.BuiltSystem.ExternalInputs,
            Loops = run.BuiltSystem.Loops
        };
        return SimulationResultCollector.Collect(run.EngineResult, request);
    }

    public static SimulationResult ExportCsv(AwgSimulationRunResult run, string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var result = Collect(run);
        SimulationResultCsvExporter.ExportDirectory(result, directory, run.EngineResult);
        return result;
    }
}
