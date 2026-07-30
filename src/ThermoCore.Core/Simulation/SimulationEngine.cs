using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Numerics;
using ThermoCore.Core.Psychrometrics;

namespace ThermoCore.Core.Simulation;

/// <summary>
/// Simulation engine supporting acyclic execution and multi-tear fixed-point solves
/// (docs/04_Simulation/16_SimulationEngine.md).
/// </summary>
public sealed class SimulationEngine : ISimulationEngine
{
    private readonly IConservationValidator _conservationValidator;
    private readonly IPsychrometricCalculator _psychrometrics;

    public SimulationEngine(
        IConservationValidator? conservationValidator = null,
        IPsychrometricCalculator? psychrometrics = null)
    {
        _conservationValidator = conservationValidator ?? new ConservationValidator();
        _psychrometrics = psychrometrics ?? new PsychrometricCalculator();
    }

    public SimulationRunResult Run(
        SimulationRequest request,
        CancellationToken cancellationToken = default,
        IProgress<SimulationProgress>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        progress?.Report(new SimulationProgress
        {
            CompletedSteps = 0,
            TotalSteps = 0,
            SimulationTimeUtc = request.StartTimeUtc,
            CurrentPhase = "Validate"
        });

        var graphValidation = request.Graph.Validate();
        if (!graphValidation.IsValid)
        {
            return FailedBeforeSteps(graphValidation.Diagnostics);
        }

        var hasCycle = GraphTopology.HasCycle(request.Graph);
        if (hasCycle && request.Loops.Count == 0)
        {
            var cyclic = GraphTopology.GetCyclicComponentIds(request.Graph);
            return FailedBeforeSteps(
            [
                new SimulationDiagnostic
                {
                    Code = "ENGINE.CYCLE_DETECTED",
                    Severity = DiagnosticSeverity.Critical,
                    Message =
                        "Graph contains cycles but no SimulationLoopDefinition tear was provided. " +
                        $"Cyclic components: {string.Join(", ", cyclic)}."
                }
            ]);
        }

        IReadOnlyList<string> order;
        IReadOnlyList<ResolvedTear> tears = Array.Empty<ResolvedTear>();

        try
        {
            if (request.Loops.Count > 0)
            {
                var resolved = new List<ResolvedTear>(request.Loops.Count);
                foreach (var loopDefinition in request.Loops)
                {
                    var tearConnection = request.Graph.Connections.FirstOrDefault(c =>
                        string.Equals(c.Id, loopDefinition.TearConnectionId, StringComparison.Ordinal));
                    if (tearConnection is null)
                    {
                        return FailedBeforeSteps(
                        [
                            new SimulationDiagnostic
                            {
                                Code = "ENGINE.UNKNOWN_TEAR_CONNECTION",
                                Severity = DiagnosticSeverity.Critical,
                                Message = $"Unknown tear connection id '{loopDefinition.TearConnectionId}'."
                            }
                        ]);
                    }

                    if (loopDefinition.RelaxationFactor is <= 0.0 or > 1.0)
                    {
                        return FailedBeforeSteps(
                        [
                            new SimulationDiagnostic
                            {
                                Code = "ENGINE.INVALID_RELAXATION",
                                Severity = DiagnosticSeverity.Critical,
                                Message =
                                    $"Loop '{loopDefinition.Id}' has RelaxationFactor outside (0, 1]."
                            }
                        ]);
                    }

                    resolved.Add(new ResolvedTear(loopDefinition, tearConnection));
                }

                tears = resolved;
                order = GraphTopology.OrderComponentIdsIgnoringConnections(
                    request.Graph,
                    tears.Select(t => t.Loop.TearConnectionId));
            }
            else
            {
                order = GraphTopology.OrderComponentIds(request.Graph);
            }
        }
        catch (SimulationGraphException ex)
        {
            return FailedBeforeSteps(
            [
                new SimulationDiagnostic
                {
                    Code = "ENGINE.CYCLE_DETECTED",
                    Severity = DiagnosticSeverity.Critical,
                    Message = ex.Message
                }
            ]);
        }

        var simulationContext = new SimulationContext
        {
            SimulationStart = request.StartTimeUtc,
            TimeStep = request.TimeStep,
            ElapsedTime = TimeSpan.Zero,
            StepIndex = 0,
            NumericalTolerances = request.NumericalTolerances
        };

        foreach (var component in request.Graph.Components)
        {
            component.Initialize(simulationContext);
        }

        var stepCount = (int)Math.Ceiling(request.Duration.TotalSeconds / request.TimeStep.TotalSeconds);
        if (stepCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Simulation duration must cover at least one timestep.");
        }

        var steps = new List<SimulationStepResult>(stepCount);
        var aggregated = ConservationBalance.Empty;
        var allDiagnostics = new List<SimulationDiagnostic>();
        var committedPortStates = new Dictionary<string, object?>(StringComparer.Ordinal);

        progress?.Report(new SimulationProgress
        {
            CompletedSteps = 0,
            TotalSteps = stepCount,
            SimulationTimeUtc = request.StartTimeUtc,
            CurrentPhase = "Execute"
        });

        foreach (var pair in request.ExternalInputs)
        {
            committedPortStates[pair.Key] = pair.Value;
        }

        for (var stepIndex = 0; stepIndex < stepCount; stepIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var elapsed = TimeSpan.FromTicks(request.TimeStep.Ticks * stepIndex);
            var stepContext = simulationContext with
            {
                StepIndex = stepIndex,
                ElapsedTime = elapsed
            };

            var stepResult = tears.Count == 0
                ? ExecuteAcyclicStep(request, order, stepContext, committedPortStates, cancellationToken)
                : ExecuteTornLoopsStep(
                    request,
                    order,
                    tears,
                    stepContext,
                    committedPortStates,
                    cancellationToken);

            steps.Add(stepResult);
            allDiagnostics.AddRange(stepResult.Diagnostics);

            progress?.Report(new SimulationProgress
            {
                CompletedSteps = stepIndex + 1,
                TotalSteps = stepCount,
                SimulationTimeUtc = request.StartTimeUtc + elapsed + request.TimeStep,
                CurrentPhase = stepResult.Committed ? "Execute" : "Failed"
            });

            if (!stepResult.Committed)
            {
                return new SimulationRunResult
                {
                    Succeeded = false,
                    Steps = steps,
                    AggregatedBalance = aggregated,
                    Diagnostics = allDiagnostics
                };
            }

            aggregated = aggregated.Aggregate(stepResult.SystemBalance);
            foreach (var pair in stepResult.PortStates)
            {
                committedPortStates[pair.Key] = pair.Value;
            }
        }

        progress?.Report(new SimulationProgress
        {
            CompletedSteps = stepCount,
            TotalSteps = stepCount,
            SimulationTimeUtc = request.StartTimeUtc + request.Duration,
            CurrentPhase = "Complete"
        });

        return new SimulationRunResult
        {
            Succeeded = true,
            Steps = steps,
            AggregatedBalance = aggregated,
            Diagnostics = allDiagnostics
        };
    }

    private SimulationStepResult ExecuteAcyclicStep(
        SimulationRequest request,
        IReadOnlyList<string> order,
        SimulationContext stepContext,
        IReadOnlyDictionary<string, object?> previousPortStates,
        CancellationToken cancellationToken)
        => EvaluateAndMaybeCommit(
            request,
            order,
            stepContext,
            previousPortStates,
            tearOverride: null,
            solverIteration: 0,
            cancellationToken);

    private SimulationStepResult ExecuteTornLoopsStep(
        SimulationRequest request,
        IReadOnlyList<string> order,
        IReadOnlyList<ResolvedTear> tears,
        SimulationContext stepContext,
        IReadOnlyDictionary<string, object?> previousPortStates,
        CancellationToken cancellationToken)
    {
        var guesses = new Dictionary<string, MoistAirState>(StringComparer.Ordinal);
        foreach (var tear in tears)
        {
            var tearTargetKey = PortKey(tear.Connection.TargetComponentId, tear.Connection.TargetPortId);
            var tearSourceKey = PortKey(tear.Connection.SourceComponentId, tear.Connection.SourcePortId);
            if (!TryGetInitialTearGuess(previousPortStates, tearTargetKey, tearSourceKey, out var guess)
                || guess is not MoistAirState moistGuess)
            {
                return new SimulationStepResult
                {
                    StepIndex = stepContext.StepIndex,
                    ElapsedTime = stepContext.ElapsedTime,
                    Committed = false,
                    SystemBalance = ConservationBalance.Empty,
                    Diagnostics =
                    [
                        new SimulationDiagnostic
                        {
                            Code = "ENGINE.LOOP_INITIAL_GUESS_MISSING",
                            Severity = DiagnosticSeverity.Critical,
                            Message =
                                $"Loop '{tear.Loop.Id}' requires an initial MoistAirState guess at '{tearTargetKey}' " +
                                $"(provide ExternalInputs or a previous committed tear value).",
                            StepIndex = stepContext.StepIndex,
                            SimulationTime = stepContext.ElapsedTime,
                            SolverIteration = 0
                        }
                    ],
                    PortStates = previousPortStates
                };
            }

            guesses[tearTargetKey] = moistGuess;
        }

        var maximumIterations = tears.Max(t => t.Loop.MaximumIterations);
        ConservationBalance lastBalance = ConservationBalance.Empty;
        var diagnostics = new List<SimulationDiagnostic>();

        for (var iteration = 0; iteration < maximumIterations; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tearOverride = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var pair in guesses)
            {
                tearOverride[pair.Key] = pair.Value;
            }

            var evaluation = EvaluateOnly(
                request,
                order,
                stepContext,
                previousPortStates,
                tearOverride,
                iteration,
                cancellationToken);

            diagnostics.AddRange(evaluation.Diagnostics);

            if (evaluation.Diagnostics.Any(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Critical))
            {
                return new SimulationStepResult
                {
                    StepIndex = stepContext.StepIndex,
                    ElapsedTime = stepContext.ElapsedTime,
                    Committed = false,
                    SystemBalance = evaluation.SystemBalance,
                    Diagnostics = diagnostics,
                    PortStates = previousPortStates
                };
            }

            var allConverged = true;
            var nextGuesses = new Dictionary<string, MoistAirState>(StringComparer.Ordinal);
            foreach (var tear in tears)
            {
                var tearTargetKey = PortKey(tear.Connection.TargetComponentId, tear.Connection.TargetPortId);
                var tearSourceKey = PortKey(tear.Connection.SourceComponentId, tear.Connection.SourcePortId);
                var currentGuess = guesses[tearTargetKey];

                if (!evaluation.PortStates.TryGetValue(tearSourceKey, out var proposedRaw)
                    || proposedRaw is not MoistAirState proposed)
                {
                    return new SimulationStepResult
                    {
                        StepIndex = stepContext.StepIndex,
                        ElapsedTime = stepContext.ElapsedTime,
                        Committed = false,
                        SystemBalance = evaluation.SystemBalance,
                        Diagnostics =
                        [
                            .. diagnostics,
                            new SimulationDiagnostic
                            {
                                Code = "ENGINE.LOOP_TEAR_OUTPUT_MISSING",
                                Severity = DiagnosticSeverity.Critical,
                                Message =
                                    $"Loop '{tear.Loop.Id}' did not produce MoistAirState at '{tearSourceKey}'.",
                                StepIndex = stepContext.StepIndex,
                                SolverIteration = iteration
                            }
                        ],
                        PortStates = previousPortStates
                    };
                }

                if (!IsConverged(currentGuess, proposed, request.NumericalTolerances))
                {
                    allConverged = false;
                }

                nextGuesses[tearTargetKey] = Relax(currentGuess, proposed, tear.Loop.RelaxationFactor);
            }

            if (allConverged)
            {
                foreach (var componentId in order)
                {
                    request.Graph.GetComponent(componentId).Commit(evaluation.ProposedResults[componentId]);
                }

                return new SimulationStepResult
                {
                    StepIndex = stepContext.StepIndex,
                    ElapsedTime = stepContext.ElapsedTime,
                    Committed = true,
                    SystemBalance = evaluation.SystemBalance,
                    Diagnostics = diagnostics,
                    PortStates = evaluation.PortStates
                };
            }

            guesses = nextGuesses;
            lastBalance = evaluation.SystemBalance;
        }

        var loopIds = string.Join(", ", tears.Select(t => t.Loop.Id));
        return new SimulationStepResult
        {
            StepIndex = stepContext.StepIndex,
            ElapsedTime = stepContext.ElapsedTime,
            Committed = false,
            SystemBalance = lastBalance,
            Diagnostics =
            [
                .. diagnostics,
                new SimulationDiagnostic
                {
                    Code = "ENGINE.LOOP_NOT_CONVERGED",
                    Severity = DiagnosticSeverity.Critical,
                    Message =
                        $"Torn loops [{loopIds}] did not converge within {maximumIterations} iterations.",
                    StepIndex = stepContext.StepIndex,
                    SolverIteration = maximumIterations
                }
            ],
            PortStates = previousPortStates
        };
    }

    private SimulationStepResult EvaluateAndMaybeCommit(
        SimulationRequest request,
        IReadOnlyList<string> order,
        SimulationContext stepContext,
        IReadOnlyDictionary<string, object?> previousPortStates,
        IReadOnlyDictionary<string, object?>? tearOverride,
        int solverIteration,
        CancellationToken cancellationToken)
    {
        var evaluation = EvaluateOnly(
            request,
            order,
            stepContext,
            previousPortStates,
            tearOverride,
            solverIteration,
            cancellationToken);

        if (evaluation.Diagnostics.Any(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Critical))
        {
            return new SimulationStepResult
            {
                StepIndex = stepContext.StepIndex,
                ElapsedTime = stepContext.ElapsedTime,
                Committed = false,
                SystemBalance = evaluation.SystemBalance,
                Diagnostics = evaluation.Diagnostics,
                PortStates = previousPortStates
            };
        }

        var balanceValidation = _conservationValidator.Validate(evaluation.SystemBalance, request.BalanceTolerance);
        var diagnostics = evaluation.Diagnostics.Concat(balanceValidation.Diagnostics).ToList();
        if (!balanceValidation.IsValid)
        {
            return new SimulationStepResult
            {
                StepIndex = stepContext.StepIndex,
                ElapsedTime = stepContext.ElapsedTime,
                Committed = false,
                SystemBalance = evaluation.SystemBalance,
                Diagnostics = diagnostics,
                PortStates = previousPortStates
            };
        }

        cancellationToken.ThrowIfCancellationRequested();

        foreach (var componentId in order)
        {
            request.Graph.GetComponent(componentId).Commit(evaluation.ProposedResults[componentId]);
        }

        return new SimulationStepResult
        {
            StepIndex = stepContext.StepIndex,
            ElapsedTime = stepContext.ElapsedTime,
            Committed = true,
            SystemBalance = evaluation.SystemBalance,
            Diagnostics = diagnostics,
            PortStates = evaluation.PortStates
        };
    }

    private EvaluationScratch EvaluateOnly(
        SimulationRequest request,
        IReadOnlyList<string> order,
        SimulationContext stepContext,
        IReadOnlyDictionary<string, object?> previousPortStates,
        IReadOnlyDictionary<string, object?>? tearOverride,
        int solverIteration,
        CancellationToken cancellationToken)
    {
        var proposedPortStates = new Dictionary<string, object?>(previousPortStates, StringComparer.Ordinal);
        if (tearOverride is not null)
        {
            foreach (var pair in tearOverride)
            {
                proposedPortStates[pair.Key] = pair.Value;
            }
        }

        var proposedResults = new Dictionary<string, ComponentStepResult>(StringComparer.Ordinal);
        var diagnostics = new List<SimulationDiagnostic>();
        var systemBalance = ConservationBalance.Empty;

        foreach (var componentId in order)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var component = request.Graph.GetComponent(componentId);
            var inputStates = GatherInputs(request.Graph, component, proposedPortStates, tearOverride);

            var evaluation = component.Evaluate(new ComponentStepContext
            {
                Simulation = stepContext,
                SolverIteration = solverIteration,
                InputStates = inputStates
            });

            proposedResults[componentId] = evaluation;
            diagnostics.AddRange(evaluation.Diagnostics);

            foreach (var output in evaluation.OutputStates)
            {
                proposedPortStates[PortKey(componentId, output.Key)] = output.Value;
            }

            systemBalance = systemBalance.Aggregate(evaluation.Balance);
        }

        return new EvaluationScratch(proposedResults, proposedPortStates, systemBalance, diagnostics);
    }

    private static Dictionary<string, object?> GatherInputs(
        SimulationGraph graph,
        ISimulationComponent component,
        IReadOnlyDictionary<string, object?> portStates,
        IReadOnlyDictionary<string, object?>? tearOverride)
    {
        var inputs = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var port in component.Ports.Where(p => p.Direction is PortDirection.Input or PortDirection.Bidirectional))
        {
            var targetKey = PortKey(component.Id, port.Id);
            if (tearOverride is not null && tearOverride.TryGetValue(targetKey, out var torn))
            {
                inputs[port.Id] = torn;
                continue;
            }

            var incoming = graph.Connections.FirstOrDefault(c =>
                string.Equals(c.TargetComponentId, component.Id, StringComparison.Ordinal)
                && string.Equals(c.TargetPortId, port.Id, StringComparison.Ordinal));

            if (incoming is null)
            {
                continue;
            }

            var key = PortKey(incoming.SourceComponentId, incoming.SourcePortId);
            if (portStates.TryGetValue(key, out var value))
            {
                inputs[port.Id] = value;
            }
        }

        return inputs;
    }

    private bool IsConverged(MoistAirState previous, MoistAirState proposed, NumericalTolerances tolerances)
    {
        return NumericComparisons.AreTemperaturesEqual(previous.TemperatureK, proposed.TemperatureK, tolerances)
            && NumericComparisons.AreApproximatelyEqual(
                previous.HumidityRatioKgPerKgDryAir,
                proposed.HumidityRatioKgPerKgDryAir,
                tolerances.MassKg,
                tolerances.Relative)
            && NumericComparisons.AreApproximatelyEqual(
                previous.DryAirMassFlowKgPerSecond,
                proposed.DryAirMassFlowKgPerSecond,
                tolerances.MassFlowKgPerSecond,
                tolerances.Relative)
            && NumericComparisons.ArePressuresEqual(previous.PressurePa, proposed.PressurePa, tolerances);
    }

    private MoistAirState Relax(MoistAirState previous, MoistAirState proposed, double lambda)
    {
        var temperatureK = lambda * proposed.TemperatureK + (1.0 - lambda) * previous.TemperatureK;
        var pressurePa = lambda * proposed.PressurePa + (1.0 - lambda) * previous.PressurePa;
        var humidityRatio = lambda * proposed.HumidityRatioKgPerKgDryAir
            + (1.0 - lambda) * previous.HumidityRatioKgPerKgDryAir;
        var massFlow = lambda * proposed.DryAirMassFlowKgPerSecond
            + (1.0 - lambda) * previous.DryAirMassFlowKgPerSecond;

        return _psychrometrics.CreateFromHumidityRatio(temperatureK, pressurePa, humidityRatio, massFlow);
    }

    private static bool TryGetInitialTearGuess(
        IReadOnlyDictionary<string, object?> previousPortStates,
        string tearTargetKey,
        string tearSourceKey,
        out object? guess)
    {
        if (previousPortStates.TryGetValue(tearTargetKey, out guess) && guess is MoistAirState)
        {
            return true;
        }

        if (previousPortStates.TryGetValue(tearSourceKey, out guess) && guess is MoistAirState)
        {
            return true;
        }

        guess = null;
        return false;
    }

    private static SimulationRunResult FailedBeforeSteps(IReadOnlyList<SimulationDiagnostic> diagnostics)
        => new()
        {
            Succeeded = false,
            Steps = Array.Empty<SimulationStepResult>(),
            AggregatedBalance = ConservationBalance.Empty,
            Diagnostics = diagnostics
        };

    private static void ValidateRequest(SimulationRequest request)
    {
        if (request.Duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Duration must be positive.");
        }

        if (request.TimeStep <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "TimeStep must be positive.");
        }
    }

    internal static string PortKey(string componentId, string portId) => $"{componentId}.{portId}";

    private sealed record EvaluationScratch(
        Dictionary<string, ComponentStepResult> ProposedResults,
        Dictionary<string, object?> PortStates,
        ConservationBalance SystemBalance,
        List<SimulationDiagnostic> Diagnostics);

    private sealed record ResolvedTear(
        SimulationLoopDefinition Loop,
        PhysicalConnection Connection);
}
