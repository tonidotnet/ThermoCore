using ThermoCore.AWG.Topology;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Simulation;

/// <summary>Builds and executes an AWG V3 graph from configuration.</summary>
public sealed class AwgSimulationRunner
{
    private readonly IAwgSystemGraphBuilder _graphBuilder;

    public AwgSimulationRunner(IAwgSystemGraphBuilder? graphBuilder = null)
    {
        _graphBuilder = graphBuilder ?? new AwgV3SystemGraphBuilder();
    }

    public AwgSimulationRunResult Run(
        AwgSystemConfiguration configuration,
        AwgInitialState initialState,
        AwgSimulationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var built = _graphBuilder.Build(configuration, initialState);
        var engineResult = new AcyclicSimulationEngine().Run(
            new SimulationRequest
            {
                Graph = built.Graph,
                StartTimeUtc = options.StartTimeUtc,
                Duration = options.Duration,
                TimeStep = options.TimeStep
            },
            cancellationToken);

        var summary = AwgRunSummaryBuilder.Build(built, options, engineResult);
        return new AwgSimulationRunResult
        {
            BuiltSystem = built,
            Options = options,
            EngineResult = engineResult,
            Summary = summary
        };
    }
}
