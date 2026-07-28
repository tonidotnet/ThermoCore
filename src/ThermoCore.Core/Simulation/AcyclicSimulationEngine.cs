using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;

namespace ThermoCore.Core.Simulation;

/// <summary>
/// Acyclic graph executor with Evaluate/Commit transaction per timestep
/// (docs/04_Simulation/16_SimulationEngine.md).
/// </summary>
public sealed class AcyclicSimulationEngine : ISimulationEngine
{
    private readonly IConservationValidator _conservationValidator;

    public AcyclicSimulationEngine(IConservationValidator? conservationValidator = null)
    {
        _conservationValidator = conservationValidator ?? new ConservationValidator();
    }

    public SimulationRunResult Run(
        SimulationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        var graphValidation = request.Graph.Validate();
        if (!graphValidation.IsValid)
        {
            return new SimulationRunResult
            {
                Succeeded = false,
                Steps = Array.Empty<SimulationStepResult>(),
                AggregatedBalance = ConservationBalance.Empty,
                Diagnostics = graphValidation.Diagnostics
            };
        }

        IReadOnlyList<string> order;
        try
        {
            order = GraphTopology.OrderComponentIds(request.Graph);
        }
        catch (SimulationGraphException ex)
        {
            return new SimulationRunResult
            {
                Succeeded = false,
                Steps = Array.Empty<SimulationStepResult>(),
                AggregatedBalance = ConservationBalance.Empty,
                Diagnostics =
                [
                    new SimulationDiagnostic
                    {
                        Code = "ENGINE.CYCLE_DETECTED",
                        Severity = DiagnosticSeverity.Critical,
                        Message = ex.Message
                    }
                ]
            };
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

            var stepResult = ExecuteStep(
                request,
                order,
                stepContext,
                committedPortStates,
                cancellationToken);

            steps.Add(stepResult);
            allDiagnostics.AddRange(stepResult.Diagnostics);

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

        return new SimulationRunResult
        {
            Succeeded = true,
            Steps = steps,
            AggregatedBalance = aggregated,
            Diagnostics = allDiagnostics
        };
    }

    private SimulationStepResult ExecuteStep(
        SimulationRequest request,
        IReadOnlyList<string> order,
        SimulationContext stepContext,
        IReadOnlyDictionary<string, object?> previousPortStates,
        CancellationToken cancellationToken)
    {
        var proposedPortStates = new Dictionary<string, object?>(previousPortStates, StringComparer.Ordinal);
        var proposedResults = new Dictionary<string, ComponentStepResult>(StringComparer.Ordinal);
        var diagnostics = new List<SimulationDiagnostic>();
        var systemBalance = ConservationBalance.Empty;

        foreach (var componentId in order)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var component = request.Graph.GetComponent(componentId);
            var inputStates = GatherInputs(request.Graph, component, proposedPortStates);

            var evaluation = component.Evaluate(new ComponentStepContext
            {
                Simulation = stepContext,
                SolverIteration = 0,
                InputStates = inputStates
            });

            proposedResults[componentId] = evaluation;
            diagnostics.AddRange(evaluation.Diagnostics);

            foreach (var output in evaluation.OutputStates)
            {
                proposedPortStates[PortKey(componentId, output.Key)] = output.Value;
            }

            systemBalance = systemBalance.Aggregate(evaluation.Balance);

            if (evaluation.Diagnostics.Any(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Critical))
            {
                return new SimulationStepResult
                {
                    StepIndex = stepContext.StepIndex,
                    ElapsedTime = stepContext.ElapsedTime,
                    Committed = false,
                    SystemBalance = systemBalance,
                    Diagnostics = diagnostics,
                    PortStates = previousPortStates
                };
            }
        }

        var balanceValidation = _conservationValidator.Validate(systemBalance, request.BalanceTolerance);
        diagnostics.AddRange(balanceValidation.Diagnostics);
        if (!balanceValidation.IsValid)
        {
            return new SimulationStepResult
            {
                StepIndex = stepContext.StepIndex,
                ElapsedTime = stepContext.ElapsedTime,
                Committed = false,
                SystemBalance = systemBalance,
                Diagnostics = diagnostics,
                PortStates = previousPortStates
            };
        }

        foreach (var componentId in order)
        {
            request.Graph.GetComponent(componentId).Commit(proposedResults[componentId]);
        }

        return new SimulationStepResult
        {
            StepIndex = stepContext.StepIndex,
            ElapsedTime = stepContext.ElapsedTime,
            Committed = true,
            SystemBalance = systemBalance,
            Diagnostics = diagnostics,
            PortStates = proposedPortStates
        };
    }

    private static Dictionary<string, object?> GatherInputs(
        SimulationGraph graph,
        ISimulationComponent component,
        IReadOnlyDictionary<string, object?> portStates)
    {
        var inputs = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var port in component.Ports.Where(p => p.Direction is PortDirection.Input or PortDirection.Bidirectional))
        {
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
}
