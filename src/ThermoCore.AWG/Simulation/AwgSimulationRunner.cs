using ThermoCore.AWG.Control;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Simulation;

namespace ThermoCore.AWG.Simulation;

/// <summary>Builds and executes an AWG V3 graph from configuration.</summary>
public sealed class AwgSimulationRunner
{
    private readonly IAwgSystemGraphBuilder _graphBuilder;
    private readonly ISimulationEngine _engine;

    public AwgSimulationRunner(
        IAwgSystemGraphBuilder? graphBuilder = null,
        ISimulationEngine? engine = null)
    {
        _graphBuilder = graphBuilder ?? new AwgV3SystemGraphBuilder();
        _engine = engine ?? new SimulationEngine();
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

        var built = _graphBuilder.Build(configuration, initialState, options.WeatherProvider);
        AwgControlCoordinator? coordinator = null;
        if (options.EnableController)
        {
            coordinator = new AwgControlCoordinator(
                built,
                parameters: options.ControlParameters,
                initialMode: options.InitialControllerMode);
        }

        var engineResult = _engine.Run(
            new SimulationRequest
            {
                Graph = built.Graph,
                StartTimeUtc = options.StartTimeUtc,
                Duration = options.Duration,
                TimeStep = options.TimeStep,
                ExternalInputs = built.ExternalInputs,
                Loops = built.Loops,
                StepHook = coordinator
            },
            cancellationToken);

        var summary = AwgRunSummaryBuilder.Build(built, options, engineResult);
        var balanceReport = AwgSystemBalanceVerifier.Verify(engineResult);
        return new AwgSimulationRunResult
        {
            BuiltSystem = built,
            Options = options,
            EngineResult = engineResult,
            Summary = summary,
            BalanceReport = balanceReport,
            FinalControllerState = coordinator?.CurrentState,
            ControllerDecisionTrace = coordinator?.DecisionTrace ?? Array.Empty<AwgDecisionTraceEntry>()
        };
    }
}
