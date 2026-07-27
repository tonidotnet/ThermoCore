# ThermoCore
## 16_SimulationEngine.md

**Version:** 1.0  
**Status:** ReadyForImplementation  
**Document Type:** Simulation engine specification  
**Applies To:** ThermoCore.Core  
**Internal units:** SI

---

# 1. Purpose

This document defines the generic execution engine of ThermoCore.

The engine shall:

- validate component graphs;
- execute deterministic timesteps;
- separate evaluation from commit;
- support acyclic and cyclic graphs;
- aggregate diagnostics and balances;
- support cancellation and progress;
- produce reproducible result series;
- remain independent from AWG-specific behavior.

# 2. Architectural placement

Recommended namespace:

```csharp
ThermoCore.Core.Simulation
```

Supporting namespaces:

```text
ThermoCore.Core.Graph
ThermoCore.Core.Diagnostics
ThermoCore.Core.Numerics
ThermoCore.Core.Results
```

# 3. Core abstractions

```csharp
public interface ISimulationComponent
{
    string Id { get; }

    ComponentEvaluationResult Evaluate(
        ComponentEvaluationContext context);

    void Commit(ComponentCommitContext context);
}
```

Evaluation shall not mutate committed component state.

# 4. Simulation graph

```csharp
public sealed record SimulationGraph
{
    public required IReadOnlyList<ISimulationComponent> Components { get; init; }

    public required IReadOnlyList<SimulationConnection> Connections { get; init; }

    public required IReadOnlyList<SimulationLoopDefinition> Loops { get; init; }
}
```

# 5. Simulation request

```csharp
public sealed record SimulationRequest
{
    public required SimulationGraph Graph { get; init; }

    public required DateTimeOffset StartTimeUtc { get; init; }

    public required TimeSpan Duration { get; init; }

    public required TimeSpan TimeStep { get; init; }

    public required NumericalSettings NumericalSettings { get; init; }

    public required ResultCapturePolicy ResultCapturePolicy { get; init; }
}
```

# 6. Simulation context

```csharp
public sealed record SimulationStepContext
{
    public required long StepIndex { get; init; }

    public required DateTimeOffset SimulationTimeUtc { get; init; }

    public required TimeSpan TimeStep { get; init; }

    public required NumericalSettings NumericalSettings { get; init; }

    public required IReadOnlyDictionary<string, object> ExternalInputs { get; init; }
}
```

# 7. Engine lifecycle

1. Validate request.
2. Validate graph.
3. Initialize execution plan.
4. Initialize component states.
5. For each timestep:
   - obtain external inputs;
   - evaluate control components;
   - evaluate acyclic sections;
   - solve cyclic sections;
   - validate component results;
   - aggregate balances;
   - decide whether the step is acceptable;
   - commit accepted states;
   - capture results;
   - report progress.
6. Finalize result and metadata.
7. Return completed, cancelled or failed outcome.

# 8. Evaluate/commit transaction

A timestep acts as a transaction.

During evaluation:

- components read committed state;
- proposed outputs and proposed states are produced;
- no committed state changes.

Commit occurs only when:

- required solvers converge;
- critical diagnostics do not prohibit commit;
- conservation residuals are inside tolerance or policy explicitly allows warning-only behavior;
- cancellation was not requested.

# 9. Acyclic execution

Acyclic components shall be topologically sorted.

For each component:

1. gather input-port values;
2. create evaluation context;
3. evaluate;
4. store proposed output-port values;
5. continue to downstream components.

Execution ordering shall be deterministic for components with equal graph rank.

# 10. Cyclic execution

Each loop definition shall identify:

```text
Loop components
Loop input/output ports
Convergence variables
Initial guess policy
Relaxation factor
Maximum iterations
```

# 11. Fixed-point loop sequence

1. initialize loop variables from previous committed timestep;
2. evaluate loop components;
3. calculate new loop variables;
4. calculate convergence metrics;
5. apply relaxation;
6. repeat until converged or failed;
7. validate loop balances;
8. expose converged outputs to downstream graph.

# 12. Loop initialization

Preferred order:

1. previous timestep converged state;
2. explicit initial condition;
3. physically conservative fallback;
4. reject if no valid initialization exists.

# 13. Nested loops

MVP may reject nested or overlapping loops.

A later engine version may support strongly connected component decomposition.

# 14. Step rejection

A timestep shall be rejected when:

- a required loop does not converge;
- a component returns a critical invalid state;
- physical bounds are violated;
- balance policy marks residual as fatal;
- an unhandled exception occurs;
- cancellation is requested before commit.

# 15. Retry and timestep reduction

Optional policy:

```text
Retry rejected step with smaller internal timestep
```

The engine may split one external step into substeps while preserving the external result boundary.

Retry count and minimum timestep shall be configured.

# 16. Diagnostics

Diagnostics shall include:

```text
Severity
Code
Message
Component ID
Port ID
Step index
Simulation time
Loop iteration
Numeric values
Suggested action
```

# 17. Diagnostic policy

```csharp
public sealed record DiagnosticPolicy
{
    public required bool StopOnError { get; init; }

    public required bool StopOnCritical { get; init; }

    public required bool StopOnBalanceFailure { get; init; }

    public required int MaximumWarningsPerCode { get; init; }
}
```

# 18. Balance aggregation

Component balances shall aggregate into:

```text
Dry-air balance
Water balance
Energy balance
Electrical balance
Liquid-water inventory
Stored-energy change
```

Internal transfers between components shall cancel at system level.

# 19. Result capture

Policies:

```text
EveryStep
FixedInterval
SummaryOnly
SelectedChannels
EventTriggered
```

Full internal precision shall be retained until serialization or display.

# 20. Result model

```csharp
public sealed record SimulationResult
{
    public required SimulationRunMetadata Metadata { get; init; }

    public required SimulationRunStatus Status { get; init; }

    public required IReadOnlyList<SimulationTimePoint> TimeSeries { get; init; }

    public required SimulationSummary Summary { get; init; }

    public required IReadOnlyCollection<SimulationDiagnostic> Diagnostics { get; init; }
}
```

# 21. Run status

```csharp
public enum SimulationRunStatus
{
    Completed,
    CompletedWithWarnings,
    Cancelled,
    FailedValidation,
    FailedConvergence,
    FailedRuntime
}
```

# 22. Cancellation

Use `CancellationToken`.

Check:

- before each timestep;
- between loop iterations;
- before commit;
- before expensive result serialization.

Cancellation shall not commit a partially evaluated step.

# 23. Progress reporting

```csharp
public sealed record SimulationProgress
{
    public required long CompletedSteps { get; init; }

    public required long TotalSteps { get; init; }

    public required DateTimeOffset SimulationTimeUtc { get; init; }

    public required string CurrentPhase { get; init; }
}
```

# 24. Determinism

The engine shall avoid:

- unordered iteration where order affects results;
- wall-clock-based physics;
- unseeded randomness;
- mutable shared static state;
- non-deterministic parallel reduction.

Parallel scenario execution is allowed if each run is isolated.

# 25. Exceptions

Configuration and programming errors may throw before execution.

Expected runtime physical limitations should return diagnostics.

Unhandled exceptions shall be captured in run metadata without exposing sensitive host details through public APIs.

# 26. Performance

Initial priority is correctness.

Potential optimizations:

```text
Precomputed execution plan
Reduced allocations
Result downsampling
Parallel independent scenarios
Cached immutable lookup tables
```

Optimization shall not change accepted numerical results outside tolerance.

# 27. Engine interface

```csharp
public interface ISimulationEngine
{
    SimulationResult Run(
        SimulationRequest request,
        IProgress<SimulationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
```

An asynchronous host wrapper may run this method on background infrastructure.

# 28. Validation phases

## Request validation

Duration, timestep, time range, required inputs.

## Graph validation

Ports, domains, connectivity, loops, IDs.

## Parameter validation

Component-specific validation.

## Initial-state validation

Physical consistency and inventory.

## Runtime validation

Convergence, balances, limits.

# 29. Reproducibility metadata

Store:

```text
Application version
Core version
Component model versions
Parameter-set IDs
Topology version
Configuration hash
Numerical settings
Time range
Weather-data identifier
Result-capture policy
```

# 30. Required unit tests

- empty graph rejection;
- duplicate ID rejection;
- topological sorting;
- acyclic execution;
- evaluate without mutation;
- commit exactly once;
- failed step does not commit;
- fixed-point convergence;
- fixed-point failure;
- cancellation before commit;
- deterministic repeatability;
- result capture policies;
- diagnostic aggregation;
- system-balance aggregation.

# 31. Integration tests

- simple sensible-heater chain;
- mixer and splitter;
- recirculation loop;
- AWG topology smoke test;
- 24-hour time series;
- timestep retry;
- progress reporting;
- cancellation;
- full metadata reproducibility.

# 32. Acceptance criteria

The engine is accepted when:

1. component evaluation and state commit are separated;
2. graph execution is deterministic;
3. cyclic loops either converge explicitly or fail explicitly;
4. rejected steps do not mutate state;
5. cancellation is safe;
6. system balances are available for every committed step;
7. no AWG-specific type is required by ThermoCore.Core.

---

**End of Document**
